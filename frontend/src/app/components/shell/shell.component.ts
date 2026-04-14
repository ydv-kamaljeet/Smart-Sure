import { Component, OnInit, HostListener } from '@angular/core';
import { Router, RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { LocaleService, COUNTRY_LOCALES } from '../../services/locale.service';

interface NavItem {
  label: string;
  path: string;
  icon: string;
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule, FormsModule],
  template: `
    <!-- Sidebar overlay (mobile) -->
    <div class="sidebar-overlay" [class.d-block]="sidebarOpen" (click)="closeSidebar()"></div>

    <!-- Sidebar -->
    <aside class="sidebar" [class.open]="sidebarOpen" [class.admin-theme]="isAdmin">
      <div class="sidebar-logo">
        <div class="logo-icon" [class.admin-gradient]="isAdmin">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
            <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
          </svg>
        </div>
        <span class="logo-text">SmartSure</span>
      </div>

      <nav class="sidebar-nav">
        <a *ngFor="let item of navItems"
           [routerLink]="item.path"
           routerLinkActive="active"
           class="nav-link-item"
           (click)="closeSidebar()">
          <i [class]="'bi ' + item.icon"></i>
          {{ item.label }}
        </a>
      </nav>

      <div class="sidebar-footer">
        <button class="nav-link-item w-100 border-0 bg-transparent" (click)="logout()">
          <i class="bi bi-box-arrow-left"></i>
          Logout
        </button>
      </div>
    </aside>

    <!-- Main wrapper -->
    <div class="main-wrapper">
      <!-- Top bar -->
      <header class="topbar">
        <div class="d-flex align-items-center gap-3">
          <button class="d-lg-none btn btn-sm btn-light p-2" (click)="toggleSidebar()">
            <i class="bi bi-list fs-5"></i>
          </button>
          <span class="topbar-title">{{ pageTitle }}</span>
        </div>
        <div class="d-flex align-items-center gap-3">
          <!-- Country / locale selector -->
          <div class="dropdown">
            <button class="btn btn-sm btn-outline-secondary dropdown-toggle d-flex align-items-center gap-1 py-1 px-2"
                    type="button" data-bs-toggle="dropdown" aria-expanded="false"
                    style="font-size:0.8rem;">
              <span style="font-size:1.1rem;">{{ currentFlag }}</span>
              <span class="d-none d-sm-inline">{{ currentCountry }}</span>
            </button>
            <ul class="dropdown-menu dropdown-menu-end" style="min-width:200px;max-height:300px;overflow-y:auto;">
              <li *ngFor="let c of countries">
                <button class="dropdown-item d-flex align-items-center gap-2 py-1"
                        [class.active]="c.country === currentCountry"
                        (click)="changeCountry(c.country)">
                  <span style="font-size:1.1rem;">{{ c.flag }}</span>
                  <span style="font-size:0.85rem;">{{ c.country }}</span>
                  <span class="ms-auto text-muted" style="font-size:0.75rem;">{{ c.currencySymbol }}</span>
                </button>
              </li>
            </ul>
          </div>
          <!-- User Profile Dropdown -->
          <div class="dropdown ms-2">
            <button class="btn btn-link dropdown-toggle text-decoration-none d-flex align-items-center gap-2 p-0"
                    type="button" data-bs-toggle="dropdown" aria-expanded="false" style="color: inherit;">
              <div class="user-avatar" [class.admin-gradient]="isAdmin">{{ userInitials }}</div>
              <span class="d-none d-sm-block text-sm fw-medium text-secondary">{{ userName }}</span>
            </button>
            <ul class="dropdown-menu dropdown-menu-end shadow border-0 mt-2 rounded-3">
              <li class="px-3 py-2 border-bottom mb-1 bg-light rounded-top">
                <p class="mb-0 fw-bold text-dark">{{ userName }}</p>
                <p class="mb-0 text-muted" style="font-size:0.8rem;">Logged in</p>
              </li>
              <li *ngIf="!isAdmin">
                <a class="dropdown-item py-2 d-flex align-items-center gap-2" routerLink="/profile">
                  <i class="bi bi-person"></i> View Profile
                </a>
              </li>
              <li>
                <button class="dropdown-item py-2 text-danger d-flex align-items-center gap-2" (click)="logout()">
                  <i class="bi bi-box-arrow-right"></i> Logout
                </button>
              </li>
            </ul>
          </div>
        </div>
      </header>

      <!-- Page content -->
      <main class="page-content">
        <router-outlet />
      </main>
    </div>
  `
})
export class ShellComponent implements OnInit {
  sidebarOpen = false;
  userInitials = '';
  userName = '';
  pageTitle = 'Dashboard';
  isAdmin = false;
  countries = COUNTRY_LOCALES;
  currentFlag = '🇮🇳';
  currentCountry = 'India';

  navItems: NavItem[] = [];

  private customerNav: NavItem[] = [
    { label: 'Dashboard', path: '/dashboard', icon: 'bi-speedometer2' },
    { label: 'My Policies', path: '/policies', icon: 'bi-file-earmark-text' },
    { label: 'My Claims', path: '/claims', icon: 'bi-exclamation-circle' },
    { label: 'Initiate Claim', path: '/claims/initiate', icon: 'bi-file-earmark-plus' },
    { label: 'Buy Policy', path: '/new-policy', icon: 'bi-cart-plus' },
    { label: 'AI Assistant', path: '/ai-assistant', icon: 'bi-robot' },
    { label: 'Profile', path: '/profile', icon: 'bi-person-circle' },
  ];

  private adminNav: NavItem[] = [
    { label: 'Admin Dashboard', path: '/dashboard', icon: 'bi-speedometer2' },
    { label: 'Manage Users', path: '/admin/users', icon: 'bi-people' },
    { label: 'Manage Policies', path: '/admin/policies', icon: 'bi-file-earmark-text' },
    { label: 'All Claims', path: '/admin/claims', icon: 'bi-exclamation-circle' },
    { label: 'Insurance Catalog', path: '/admin/insurance', icon: 'bi-shield-plus' },
    { label: 'Reports', path: '/admin/reports', icon: 'bi-graph-up' },
    { label: 'Audit Logs', path: '/admin/audit-logs', icon: 'bi-shield-lock' },
    { label: 'System Logs', path: '/admin/system-logs', icon: 'bi-terminal' },
    { label: 'AI Assistant', path: '/ai-assistant', icon: 'bi-robot' },
  ];

  private pageTitles: Record<string, string> = {
    '/dashboard': 'Dashboard',
    '/policies': 'My Policies',
    '/claims': 'My Claims',
    '/claims/initiate': 'Initiate Claim',
    '/new-policy': 'Buy New Policy',
    '/profile': 'Profile',
    '/admin/users': 'Manage Users',
    '/admin/audit-logs': 'Audit Logs',
    '/admin/reports': 'Reports',
    '/admin/insurance': 'Insurance Catalog',
    '/admin/policies': 'Manage Policies',
    '/admin/claims': 'Manage Claims',
    '/admin/system-logs': 'System Logs',
    '/ai-assistant': 'AI Assistant',
  };

  constructor(private auth: AuthService, private router: Router, private locale: LocaleService) {}

  ngOnInit(): void {
    this.userInitials = this.auth.getUserInitials();
    this.userName = this.auth.getUserFullName();
    this.refreshLocale();
    
    const user = this.auth.getUser();
    this.isAdmin = user?.roles?.includes('Admin') ?? false;
    this.navItems = this.isAdmin ? this.adminNav : this.customerNav;

    this.router.events.subscribe(() => {
      this.updatePageTitle();
    });
    this.updatePageTitle();
  }

  refreshLocale(): void {
    const loc = this.locale.getLocale();
    this.currentFlag = loc.flag;
    this.currentCountry = loc.country;
  }

  changeCountry(country: string): void {
    this.locale.setCountry(country);
    this.refreshLocale();
  }

  private updatePageTitle(): void {
    const path = this.router.url.split('?')[0]; // Handle query params
    this.pageTitle = this.pageTitles[path] || (this.isAdmin ? 'Admin Panel' : 'SmartSure');
  }

  toggleSidebar(): void {
    this.sidebarOpen = !this.sidebarOpen;
  }

  closeSidebar(): void {
    this.sidebarOpen = false;
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }

  @HostListener('window:resize')
  onResize(): void {
    if (window.innerWidth >= 992) {
      this.sidebarOpen = false;
    }
  }
}
