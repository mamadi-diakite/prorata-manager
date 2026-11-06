import { Component, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  AuthService,
  FormSubmission,
  MANAGER_KINDS,
  ManagerKind,
} from '@prorata-manager/shared/core';
import { FieldErrors } from '@prorata-manager/shared/ui';

@Component({
  selector: 'pm-sign-up-page',
  imports: [ReactiveFormsModule, RouterLink, FieldErrors],
  template: `
    <form class="form-card" [formGroup]="form" (ngSubmit)="signUp()" novalidate>
      <h1 class="text-2xl font-bold tracking-tight">Sign up your organization</h1>

      @if (submission.errors().form; as message) {
        <p class="alert-error" role="alert">{{ message }}</p>
      }

      <p class="text-sm text-slate-600">
        Your organization runs the shared-cost account of its sites. You become its admin.
      </p>

      <div>
        <label class="form-label" for="organizationName">Organization name</label>
        <input
          id="organizationName"
          class="form-input"
          formControlName="organizationName"
          autocomplete="organization"
        />
        <pm-field-errors [messages]="submission.messagesFor('organizationName')" />
      </div>

      <fieldset>
        <legend class="form-label">Your organization is</legend>
        @for (kind of kinds; track kind.value) {
          <label class="mt-2 flex items-center gap-2 text-sm">
            <input
              type="radio"
              [id]="'organizationKind-' + kind.value"
              formControlName="organizationKind"
              [value]="kind.value"
            />
            {{ kind.label }}
          </label>
        }
        <pm-field-errors [messages]="submission.messagesFor('organizationKind')" />
      </fieldset>

      <div>
        <label class="form-label" for="displayName">Your name</label>
        <input
          id="displayName"
          class="form-input"
          formControlName="displayName"
          autocomplete="name"
        />
        <pm-field-errors [messages]="submission.messagesFor('displayName')" />
      </div>

      <div>
        <label class="form-label" for="email">Email</label>
        <input
          id="email"
          class="form-input"
          type="email"
          formControlName="email"
          autocomplete="email"
        />
        <pm-field-errors [messages]="submission.messagesFor('email')" />
      </div>

      <div>
        <label class="form-label" for="password">Password</label>
        <input
          id="password"
          class="form-input"
          type="password"
          formControlName="password"
          autocomplete="new-password"
        />
        <p class="mt-1 text-xs text-slate-500">At least 8 characters, with a letter and a digit.</p>
        <pm-field-errors [messages]="submission.messagesFor('password')" />
      </div>

      <div>
        <label class="form-label" for="confirmPassword">Confirm password</label>
        <input
          id="confirmPassword"
          class="form-input"
          type="password"
          formControlName="confirmPassword"
          autocomplete="new-password"
        />
        <pm-field-errors [messages]="submission.messagesFor('confirmPassword')" />
      </div>

      <button class="button-primary" type="submit" [disabled]="submission.pending()">
        Create my organization
      </button>

      <p class="text-center text-sm text-slate-600">
        Already have an account?
        <a routerLink="/login" class="font-medium text-indigo-600">Log in</a>
      </p>
    </form>
  `,
})
export class SignUpPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly kinds = MANAGER_KINDS;
  protected readonly form = inject(NonNullableFormBuilder).group({
    organizationName: ['', Validators.required],
    organizationKind: ['' as ManagerKind | '', Validators.required],
    displayName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    confirmPassword: ['', Validators.required],
  });
  protected readonly submission = new FormSubmission(this.form, {
    organizationName: 'Enter your organization name.',
    organizationKind: 'Say whether your organization is a contractor or a third party.',
    displayName: 'Enter your name.',
    email: 'Enter a valid email address.',
    password: 'Choose a password.',
    confirmPassword: 'Confirm your password.',
  });

  protected signUp(): void {
    this.submission.submit(
      () => this.auth.signUp(this.form.getRawValue()),
      () => void this.router.navigateByUrl('/'),
    );
  }
}
