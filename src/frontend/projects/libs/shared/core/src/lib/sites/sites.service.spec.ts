import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideAppEnvironment } from '@prorata-manager/shared/environment';
import { Observable } from 'rxjs';
import { SitesService } from './sites.service';

describe('SitesService', () => {
  let service: SitesService;
  let http: HttpTestingController;

  const site = { name: 'Linden Court housing' };
  const siteRequest = {
    name: 'Linden Court housing',
    address: '12 Avenue des Tilleuls',
    projectOwner: 'Cedar Homes',
    projectManager: 'North Arc Architects',
    startDate: '2027-01-04',
    plannedHandoverDate: '2028-06-30',
  };
  const lotRequest = {
    number: '01',
    name: 'Structure',
    family: 'Structure' as const,
    contractAmount: 1845000,
  };
  const contractorRequest = {
    companyName: 'Atlas Structures',
    registrationNumber: '',
    address: '',
    contactName: '',
    contactEmail: '',
    phone: '',
    lotIds: ['lot-1'],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAppEnvironment({ apiBaseUrl: '/api' }),
      ],
    });
    service = TestBed.inject(SitesService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it.each<[string, () => Observable<unknown>, string, string, unknown]>([
    ['lists the sites', () => service.list(), 'GET', '/api/sites', null],
    ['reads a site', () => service.get('site-1'), 'GET', '/api/sites/site-1', null],
    ['creates a site', () => service.create(siteRequest), 'POST', '/api/sites', siteRequest],
    [
      'updates a site',
      () => service.update('site-1', siteRequest),
      'PUT',
      '/api/sites/site-1',
      siteRequest,
    ],
    [
      'changes the status',
      () => service.changeStatus('site-1', 'InProgress'),
      'PUT',
      '/api/sites/site-1/status',
      { status: 'InProgress' },
    ],
    [
      'adds a lot',
      () => service.addLot('site-1', lotRequest),
      'POST',
      '/api/sites/site-1/lots',
      lotRequest,
    ],
    [
      'updates a lot',
      () => service.updateLot('site-1', 'lot-1', lotRequest),
      'PUT',
      '/api/sites/site-1/lots/lot-1',
      lotRequest,
    ],
    [
      'adds a contractor',
      () => service.addContractor('site-1', contractorRequest),
      'POST',
      '/api/sites/site-1/contractors',
      contractorRequest,
    ],
    [
      'updates a contractor',
      () => service.updateContractor('site-1', 'contractor-1', contractorRequest),
      'PUT',
      '/api/sites/site-1/contractors/contractor-1',
      contractorRequest,
    ],
  ])('%s', (_case, call, method, url, body) => {
    let result: unknown;
    call().subscribe((value) => (result = value));

    const request = http.expectOne(url);
    expect(request.request.method).toBe(method);
    expect(request.request.body).toEqual(body);
    request.flush(site);

    expect(result).toEqual(site);
  });
});
