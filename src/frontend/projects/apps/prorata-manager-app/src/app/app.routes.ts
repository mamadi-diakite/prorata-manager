import { Routes } from '@angular/router';
import { adminGuard, authGuard, guestGuard } from '@prorata-manager/shared/core';
import { ChangePasswordPage } from './auth/change-password-page';
import { LoginPage } from './auth/login-page';
import { SignUpPage } from './auth/sign-up-page';
import { HomePage } from './home/home-page';
import { OrganizationPage } from './organization/organization-page';
import { SiteFormPage } from './sites/site-form-page';
import { SitePage } from './sites/site-page';
import { SitesPage } from './sites/sites-page';

export const routes: Routes = [
  { path: '', component: HomePage, title: 'Prorata Manager' },
  {
    path: 'login',
    component: LoginPage,
    canActivate: [guestGuard],
    title: 'Log in · Prorata Manager',
  },
  {
    path: 'sign-up',
    component: SignUpPage,
    canActivate: [guestGuard],
    title: 'Sign up · Prorata Manager',
  },
  {
    path: 'account/password',
    component: ChangePasswordPage,
    canActivate: [authGuard],
    title: 'Change password · Prorata Manager',
  },
  {
    path: 'organization',
    component: OrganizationPage,
    canActivate: [authGuard],
    title: 'Organization · Prorata Manager',
  },
  {
    path: 'sites',
    component: SitesPage,
    canActivate: [authGuard],
    title: 'Sites · Prorata Manager',
  },
  {
    path: 'sites/new',
    component: SiteFormPage,
    canActivate: [adminGuard],
    title: 'New site · Prorata Manager',
  },
  {
    path: 'sites/:id',
    component: SitePage,
    canActivate: [authGuard],
    title: 'Site · Prorata Manager',
  },
  {
    path: 'sites/:id/edit',
    component: SiteFormPage,
    canActivate: [authGuard],
    title: 'Edit site · Prorata Manager',
  },
  { path: '**', redirectTo: '' },
];
