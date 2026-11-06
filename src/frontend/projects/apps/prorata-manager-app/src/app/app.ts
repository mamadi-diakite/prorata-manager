import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { ApiHealthService, AuthService, SessionStore } from '@prorata-manager/shared/core';
import { Shell } from '@prorata-manager/shared/ui';

@Component({
  selector: 'pm-root',
  imports: [RouterOutlet, RouterLink, Shell],
  template: `
    <pm-shell appName="Prorata Manager" [apiStatus]="apiStatus()">
      <ng-container shellNav>
        @if (user(); as user) {
          <span class="text-slate-600" data-testid="user-name">{{ user.displayName }}</span>
          <a routerLink="/organization" class="text-indigo-600">Organization</a>
          <a routerLink="/account/password" class="text-indigo-600">Change password</a>
          <button type="button" class="text-indigo-600" (click)="logOut()">Log out</button>
        } @else {
          <a routerLink="/login" class="text-indigo-600">Log in</a>
          <a routerLink="/sign-up" class="text-indigo-600">Sign up</a>
        }
      </ng-container>
      <router-outlet />
    </pm-shell>
  `,
})
export class App {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly user = inject(SessionStore).user;
  protected readonly apiStatus = toSignal(inject(ApiHealthService).check(), {
    initialValue: 'checking',
  });

  protected logOut(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
