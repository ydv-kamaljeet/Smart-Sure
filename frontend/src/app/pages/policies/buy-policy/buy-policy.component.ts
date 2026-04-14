import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PolicyService } from '../../../services/policy.service';
import { LocaleService } from '../../../services/locale.service';
import { ConvertCurrencyPipe } from '../../../pipes/convert-currency.pipe';
import { InsuranceType, InsuranceSubType } from '../../../models/models';

@Component({
  selector: 'app-buy-policy',
  standalone: true,
  imports: [CommonModule, FormsModule, ConvertCurrencyPipe],
  templateUrl: './buy-policy.component.html',
  styleUrls: ['./buy-policy.component.css']
})
export class BuyPolicyComponent implements OnInit {
  private policyService = inject(PolicyService);
  private router = inject(Router);
  locale = inject(LocaleService);

  step = 1;
  loading = true;
  submitting = false;
  error: string | null = null;

  insuranceTypes: InsuranceType[] = [];
  subTypes: InsuranceSubType[] = [];
  selectedType: InsuranceType | null = null;
  selectedSubType: InsuranceSubType | null = null;

  idv = 0;
  finalPremium = 0;

  vehicle = { make: '', model: '', year: new Date().getFullYear(), listedPrice: 0, licensePlate: '', vin: '', annualMileage: 0 };
  home = { propertyAddress: '', propertyValue: 0, yearBuilt: 2000, constructionType: 'Brick', hasSecuritySystem: false, hasFireAlarm: false };

  ngOnInit(): void {
    this.policyService.getInsuranceTypes().subscribe({
      next: (types) => { this.insuranceTypes = types; this.loading = false; },
      error: () => { this.error = 'Failed to load insurance catalog.'; this.loading = false; }
    });
  }

  isVehicle(): boolean { return this.selectedType?.name?.toLowerCase().includes('vehicle') ?? false; }
  isHome(): boolean { return this.selectedType?.name?.toLowerCase().includes('home') ?? false; }
  isContentInsurance(): boolean { return this.selectedSubType?.name?.toLowerCase().includes('content') ?? false; }

  selectType(type: InsuranceType): void {
    this.selectedType = type;
    this.loading = true;
    this.policyService.getInsuranceSubTypes(type.id).subscribe({
      next: (subs) => { this.subTypes = subs; this.step = 2; this.loading = false; },
      error: () => { this.error = 'Failed to load plans.'; this.loading = false; }
    });
  }

  selectSubType(sub: InsuranceSubType): void {
    this.selectedSubType = sub;
    this.step = 3;
  }

  goBack(): void { if (this.step > 1) this.step--; }

  // Vehicle IDV: listed price minus depreciation by age
  calculateVehicleIDV(): number {
    const age = new Date().getFullYear() - this.vehicle.year;
    let dep = age < 1 ? 0.05 : age < 2 ? 0.15 : age < 3 ? 0.20 : age < 4 ? 0.30 : age < 5 ? 0.40 : 0.50;
    return Math.round(this.vehicle.listedPrice * (1 - dep));
  }

  // Home IDV: reconstruction cost (80% of market value) minus age depreciation
  // This matches IRDAI home insurance practice — IDV never equals full market value
  calculateHomeIDV(): number {
    const age = new Date().getFullYear() - this.home.yearBuilt;
    const dep = age < 5 ? 0.10 : age < 10 ? 0.20 : age < 20 ? 0.30 : age < 30 ? 0.40 : 0.50;
    const reconstructionCost = this.home.propertyValue * 0.80; // 80% of market value
    return Math.round(reconstructionCost * (1 - dep));
  }
  calculatePremium(idv: number): number {
    if (this.isVehicle()) {
      let p = idv * 0.02;
      if (this.vehicle.annualMileage > 15000) p *= 1.2;
      return Math.round(p);
    } else {
      let p = idv * 0.001;
      if (this.home.hasSecuritySystem) p *= 0.9;
      return Math.round(p);
    }
  }

  goToPayment(): void {
    this.idv = this.isVehicle() ? this.calculateVehicleIDV() : this.calculateHomeIDV();
    this.finalPremium = this.calculatePremium(this.idv);
    this.step = 4;
  }

  onBuy(): void {
    this.submitting = true;
    this.error = null;

    this.policyService.createRazorpayOrder(this.finalPremium).subscribe({
      next: (order) => this.openRazorpay(order),
      error: () => { this.error = 'Failed to initiate payment. Please try again.'; this.submitting = false; }
    });
  }

  private openRazorpay(order: { orderId: string; amount: number; currency: string; keyId: string }): void {
    const options = {
      key: order.keyId,
      amount: order.amount * 100, // paise
      currency: order.currency,
      name: 'SmartSure',
      description: `${this.selectedType?.name} - ${this.selectedSubType?.name}`,
      order_id: order.orderId,
      handler: (response: any) => this.onPaymentSuccess(response),
      modal: {
        ondismiss: () => { this.submitting = false; }
      },
      theme: { color: '#1a56db' }
    };

    const rzp = new (window as any).Razorpay(options);
    rzp.open();
  }

  private onPaymentSuccess(response: any): void {
    const payload: any = {
      razorpayOrderId: response.razorpay_order_id,
      razorpayPaymentId: response.razorpay_payment_id,
      razorpaySignature: response.razorpay_signature,
      subTypeId: this.selectedSubType!.id
    };

    if (this.isVehicle()) {
      payload.vehicleDetails = { make: this.vehicle.make, model: this.vehicle.model, year: this.vehicle.year, listedPrice: this.vehicle.listedPrice, licensePlate: this.vehicle.licensePlate, vin: this.vehicle.vin, annualMileage: this.vehicle.annualMileage };
    } else {
      payload.homeDetails = { propertyAddress: this.home.propertyAddress, propertyValue: this.home.propertyValue, yearBuilt: this.home.yearBuilt, constructionType: this.home.constructionType, hasSecuritySystem: this.home.hasSecuritySystem, hasFireAlarm: this.home.hasFireAlarm };
    }

    this.policyService.verifyRazorpayPayment(payload).subscribe({
      next: () => this.router.navigate(['/policies']),
      error: (err) => { this.error = err.error?.errorMessage ?? 'Payment verification failed.'; this.submitting = false; }
    });
  }
}
