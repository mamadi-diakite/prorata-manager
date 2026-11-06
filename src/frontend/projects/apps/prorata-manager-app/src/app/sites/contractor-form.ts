import { Component, effect, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Contractor, FormSubmission, Lot, Site, SitesService } from '@prorata-manager/shared/core';
import { FieldErrors } from '@prorata-manager/shared/ui';

/** Adds a contractor to a site, or edits one, and chooses the lots it holds. */
@Component({
  selector: 'pm-contractor-form',
  imports: [ReactiveFormsModule, FieldErrors],
  template: `
    <form id="contractor-form" class="panel" [formGroup]="form" (ngSubmit)="save()" novalidate>
      <h3 class="font-semibold">
        {{ contractor() ? 'Edit ' + contractor()!.companyName : 'New contractor' }}
      </h3>

      @if (submission.errors().form; as message) {
        <p class="alert-error" role="alert">{{ message }}</p>
      }

      <div class="grid gap-4 sm:grid-cols-2">
        <div>
          <label class="form-label" for="contractor-companyName">Company name</label>
          <input id="contractor-companyName" class="form-input" formControlName="companyName" />
          <pm-field-errors [messages]="submission.messagesFor('companyName')" />
        </div>
        <div>
          <label class="form-label" for="contractor-registrationNumber">Registration number</label>
          <input
            id="contractor-registrationNumber"
            class="form-input"
            formControlName="registrationNumber"
          />
          <pm-field-errors [messages]="submission.messagesFor('registrationNumber')" />
        </div>
        <div class="sm:col-span-2">
          <label class="form-label" for="contractor-address">Address</label>
          <input id="contractor-address" class="form-input" formControlName="address" />
          <pm-field-errors [messages]="submission.messagesFor('address')" />
        </div>
        <div>
          <label class="form-label" for="contractor-contactName">Contact name</label>
          <input id="contractor-contactName" class="form-input" formControlName="contactName" />
          <pm-field-errors [messages]="submission.messagesFor('contactName')" />
        </div>
        <div>
          <label class="form-label" for="contractor-contactEmail">Contact email</label>
          <input
            id="contractor-contactEmail"
            class="form-input"
            type="email"
            formControlName="contactEmail"
          />
          <pm-field-errors [messages]="submission.messagesFor('contactEmail')" />
        </div>
        <div>
          <label class="form-label" for="contractor-phone">Phone</label>
          <input id="contractor-phone" class="form-input" type="tel" formControlName="phone" />
          <pm-field-errors [messages]="submission.messagesFor('phone')" />
        </div>
      </div>

      <fieldset>
        <legend class="form-label">Lots</legend>
        @for (lot of lots(); track lot.id) {
          <label class="mt-2 flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              [id]="'contractor-lot-' + lot.number"
              [checked]="isChosen(lot.id)"
              [disabled]="heldByOther(lot)"
              (change)="toggle(lot.id)"
            />
            {{ lot.number }} · {{ lot.name }}
            @if (heldByOther(lot)) {
              <span class="text-slate-500">(held by {{ lot.contractorName }})</span>
            }
          </label>
        } @empty {
          <p class="mt-1 text-sm text-slate-500">Add lots to the site to give them out.</p>
        }
        <pm-field-errors [messages]="submission.messagesFor('lotIds')" />
      </fieldset>

      <div class="flex gap-3">
        <button class="button-primary sm:w-auto" type="submit" [disabled]="submission.pending()">
          Save contractor
        </button>
        <button class="button-secondary" type="button" (click)="cancelled.emit()">Cancel</button>
      </div>
    </form>
  `,
})
export class ContractorForm {
  private readonly sites = inject(SitesService);

  readonly siteId = input.required<string>();
  readonly lots = input.required<readonly Lot[]>();
  readonly contractor = input<Contractor | null>(null);
  readonly saved = output<Site>();
  readonly cancelled = output();

  protected readonly form = inject(NonNullableFormBuilder).group({
    companyName: ['', Validators.required],
    registrationNumber: [''],
    address: [''],
    contactName: [''],
    contactEmail: ['', Validators.email],
    phone: [''],
    lotIds: [[] as readonly string[]],
  });
  protected readonly submission = new FormSubmission(this.form, {
    companyName: 'Enter the company name.',
    contactEmail: 'Enter a valid email address.',
  });

  constructor() {
    effect(() => {
      const contractor = this.contractor();
      if (contractor) {
        this.form.setValue({
          companyName: contractor.companyName,
          registrationNumber: contractor.registrationNumber ?? '',
          address: contractor.address ?? '',
          contactName: contractor.contactName ?? '',
          contactEmail: contractor.contactEmail ?? '',
          phone: contractor.phone ?? '',
          lotIds: contractor.lotIds,
        });
      } else {
        this.form.reset();
      }
    });
  }

  protected isChosen(lotId: string): boolean {
    return this.form.controls.lotIds.value.includes(lotId);
  }

  protected heldByOther(lot: Lot): boolean {
    return lot.contractorId !== null && lot.contractorId !== this.contractor()?.id;
  }

  protected toggle(lotId: string): void {
    const lotIds = this.form.controls.lotIds.value;
    this.form.controls.lotIds.setValue(
      lotIds.includes(lotId) ? lotIds.filter((id) => id !== lotId) : [...lotIds, lotId],
    );
  }

  protected save(): void {
    const contractor = this.contractor();
    const request = this.form.getRawValue();
    this.submission.submit(
      () =>
        contractor
          ? this.sites.updateContractor(this.siteId(), contractor.id, request)
          : this.sites.addContractor(this.siteId(), request),
      (site) => this.saved.emit(site),
    );
  }
}
