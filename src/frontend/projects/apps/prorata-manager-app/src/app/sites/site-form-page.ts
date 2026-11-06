import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  FormSubmission,
  LoadState,
  Site,
  SitesService,
  withLoadState,
} from '@prorata-manager/shared/core';
import { FieldErrors } from '@prorata-manager/shared/ui';
import { of } from 'rxjs';

/** Creates a site, or edits one when the route has an id. */
@Component({
  selector: 'pm-site-form-page',
  imports: [ReactiveFormsModule, RouterLink, FieldErrors],
  template: `
    @let current = state();
    @if (current.status === 'loading') {
      <p class="text-slate-600" role="status">Loading the site…</p>
    } @else if (current.status === 'missing') {
      <p class="alert-error" role="alert">This site does not exist.</p>
    } @else if (current.status === 'error') {
      <p class="alert-error" role="alert">The site could not be loaded. Please try again.</p>
    } @else {
      <form class="form-card" [formGroup]="form" (ngSubmit)="save()" novalidate>
        <h1 class="text-2xl font-bold tracking-tight">{{ id ? 'Edit site' : 'New site' }}</h1>

        @if (submission.errors().form; as message) {
          <p class="alert-error" role="alert">{{ message }}</p>
        }

        <div>
          <label class="form-label" for="name">Site name</label>
          <input id="name" class="form-input" formControlName="name" />
          <pm-field-errors [messages]="submission.messagesFor('name')" />
        </div>

        <div>
          <label class="form-label" for="address">Address</label>
          <input id="address" class="form-input" formControlName="address" />
          <pm-field-errors [messages]="submission.messagesFor('address')" />
        </div>

        <div>
          <label class="form-label" for="projectOwner">Project owner</label>
          <input id="projectOwner" class="form-input" formControlName="projectOwner" />
          <p class="mt-1 text-xs text-slate-500">The client who orders the works.</p>
          <pm-field-errors [messages]="submission.messagesFor('projectOwner')" />
        </div>

        <div>
          <label class="form-label" for="projectManager">Project manager</label>
          <input id="projectManager" class="form-input" formControlName="projectManager" />
          <p class="mt-1 text-xs text-slate-500">
            The architect or engineer who supervises the works.
          </p>
          <pm-field-errors [messages]="submission.messagesFor('projectManager')" />
        </div>

        <div class="grid gap-4 sm:grid-cols-2">
          <div>
            <label class="form-label" for="startDate">Start date</label>
            <input id="startDate" class="form-input" type="date" formControlName="startDate" />
            <pm-field-errors [messages]="submission.messagesFor('startDate')" />
          </div>
          <div>
            <label class="form-label" for="plannedHandoverDate">Planned handover</label>
            <input
              id="plannedHandoverDate"
              class="form-input"
              type="date"
              formControlName="plannedHandoverDate"
            />
            <pm-field-errors [messages]="submission.messagesFor('plannedHandoverDate')" />
          </div>
        </div>

        <div class="flex gap-3">
          <button class="button-primary" type="submit" [disabled]="submission.pending()">
            {{ id ? 'Save site' : 'Create site' }}
          </button>
          <a class="button-secondary" [routerLink]="id ? ['/sites', id] : ['/sites']">Cancel</a>
        </div>
      </form>
    }
  `,
})
export class SiteFormPage {
  private readonly sites = inject(SitesService);
  private readonly router = inject(Router);

  protected readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id');
  protected readonly state = signal<LoadState<Site | null>>({ status: 'loading' });
  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', Validators.required],
    address: ['', Validators.required],
    projectOwner: ['', Validators.required],
    projectManager: ['', Validators.required],
    startDate: ['', Validators.required],
    plannedHandoverDate: ['', Validators.required],
  });
  protected readonly submission = new FormSubmission(this.form, {
    name: 'Enter the site name.',
    address: 'Enter the address.',
    projectOwner: 'Enter the project owner.',
    projectManager: 'Enter the project manager.',
    startDate: 'Enter the start date.',
    plannedHandoverDate: 'Enter the planned handover date.',
  });

  constructor() {
    withLoadState(this.id ? this.sites.get(this.id) : of(null))
      .pipe(takeUntilDestroyed())
      .subscribe((state) => {
        if (state.status === 'ready' && state.value) {
          const { name, address, projectOwner, projectManager, startDate, plannedHandoverDate } =
            state.value;
          this.form.setValue({
            name,
            address,
            projectOwner,
            projectManager,
            startDate,
            plannedHandoverDate,
          });
        }
        this.state.set(state);
      });
  }

  protected save(): void {
    const request = this.form.getRawValue();
    this.submission.submit(
      () => (this.id ? this.sites.update(this.id, request) : this.sites.create(request)),
      (site) => void this.router.navigate(['/sites', site.id]),
    );
  }
}
