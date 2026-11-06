import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import {
  labelOf,
  SessionStore,
  SITE_STATUSES,
  SitesService,
  SiteStatus,
  withLoadState,
} from '@prorata-manager/shared/core';

/** The sites of the organization with their status and contract amounts. */
@Component({
  selector: 'pm-sites-page',
  imports: [CurrencyPipe, DatePipe, RouterLink],
  template: `
    <section class="space-y-6">
      <div class="flex flex-wrap items-center justify-between gap-4">
        <h1 class="text-2xl font-bold tracking-tight">Sites</h1>
        @if (isAdmin()) {
          <a routerLink="/sites/new" class="button-secondary">New site</a>
        }
      </div>

      @let current = state();
      @if (current.status === 'loading') {
        <p class="text-slate-600" role="status">Loading the sites…</p>
      } @else if (current.status === 'ready') {
        @if (current.value.length === 0) {
          <p class="text-slate-600" data-testid="no-sites">No sites yet.</p>
        } @else {
          <div class="panel overflow-x-auto">
            <table class="data-table">
              <thead>
                <tr>
                  <th>Site</th>
                  <th>Status</th>
                  <th>Start</th>
                  <th>Planned handover</th>
                  <th class="text-right">Lots</th>
                  <th class="text-right">Contractors</th>
                  <th class="text-right">Contract amount</th>
                </tr>
              </thead>
              <tbody>
                @for (site of current.value; track site.id) {
                  <tr>
                    <td>
                      <a class="link" [routerLink]="['/sites', site.id]">{{ site.name }}</a>
                      <p class="text-xs text-slate-500">{{ site.address }}</p>
                    </td>
                    <td>{{ statusLabel(site.status) }}</td>
                    <td>{{ site.startDate | date: 'mediumDate' }}</td>
                    <td>{{ site.plannedHandoverDate | date: 'mediumDate' }}</td>
                    <td class="text-right">{{ site.lotCount }}</td>
                    <td class="text-right">{{ site.contractorCount }}</td>
                    <td class="text-right">{{ site.totalContractAmount | currency: 'EUR' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      } @else {
        <p class="alert-error" role="alert">The sites could not be loaded. Please try again.</p>
      }
    </section>
  `,
})
export class SitesPage {
  protected readonly isAdmin = inject(SessionStore).isAdmin;
  protected readonly state = toSignal(withLoadState(inject(SitesService).list()), {
    requireSync: true,
  });

  protected statusLabel(status: SiteStatus): string {
    return labelOf(SITE_STATUSES, status);
  }
}
