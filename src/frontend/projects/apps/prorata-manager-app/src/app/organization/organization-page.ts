import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  FormSubmission,
  MANAGER_KINDS,
  ManagerKind,
  Organization,
  OrganizationService,
  SessionStore,
} from '@prorata-manager/shared/core';
import { FieldErrors } from '@prorata-manager/shared/ui';

@Component({
  selector: 'pm-organization-page',
  imports: [ReactiveFormsModule, FieldErrors],
  template: `
    @if (loadFailed()) {
      <p class="alert-error" role="alert">
        The organization could not be loaded. Please try again.
      </p>
    } @else if (!loaded()) {
      <p class="text-slate-600" role="status">Loading…</p>
    } @else {
      <form class="form-card" [formGroup]="form" (ngSubmit)="save()" novalidate>
        <h1 class="text-2xl font-bold tracking-tight">Organization</h1>

        @if (saved()) {
          <p class="alert-info" role="status">The details have been saved.</p>
        }
        @if (!canEdit) {
          <p class="alert-info" data-testid="read-only">
            Only a manager admin can change these details.
          </p>
        }
        @if (submission.errors().form; as message) {
          <p class="alert-error" role="alert">{{ message }}</p>
        }

        <div>
          <label class="form-label" for="name">Name</label>
          <input id="name" class="form-input" formControlName="name" autocomplete="organization" />
          <pm-field-errors [messages]="submission.messagesFor('name')" />
        </div>

        <fieldset>
          <legend class="form-label">Account manager</legend>
          @for (kind of kinds; track kind.value) {
            <label class="mt-2 flex items-center gap-2 text-sm">
              <input
                type="radio"
                [id]="'kind-' + kind.value"
                formControlName="kind"
                [value]="kind.value"
              />
              {{ kind.label }}
            </label>
          }
          <pm-field-errors [messages]="submission.messagesFor('kind')" />
        </fieldset>

        <div>
          <label class="form-label" for="registrationNumber">Registration number</label>
          <input id="registrationNumber" class="form-input" formControlName="registrationNumber" />
          <pm-field-errors [messages]="submission.messagesFor('registrationNumber')" />
        </div>

        <div>
          <label class="form-label" for="address">Address</label>
          <input
            id="address"
            class="form-input"
            formControlName="address"
            autocomplete="street-address"
          />
          <pm-field-errors [messages]="submission.messagesFor('address')" />
        </div>

        <div>
          <label class="form-label" for="phone">Phone</label>
          <input
            id="phone"
            class="form-input"
            type="tel"
            formControlName="phone"
            autocomplete="tel"
          />
          <pm-field-errors [messages]="submission.messagesFor('phone')" />
        </div>

        @if (canEdit) {
          <button class="button-primary" type="submit" [disabled]="submission.pending()">
            Save details
          </button>
        }
      </form>
    }
  `,
})
export class OrganizationPage {
  private readonly organizations = inject(OrganizationService);

  protected readonly kinds = MANAGER_KINDS;
  protected readonly canEdit = inject(SessionStore).user()!.role === 'ManagerAdmin';
  protected readonly loaded = signal(false);
  protected readonly loadFailed = signal(false);
  protected readonly saved = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', Validators.required],
    kind: ['' as ManagerKind | '', Validators.required],
    registrationNumber: [''],
    address: [''],
    phone: [''],
  });
  protected readonly submission = new FormSubmission(this.form, {
    name: 'Enter the organization name.',
    kind: 'Say whether the organization is a contractor or a third party.',
  });

  constructor() {
    this.organizations.get().subscribe({
      next: (organization) => {
        this.show(organization);
        if (!this.canEdit) {
          this.form.disable();
        }
        this.loaded.set(true);
      },
      error: () => this.loadFailed.set(true),
    });
  }

  protected save(): void {
    this.saved.set(false);
    this.submission.submit(
      () => this.organizations.update(this.form.getRawValue()),
      (organization) => {
        this.show(organization);
        this.saved.set(true);
      },
    );
  }

  private show(organization: Organization): void {
    this.form.setValue({
      name: organization.name,
      kind: organization.kind,
      registrationNumber: organization.registrationNumber ?? '',
      address: organization.address ?? '',
      phone: organization.phone ?? '',
    });
  }
}
