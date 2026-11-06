import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { APP_ENVIRONMENT } from '@prorata-manager/shared/environment';
import { Observable } from 'rxjs';
import {
  ContractorRequest,
  LotRequest,
  Site,
  SiteRequest,
  SiteStatus,
  SiteSummary,
} from './sites.models';

/** Sites of the user's organization; every change returns the whole updated site. */
@Injectable({ providedIn: 'root' })
export class SitesService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(APP_ENVIRONMENT).apiBaseUrl}/sites`;

  list(): Observable<SiteSummary[]> {
    return this.http.get<SiteSummary[]>(this.url);
  }

  get(id: string): Observable<Site> {
    return this.http.get<Site>(`${this.url}/${id}`);
  }

  create(request: SiteRequest): Observable<Site> {
    return this.http.post<Site>(this.url, request);
  }

  update(id: string, request: SiteRequest): Observable<Site> {
    return this.http.put<Site>(`${this.url}/${id}`, request);
  }

  changeStatus(id: string, status: SiteStatus | ''): Observable<Site> {
    return this.http.put<Site>(`${this.url}/${id}/status`, { status });
  }

  addLot(id: string, request: LotRequest): Observable<Site> {
    return this.http.post<Site>(`${this.url}/${id}/lots`, request);
  }

  updateLot(id: string, lotId: string, request: LotRequest): Observable<Site> {
    return this.http.put<Site>(`${this.url}/${id}/lots/${lotId}`, request);
  }

  addContractor(id: string, request: ContractorRequest): Observable<Site> {
    return this.http.post<Site>(`${this.url}/${id}/contractors`, request);
  }

  updateContractor(id: string, contractorId: string, request: ContractorRequest): Observable<Site> {
    return this.http.put<Site>(`${this.url}/${id}/contractors/${contractorId}`, request);
  }
}
