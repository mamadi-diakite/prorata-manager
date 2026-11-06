import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { APP_ENVIRONMENT } from '@prorata-manager/shared/environment';
import { Observable } from 'rxjs';
import { Organization, UpdateOrganizationRequest } from './organization.models';

/** The manager organization of the signed-in user. */
@Injectable({ providedIn: 'root' })
export class OrganizationService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(APP_ENVIRONMENT).apiBaseUrl}/organization`;

  get(): Observable<Organization> {
    return this.http.get<Organization>(this.url);
  }

  update(request: UpdateOrganizationRequest): Observable<Organization> {
    return this.http.put<Organization>(this.url, request);
  }
}
