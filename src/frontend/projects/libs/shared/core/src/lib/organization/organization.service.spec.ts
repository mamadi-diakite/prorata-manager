import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideAppEnvironment } from '@prorata-manager/shared/environment';
import { firstValueFrom } from 'rxjs';
import { Organization } from './organization.models';
import { OrganizationService } from './organization.service';

const organization: Organization = {
  id: 'organization-1',
  name: 'Atlas Structures',
  kind: 'Contractor',
  registrationNumber: null,
  address: null,
  phone: null,
};

describe('OrganizationService', () => {
  let service: OrganizationService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAppEnvironment({ apiBaseUrl: '/api' }),
      ],
    });
    service = TestBed.inject(OrganizationService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('reads the organization', async () => {
    const result = firstValueFrom(service.get());

    const request = http.expectOne('/api/organization');
    expect(request.request.method).toBe('GET');
    request.flush(organization);

    expect(await result).toEqual(organization);
  });

  it('saves the details', async () => {
    const body = {
      name: 'Prism Site Services',
      kind: 'ThirdParty' as const,
      registrationNumber: '',
      address: '',
      phone: '',
    };
    const result = firstValueFrom(service.update(body));

    const request = http.expectOne('/api/organization');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(body);
    request.flush({ ...organization, name: 'Prism Site Services', kind: 'ThirdParty' });

    expect((await result).kind).toBe('ThirdParty');
  });
});
