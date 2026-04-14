import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { 
    path: '', 
    loadComponent: () => import('./pages/landing/landing.component').then(m => m.LandingComponent) 
  },
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./pages/register/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./pages/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent)
  },
  {
    path: 'verify-email',
    loadComponent: () => import('./pages/verify-email/verify-email.component').then(m => m.VerifyEmailComponent)
  },
  {
    path: 'auth/google/callback',
    loadComponent: () => import('./pages/google-callback/google-callback.component').then(m => m.GoogleCallbackComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./components/shell/shell.component').then(m => m.ShellComponent),
    children: [
      {
        path: 'claims/initiate',
        canActivate: [authGuard],
        loadComponent: () => import('./pages/claims/initiate-claim/initiate-claim.component').then(m => m.InitiateClaimComponent)
      },
      {
        path: 'new-policy',
        canActivate: [authGuard],
        loadComponent: () => import('./pages/policies/buy-policy/buy-policy.component').then(m => m.BuyPolicyComponent),
        title: 'Buy New Policy'
      },
      {
        path: 'admin/audit-logs',
        canActivate: [authGuard],
        loadComponent: () => import('./pages/admin/audit-logs/audit-logs.component').then(m => m.AuditLogComponent),
        title: 'Audit Logs'
      },
      {
        path: 'admin/system-logs',
        canActivate: [authGuard],
        loadComponent: () => import('./pages/admin/system-logs/system-logs.component').then(m => m.SystemLogsComponent),
        title: 'System Logs'
      },

      {
        path: 'admin/users',
        canActivate: [authGuard],
        loadComponent: () => import('./pages/admin/manage-users/manage-users.component').then(m => m.ManageUsersComponent),
        title: 'Manage Users'
      },
      {
        path: 'admin/insurance',
        canActivate: [authGuard],
        loadComponent: () => import('./pages/admin/manage-insurance/manage-insurance.component').then(m => m.ManageInsuranceComponent),
        title: 'Manage Insurance'
      },
      {
        path: 'admin/reports',
        canActivate: [authGuard],
        loadComponent: () => import('./pages/admin/reports/reports.component').then(m => m.ReportsComponent),
        title: 'Reports'
      },
      {
        path: 'admin/policies',
        canActivate: [authGuard],
        loadComponent: () => import('./pages/admin/manage-policies/manage-policies.component').then(m => m.ManagePoliciesComponent),
        title: 'Manage Policies'
      },
      {
        path: 'admin/claims',
        canActivate: [authGuard],
        loadComponent: () => import('./pages/admin/manage-claims/manage-claims.component').then(m => m.ManageClaimsComponent),
        title: 'Manage Claims'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent),
        title: 'Dashboard'
      },
      {
        path: 'policies',
        loadComponent: () => import('./pages/policies/policies.component').then(m => m.PoliciesComponent),
        title: 'My Policies'
      },
      {
        path: 'claims',
        loadComponent: () => import('./pages/claims/claims.component').then(m => m.ClaimsComponent),
        title: 'My Claims'
      },
      {
        path: 'profile',
        loadComponent: () => import('./pages/profile/profile.component').then(m => m.ProfileComponent),
        title: 'Profile'
      },
      {
        path: 'ai-assistant',
        canActivate: [authGuard],
        loadComponent: () => import('./pages/ai-assistant/ai-assistant.component').then(m => m.AiAssistantComponent),
        title: 'AI Assistant'
      }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
