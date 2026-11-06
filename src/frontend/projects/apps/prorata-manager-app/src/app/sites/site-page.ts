import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  Contractor,
  FormSubmission,
  labelOf,
  laterStatuses,
  LoadState,
  Lot,
  LOT_FAMILIES,
  LotFamily,
  Site,
  SITE_STATUSES,
  SitesService,
  SiteStatus,
  withLoadState,
} from '@prorata-manager/shared/core';
import { FieldErrors } from '@prorata-manager/shared/ui';
import { switchMap } from 'rxjs';
import { ContractorForm } from './contractor-form';
import { LotForm } from './lot-form';

/** One site with its status, lots, contractors and contract amounts. */
@Component({
  selector: 'pm-site-page',
  imports: [
    CurrencyPipe,
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    FieldErrors,
    LotForm,
    ContractorForm,
  ],
  template: `
    @let current = state();
    @if (current.status === 'loading') {
      <p class="text-slate-600" role="status">Loading the site…</p>
    } @else if (current.status === 'missing') {
      <p class="alert-error" role="alert">This site does not exist.</p>
    } @else if (current.status === 'error') {
      <p class="alert-error" role="alert">The site could not be loaded. Please try again.</p>
    } @else {
      @let site = current.value;
      <section class="space-y-8">
        <div class="flex flex-wrap items-start justify-between gap-4">
          <div class="space-y-1">
            <a routerLink="/sites" class="link text-sm">All sites</a>
            <h1 class="text-2xl font-bold tracking-tight">{{ site.name }}</h1>
            <p class="text-slate-600" data-testid="site-address">{{ site.address }}</p>
          </div>
          <a class="button-secondary" [routerLink]="['/sites', site.id, 'edit']">Edit site</a>
        </div>

        <dl class="panel grid gap-4 sm:grid-cols-3" data-testid="site-details">
          <div>
            <dt class="text-sm text-slate-500">Status</dt>
            <dd data-testid="site-status">{{ statusLabel(site.status) }}</dd>
          </div>
          <div>
            <dt class="text-sm text-slate-500">Project owner</dt>
            <dd>{{ site.projectOwner }}</dd>
          </div>
          <div>
            <dt class="text-sm text-slate-500">Project manager</dt>
            <dd>{{ site.projectManager }}</dd>
          </div>
          <div>
            <dt class="text-sm text-slate-500">Start</dt>
            <dd>{{ site.startDate | date: 'mediumDate' }}</dd>
          </div>
          <div>
            <dt class="text-sm text-slate-500">Planned handover</dt>
            <dd>{{ site.plannedHandoverDate | date: 'mediumDate' }}</dd>
          </div>
        </dl>

        @if (nextStatuses(site.status).length > 0) {
          <form
            id="status-form"
            class="panel flex flex-wrap items-end gap-4"
            [formGroup]="statusForm"
            (ngSubmit)="changeStatus(site)"
            novalidate
          >
            <div>
              <label class="form-label" for="status">Move the site to</label>
              <select id="status" class="form-input" formControlName="status">
                <option value="">Choose a status</option>
                @for (option of nextStatuses(site.status); track option.value) {
                  <option [value]="option.value">{{ option.label }}</option>
                }
              </select>
              <pm-field-errors [messages]="statusSubmission.messagesFor('status')" />
            </div>
            <button class="button-secondary" type="submit" [disabled]="statusSubmission.pending()">
              Change status
            </button>
            @if (statusSubmission.errors().form; as message) {
              <p class="alert-error w-full" role="alert">{{ message }}</p>
            }
          </form>
        }

        <section class="space-y-3" aria-labelledby="totals-title">
          <h2 id="totals-title" class="text-lg font-semibold">Contract amounts (excl. VAT)</h2>
          <dl class="panel grid gap-4 sm:grid-cols-4" data-testid="totals">
            <div>
              <dt class="text-sm text-slate-500">Total</dt>
              <dd class="font-semibold">{{ site.totals.total | currency: 'EUR' }}</dd>
            </div>
            @for (family of families; track family.value) {
              <div>
                <dt class="text-sm text-slate-500">{{ family.label }}</dt>
                <dd>{{ familyTotal(site, family.value) | currency: 'EUR' }}</dd>
              </div>
            }
          </dl>
          @if (site.totals.lotsWithoutAmount > 0 || site.totals.lotsWithoutContractor > 0) {
            <p class="alert-info" data-testid="missing">
              {{ site.totals.lotsWithoutAmount }} without a contract amount ·
              {{ site.totals.lotsWithoutContractor }} without a contractor
            </p>
          }
        </section>

        <section class="space-y-3" aria-labelledby="lots-title">
          <div class="flex items-center justify-between gap-4">
            <h2 id="lots-title" class="text-lg font-semibold">Lots</h2>
            <button
              type="button"
              class="button-secondary"
              data-testid="add-lot"
              (click)="editLot(null)"
            >
              Add lot
            </button>
          </div>
          @if (lotEditor(); as editor) {
            <pm-lot-form
              [siteId]="site.id"
              [lot]="editor.lot"
              (saved)="show($event)"
              (cancelled)="lotEditor.set(null)"
            />
          }
          @if (site.lots.length === 0) {
            <p class="text-slate-600" data-testid="no-lots">No lots yet.</p>
          } @else {
            <div class="panel overflow-x-auto">
              <table class="data-table" data-testid="lots">
                <thead>
                  <tr>
                    <th>Lot</th>
                    <th>Family</th>
                    <th>Contractor</th>
                    <th class="text-right">Contract amount</th>
                    <th><span class="sr-only">Actions</span></th>
                  </tr>
                </thead>
                <tbody>
                  @for (lot of site.lots; track lot.id) {
                    <tr>
                      <td>{{ lot.number }} · {{ lot.name }}</td>
                      <td>{{ familyLabel(lot.family) }}</td>
                      <td>{{ lot.contractorName ?? 'No contractor yet' }}</td>
                      <td class="text-right">
                        @if (lot.contractAmount === null) {
                          <span class="text-slate-500">Not recorded</span>
                        } @else {
                          {{ lot.contractAmount | currency: 'EUR' }}
                        }
                      </td>
                      <td class="text-right">
                        <button
                          type="button"
                          class="link"
                          [attr.data-testid]="'edit-lot-' + lot.number"
                          (click)="editLot(lot)"
                        >
                          Edit
                        </button>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </section>

        <section class="space-y-3" aria-labelledby="contractors-title">
          <div class="flex items-center justify-between gap-4">
            <h2 id="contractors-title" class="text-lg font-semibold">Contractors</h2>
            <button
              type="button"
              class="button-secondary"
              data-testid="add-contractor"
              (click)="editContractor(null)"
            >
              Add contractor
            </button>
          </div>
          @if (contractorEditor(); as editor) {
            <pm-contractor-form
              [siteId]="site.id"
              [lots]="site.lots"
              [contractor]="editor.contractor"
              (saved)="show($event)"
              (cancelled)="contractorEditor.set(null)"
            />
          }
          <ul class="grid gap-3 md:grid-cols-2" data-testid="contractors">
            @for (contractor of site.contractors; track contractor.id) {
              <li class="panel space-y-1">
                <div class="flex items-start justify-between gap-2">
                  <p class="font-medium">{{ contractor.companyName }}</p>
                  <button
                    type="button"
                    class="link text-sm"
                    [attr.data-testid]="'edit-contractor-' + $index"
                    (click)="editContractor(contractor)"
                  >
                    Edit
                  </button>
                </div>
                @if (contractor.contactName || contractor.contactEmail) {
                  <p class="text-sm text-slate-600" data-testid="contact">
                    {{ contractor.contactName }} {{ contractor.contactEmail }}
                  </p>
                }
                <p class="text-sm" data-testid="held-lots">{{ heldLots(site, contractor) }}</p>
                <p class="text-sm font-medium">{{ contractor.contractAmount | currency: 'EUR' }}</p>
              </li>
            } @empty {
              <li class="text-slate-600">No contractors yet.</li>
            }
          </ul>
        </section>
      </section>
    }
  `,
})
export class SitePage {
  private readonly sites = inject(SitesService);

  protected readonly families = LOT_FAMILIES;
  protected readonly nextStatuses = laterStatuses;
  protected readonly state = signal<LoadState<Site>>({ status: 'loading' });
  protected readonly lotEditor = signal<{ readonly lot: Lot | null } | null>(null);
  protected readonly contractorEditor = signal<{ readonly contractor: Contractor | null } | null>(
    null,
  );
  protected readonly statusForm = inject(NonNullableFormBuilder).group({
    status: ['' as SiteStatus | '', Validators.required],
  });
  protected readonly statusSubmission = new FormSubmission(this.statusForm, {
    status: 'Choose the next status.',
  });

  constructor() {
    inject(ActivatedRoute)
      .paramMap.pipe(
        switchMap((params) => withLoadState(this.sites.get(params.get('id')!))),
        takeUntilDestroyed(),
      )
      .subscribe((state) => this.state.set(state));
  }

  protected statusLabel(status: SiteStatus): string {
    return labelOf(SITE_STATUSES, status);
  }

  protected familyLabel(family: LotFamily): string {
    return labelOf(LOT_FAMILIES, family);
  }

  protected familyTotal(site: Site, family: LotFamily): number {
    return {
      Structure: site.totals.structure,
      FinishingWorks: site.totals.finishingWorks,
      BuildingServices: site.totals.buildingServices,
    }[family];
  }

  protected heldLots(site: Site, contractor: Contractor): string {
    const numbers = site.lots
      .filter((lot) => contractor.lotIds.includes(lot.id))
      .map((lot) => lot.number);
    return numbers.length > 0 ? `Lots ${numbers.join(', ')}` : 'No lots yet';
  }

  protected editLot(lot: Lot | null): void {
    this.contractorEditor.set(null);
    this.lotEditor.set({ lot });
  }

  protected editContractor(contractor: Contractor | null): void {
    this.lotEditor.set(null);
    this.contractorEditor.set({ contractor });
  }

  protected changeStatus(site: Site): void {
    this.statusSubmission.submit(
      () => this.sites.changeStatus(site.id, this.statusForm.getRawValue().status),
      (updated) => this.show(updated),
    );
  }

  protected show(site: Site): void {
    this.state.set({ status: 'ready', value: site });
    this.lotEditor.set(null);
    this.contractorEditor.set(null);
    this.statusForm.reset();
  }
}
