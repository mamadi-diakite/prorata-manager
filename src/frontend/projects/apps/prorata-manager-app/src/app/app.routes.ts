import { Routes } from '@angular/router';
import { authGuard, guestGuard } from '@prorata-manager/shared/core';
import { ChangePasswordPage } from './auth/change-password-page';
import { LoginPage } from './auth/login-page';
import { SignUpPage } from './auth/sign-up-page';
import { HomePage } from './home/home-page';
import { OrganizationPage } from './organization/organization-page';

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
  { path: '**', redirectTo: '' },
];
