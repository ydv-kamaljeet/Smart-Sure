import { Pipe, PipeTransform, inject, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { Subscription } from 'rxjs';
import { LocaleService } from '../services/locale.service';

/**
 * Usage: {{ amountInINR | convertCurrency }}
 * Converts from INR to the user's selected currency using live exchange rates.
 * pure: false so it re-evaluates when rates load or country changes.
 */
@Pipe({ name: 'convertCurrency', standalone: true, pure: false })
export class ConvertCurrencyPipe implements PipeTransform, OnDestroy {
  private locale = inject(LocaleService);
  private cdr = inject(ChangeDetectorRef);
  private currencyPipe = new CurrencyPipe('en-US');
  private sub: Subscription;

  constructor() {
    // Re-trigger change detection whenever rates update
    this.sub = this.locale.getRates().subscribe(() => this.cdr.markForCheck());
  }

  transform(amountInr: number | null | undefined, digitsInfo = '1.0-0'): string {
    if (amountInr == null) return '';
    const converted = this.locale.convert(amountInr);
    const loc = this.locale.getLocale();
    return this.currencyPipe.transform(converted, loc.currencyCode, 'symbol', digitsInfo, loc.locale) ?? '';
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }
}
