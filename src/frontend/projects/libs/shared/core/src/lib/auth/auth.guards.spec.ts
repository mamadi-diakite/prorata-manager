import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  provideRouter,
  Router,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { adminGuard, authGuard, guestGuard, safeReturnUrl } from './auth.guards';
import { UserRole } from './auth.models';
import { SessionStore } from './session.store';

describe('auth guards', () => {
  const route = {} as ActivatedRouteSnapshot;
  const state = { url: '/account/password' } as RouterStateSnapshot;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
  });

  function signIn(role: UserRole = 'ManagerAdmin'): void {
    TestBed.inject(SessionStore).start({
      accessToken: 'token-1',
      expiresAtUtc: '2999-01-01T00:00:00Z',
      user: {
        id: 'user-1',
        email: 'claire.martin@example.com',
        displayName: 'Claire Martin',
        role,
        organizationId: 'organization-1',
      },
    });
  }

  it('sends visitors of admin pages to login', () => {
    const result = TestBed.runInInjectionContext(() => adminGuard(route, state));

    expect(serialize(result)).toBe('/login?returnUrl=%2Faccount%2Fpassword');
  });

  it('lets admins through admin pages', () => {
    signIn();

    expect(TestBed.runInInjectionContext(() => adminGuard(route, state))).toBe(true);
  });

  it('sends staff members from admin pages to the site list', () => {
    signIn('ManagerStaff');

    expect(serialize(TestBed.runInInjectionContext(() => adminGuard(route, state)))).toBe('/sites');
  });

  function serialize(result: unknown): string {
    return TestBed.inject(Router).serializeUrl(result as UrlTree);
  }

  it('sends visitors to login with the page they wanted', () => {
    const result = TestBed.runInInjectionContext(() => authGuard(route, state));

    expect(serialize(result)).toBe('/login?returnUrl=%2Faccount%2Fpassword');
  });

  it('lets signed-in users through protected pages', () => {
    signIn();

    expect(TestBed.runInInjectionContext(() => authGuard(route, state))).toBe(true);
  });

  it('lets visitors open the login page', () => {
    expect(TestBed.runInInjectionContext(() => guestGuard(route, state))).toBe(true);
  });

  it('sends signed-in users home from the login page', () => {
    signIn();

    expect(serialize(TestBed.runInInjectionContext(() => guestGuard(route, state)))).toBe('/');
  });

  it.each([
    ['/account/password', '/account/password'],
    ['//evil.example.com', '/'],
    ['https://evil.example.com', '/'],
    [null, '/'],
    [undefined, '/'],
  ])('keeps the return address %s inside the app', (returnUrl, expected) => {
    expect(safeReturnUrl(returnUrl)).toBe(expected);
  });
});
