import { Component, effect, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  FormSubmission,
  Lot,
  LOT_FAMILIES,
  LotFamily,
  Site,
  SitesService,
} from '@prorata-manager/shared/core';
import { FieldErrors } from '@prorata-manager/shared/ui';

/** Euros with two decimals at most; a comma is accepted as the decimal separator. */
const AMOUNT_PATTERN = /^\s*\d+([.,]\d{1,2})?\s*$/;

export function toAmount(value: string): number | null {
  return value.trim() === '' ? null : Number(value.trim().replace(',', '.'));
}

/** Adds a lot to a site, or edits one, with its contract amount excluding VAT. */
@Component({
  selector: 'pm-lot-form',
  imports: [ReactiveFormsModule, FieldErrors],
  template: `
    <form id="lot-form" class="panel" [formGroup]="form" (ngSubmit)="save()" novalidate>
      <h3 class="font-semibold">{{ lot() ? 'Edit lot ' + lot()!.number : 'New lot' }}</h3>

      @if (submission.errors().form; as message) {
        <p class="alert-error" role="alert">{{ message }}</p>
      }

      <div class="grid gap-4 sm:grid-cols-2">
        <div>
          <label class="form-label" for="lot-number">Number</label>
          <input id="lot-number" class="form-input" formControlName="number" />
          <pm-field-errors [messages]="submission.messagesFor('number')" />
        </div>
        <div>
          <label class="form-label" for="lot-name">Name</label>
          <input id="lot-name" class="form-input" formControlName="name" />
          <pm-field-errors [messages]="submission.messagesFor('name')" />
        </div>
        <div>
          <label class="form-label" for="lot-family">Family</label>
          <select id="lot-family" class="form-input" formControlName="family">
            <option value="">Choose a family</option>
            @for (family of families; track family.value) {
              <option [value]="family.value">{{ family.label }}</option>
            }
          </select>
          <pm-field-errors [messages]="submission.messagesFor('family')" />
        </div>
        <div>
          <label class="form-label" for="lot-contractAmount">Contract amount (€, excl. VAT)</label>
          <input
            id="lot-contractAmount"
            class="form-input"
            inputmode="decimal"
            formControlName="contractAmount"
          />
          <p class="mt-1 text-xs text-slate-500">Leave empty until the amount is known.</p>
          <pm-field-errors [messages]="submission.messagesFor('contractAmount')" />
        </div>
      </div>

      <div class="flex gap-3">
        <button class="button-primary sm:w-auto" type="submit" [disabled]="submission.pending()">
          Save lot
        </button>
        <button class="button-secondary" type="button" (click)="cancelled.emit()">Cancel</button>
      </div>
    </form>
  `,
})
export class LotForm {
  private readonly sites = inject(SitesService);

  readonly siteId = input.required<string>();
  readonly lot = input<Lot | null>(null);
  readonly saved = output<Site>();
  readonly cancelled = output();

  protected readonly families = LOT_FAMILIES;
  protected readonly form = inject(NonNullableFormBuilder).group({
    number: ['', Validators.required],
    name: ['', Validators.required],
    family: ['' as LotFamily | '', Validators.required],
    contractAmount: ['', Validators.pattern(AMOUNT_PATTERN)],
  });
  protected readonly submission = new FormSubmission(this.form, {
    number: 'Enter the lot number.',
    name: 'Enter the lot name.',
    family: 'Choose a family.',
    contractAmount: 'Enter a positive amount in euros, with two decimals at most.',
  });

  constructor() {
    effect(() => {
      const lot = this.lot();
      if (lot) {
        this.form.setValue({
          number: lot.number,
          name: lot.name,
          family: lot.family,
          contractAmount: lot.contractAmount === null ? '' : String(lot.contractAmount),
        });
      } else {
        this.form.reset();
      }
    });
  }

  protected save(): void {
    const value = this.form.getRawValue();
    const request = { ...value, contractAmount: toAmount(value.contractAmount) };
    const lot = this.lot();
    this.submission.submit(
      () =>
        lot
          ? this.sites.updateLot(this.siteId(), lot.id, request)
          : this.sites.addLot(this.siteId(), request),
      (site) => this.saved.emit(site),
    );
  }
}
