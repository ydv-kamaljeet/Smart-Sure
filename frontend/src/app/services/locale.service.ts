import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, Subject, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';

export interface CountryLocale {
  country: string;
  flag: string;
  currencyCode: string;
  currencySymbol: string;
  timezone: string;
  locale: string;
}

export const COUNTRY_LOCALES: CountryLocale[] = [
  { country: 'India',          flag: '🇮🇳', currencyCode: 'INR', currencySymbol: '₹',   timezone: 'Asia/Kolkata',     locale: 'en-IN' },
  { country: 'United States',  flag: '🇺🇸', currencyCode: 'USD', currencySymbol: '$',   timezone: 'America/New_York', locale: 'en-US' },
  { country: 'United Kingdom', flag: '🇬🇧', currencyCode: 'GBP', currencySymbol: '£',   timezone: 'Europe/London',    locale: 'en-GB' },
  { country: 'Australia',      flag: '🇦🇺', currencyCode: 'AUD', currencySymbol: 'A$',  timezone: 'Australia/Sydney', locale: 'en-AU' },
  { country: 'Canada',         flag: '🇨🇦', currencyCode: 'CAD', currencySymbol: 'C$',  timezone: 'America/Toronto',  locale: 'en-CA' },
  { country: 'Singapore',      flag: '🇸🇬', currencyCode: 'SGD', currencySymbol: 'S$',  timezone: 'Asia/Singapore',   locale: 'en-SG' },
];

const STORAGE_KEY = 'app_country';
const RATES_CACHE_KEY = 'app_exchange_rates';
const RATES_CACHE_TTL = 6 * 60 * 60 * 1000; // 6 hours

// Hardcoded fallback rates from INR (updated periodically)
const FALLBACK_RATES: Record<string, number> = {
  INR: 1,
  USD: 0.01194,
  GBP: 0.00938,
  AUD: 0.01843,
  CAD: 0.01651,
  SGD: 0.01601,
};

// All amounts in the app are stored in INR
const BASE_CURRENCY = 'INR';

@Injectable({ providedIn: 'root' })
export class LocaleService {
  // rates$ emits whenever exchange rates are loaded/updated
  private rates$ = new BehaviorSubject<Record<string, number>>({ INR: 1 });
  private localeChange$ = new Subject<void>();
  ratesLoaded = false;

  constructor(private http: HttpClient) {
    this.loadRates();
  }

  // ── Country / locale ──────────────────────────────────────────────────────

  getLocale(): CountryLocale {
    const saved = localStorage.getItem(STORAGE_KEY);
    return COUNTRY_LOCALES.find(c => c.country === saved) ?? COUNTRY_LOCALES[0];
  }

  setCountry(country: string): void {
    localStorage.setItem(STORAGE_KEY, country);
    // Force re-render by nudging the rates subject
    this.rates$.next(this.rates$.getValue());
  }

  get currencyCode(): string { return this.getLocale().currencyCode; }
  get timezone(): string     { return this.getLocale().timezone; }
  get locale(): string       { return this.getLocale().locale; }

  // ── Currency conversion ───────────────────────────────────────────────────

  /** Convert an INR amount to the currently selected currency */
  convert(amountInr: number): number {
    const rates = this.rates$.getValue();
    const code = this.currencyCode;
    if (code === BASE_CURRENCY) return amountInr;
    // Use live rate, fall back to hardcoded rate, fall back to 1
    const rate = rates[code] ?? FALLBACK_RATES[code] ?? 1;
    return amountInr * rate;
  }

  /** Observable of rates — useful for async pipe if needed */
  getRates(): Observable<Record<string, number>> {
    return this.rates$.asObservable();
  }

  // ── Rate loading ──────────────────────────────────────────────────────────

  private loadRates(): void {
    // Try cache first
    const cached = this.getCachedRates();
    if (cached) {
      this.rates$.next(cached);
      this.ratesLoaded = true;
      return;
    }
    this.fetchRates();
  }

  private fetchRates(): void {
    // fawazahmed0/currency-api — free, no key, updated daily from multiple sources
    // Primary CDN, fallback to direct GitHub
    const primary = `https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies/inr.json`;
    const fallbackUrl = `https://latest.currency-api.pages.dev/v1/currencies/inr.json`;

    this.http.get<any>(primary).pipe(
      catchError(() => this.http.get<any>(fallbackUrl))
    ).pipe(
      map(res => {
        // Response shape: { date: '...', inr: { usd: 0.0107, ... } }
        const raw = res.inr as Record<string, number>;
        // Convert to uppercase keys to match our currencyCode values
        const rates: Record<string, number> = { INR: 1 };
        Object.entries(raw).forEach(([k, v]) => rates[k.toUpperCase()] = v);
        return rates;
      }),
      tap(rates => {
        this.cacheRates(rates);
        this.rates$.next(rates);
        this.ratesLoaded = true;
      }),
      catchError(() => {
        this.rates$.next(FALLBACK_RATES);
        this.ratesLoaded = true;
        return of(FALLBACK_RATES);
      })
    ).subscribe();
  }

  private getCachedRates(): Record<string, number> | null {
    try {
      const raw = localStorage.getItem(RATES_CACHE_KEY);
      if (!raw) return null;
      const { rates, timestamp } = JSON.parse(raw);
      if (Date.now() - timestamp > RATES_CACHE_TTL) return null;
      // Invalidate cache if any required currency is missing
      const required = ['USD', 'GBP', 'AUD', 'CAD', 'SGD'];
      if (required.some(c => !rates[c])) return null;
      return rates;
    } catch { return null; }
  }

  private cacheRates(rates: Record<string, number>): void {
    localStorage.setItem(RATES_CACHE_KEY, JSON.stringify({ rates, timestamp: Date.now() }));
  }
}
