import { Site } from '@prorata-manager/shared/core';
import { AppHarness } from '../../testing/app-harness';
import { harbour, linden } from '../../testing/site-fixtures';
import { toAmount } from './lot-form';

describe('Site page', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  async function open(site: Site = linden): Promise<void> {
    app.signIn();
    await app.open(`/sites/${site.id}`);
    app.http.expectOne(`/api/sites/${site.id}`).flush(site);
    await app.settle();
  }

  function value(id: string): string {
    return app.element.querySelector<HTMLInputElement>(`#${id}`)!.value;
  }

  function checkbox(number: string): HTMLInputElement {
    return app.element.querySelector<HTMLInputElement>(`#contractor-lot-${number}`)!;
  }

  it('needs a signed-in user', async () => {
    await app.open('/sites/site-1');

    expect(app.router.url).toBe('/login?returnUrl=%2Fsites%2Fsite-1');
  });

  it('shows the site, its totals, lots and contractors', async () => {
    app.signIn();
    await app.open('/sites/site-1');
    expect(app.text('[role="status"]')).toBe('Loading the site…');
    app.http.expectOne('/api/sites/site-1').flush(linden);
    await app.settle();

    expect(app.text('h1')).toBe('Linden Court housing');
    expect(app.text('[data-testid="site-status"]')).toBe('In progress');
    expect(app.texts('[data-testid="site-details"] dd')).toEqual([
      'In progress',
      'Cedar Homes',
      'North Arc Architects',
      'Jan 4, 2027',
      'Jun 30, 2028',
    ]);
    expect(app.texts('[data-testid="totals"] dd')).toEqual([
      '€2,163,720.50',
      '€1,845,000.00',
      '€318,720.50',
      '€0.00',
    ]);
    expect(app.text('[data-testid="missing"]')).toBe(
      '1 without a contract amount · 1 without a contractor',
    );
    expect(app.texts('[data-testid="lots"] tbody td')).toEqual([
      '01 · Earthworks and structure',
      'Structure',
      'Atlas Structures',
      '€1,845,000.00',
      'Edit',
      '05 · Partitions and ceilings',
      'Finishing works',
      'Clearwall Interiors',
      '€318,720.50',
      'Edit',
      '06 · Painting',
      'Finishing works',
      'No contractor yet',
      'Not recorded',
      'Edit',
    ]);
    expect(app.texts('[data-testid="contact"]')).toEqual([
      'Claire Martin claire.martin@example.com',
    ]);
    expect(app.texts('[data-testid="held-lots"]')).toEqual(['Lots 01', 'Lots 05']);
    expect(app.element.querySelector('a[href="/sites/site-1/edit"]')).not.toBeNull();
  });

  it('shows an empty site in preparation', async () => {
    await open(harbour);

    expect(app.text('[data-testid="no-lots"]')).toBe('No lots yet.');
    expect(app.text('[data-testid="contractors"] li')).toBe('No contractors yet.');
    expect(app.element.querySelector('[data-testid="missing"]')).toBeNull();
    expect(app.texts('#status option')).toEqual([
      'Choose a status',
      'In progress',
      'Handed over',
      'Closed',
    ]);
  });

  it('says when the site does not exist or cannot be loaded', async () => {
    app.signIn();
    await app.open('/sites/site-9');
    app.http.expectOne('/api/sites/site-9').flush(null, { status: 404, statusText: 'Not Found' });
    await app.settle();
    expect(app.text('[role="alert"]')).toBe('This site does not exist.');

    await app.open('/sites/site-8');
    app.http
      .expectOne('/api/sites/site-8')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await app.settle();
    expect(app.text('[role="alert"]')).toBe('The site could not be loaded. Please try again.');
  });

  describe('status', () => {
    it('moves the site forward', async () => {
      await open();
      expect(app.texts('#status option')).toEqual(['Choose a status', 'Handed over', 'Closed']);

      await app.select('status', 'HandedOver');
      await app.submit('#status-form');
      const request = app.http.expectOne('/api/sites/site-1/status');
      expect(request.request.body).toEqual({ status: 'HandedOver' });
      request.flush({ ...linden, status: 'HandedOver' });
      await app.settle();

      expect(app.text('[data-testid="site-status"]')).toBe('Handed over');
      expect(value('status')).toBe('');
    });

    it('asks for a status before sending', async () => {
      await open();

      await app.submit('#status-form');

      expect(app.texts('#status-form pm-field-errors p')).toEqual(['Choose the next status.']);
    });

    it('shows a general message when the API fails', async () => {
      await open();
      await app.select('status', 'Closed');

      await app.submit('#status-form');
      app.http
        .expectOne('/api/sites/site-1/status')
        .flush(null, { status: 500, statusText: 'Server Error' });
      await app.settle();

      expect(app.text('#status-form [role="alert"]')).toBe(
        'Something went wrong. Please try again.',
      );
    });

    it('is not offered once the site is closed', async () => {
      await open({ ...linden, status: 'Closed' });

      expect(app.element.querySelector('#status-form')).toBeNull();
    });
  });

  describe('lots', () => {
    it('adds a lot with its family and contract amount', async () => {
      await open();
      await app.click('[data-testid="add-lot"]');
      expect(app.text('#lot-form h3')).toBe('New lot');

      await app.type('lot-number', '08');
      await app.type('lot-name', 'Electrical works');
      await app.select('lot-family', 'BuildingServices');
      await app.type('lot-contractAmount', '412380,45');
      await app.submit('#lot-form');
      const request = app.http.expectOne('/api/sites/site-1/lots');
      expect(request.request.method).toBe('POST');
      expect(request.request.body).toEqual({
        number: '08',
        name: 'Electrical works',
        family: 'BuildingServices',
        contractAmount: 412380.45,
      });
      request.flush(linden);
      await app.settle();

      expect(app.element.querySelector('#lot-form')).toBeNull();
    });

    it('edits a lot and records its amount later', async () => {
      await open();
      await app.click('[data-testid="edit-lot-06"]');
      expect(app.text('#lot-form h3')).toBe('Edit lot 06');
      expect(value('lot-contractAmount')).toBe('');

      await app.type('lot-contractAmount', '96500');
      await app.submit('#lot-form');
      const request = app.http.expectOne('/api/sites/site-1/lots/lot-6');
      expect(request.request.method).toBe('PUT');
      expect(request.request.body).toEqual({
        number: '06',
        name: 'Painting',
        family: 'FinishingWorks',
        contractAmount: 96500,
      });
      request.flush(linden);
      await app.settle();

      expect(app.element.querySelector('#lot-form')).toBeNull();
    });

    it('fills the form again when another lot is chosen', async () => {
      await open();
      await app.click('[data-testid="edit-lot-01"]');
      expect(value('lot-contractAmount')).toBe('1845000');

      await app.click('[data-testid="edit-lot-05"]');
      expect(value('lot-name')).toBe('Partitions and ceilings');
      expect(value('lot-contractAmount')).toBe('318720.5');

      await app.click('[data-testid="add-lot"]');
      expect(value('lot-name')).toBe('');
    });

    it('asks for the required fields and a valid amount', async () => {
      await open();
      await app.click('[data-testid="add-lot"]');
      await app.type('lot-contractAmount', '12.345');

      await app.submit('#lot-form');

      expect(app.texts('#lot-form pm-field-errors p')).toEqual([
        'Enter the lot number.',
        'Enter the lot name.',
        'Choose a family.',
        'Enter a positive amount in euros, with two decimals at most.',
      ]);
    });

    it('shows the API messages and general errors', async () => {
      await open();
      await app.click('[data-testid="edit-lot-01"]');
      await app.type('lot-number', '05');

      await app.submit('#lot-form');
      app.http
        .expectOne('/api/sites/site-1/lots/lot-1')
        .flush(
          { errors: { number: ['Another lot of this site already uses this number.'] } },
          { status: 400, statusText: 'Bad Request' },
        );
      await app.settle();
      expect(app.texts('#lot-form pm-field-errors p')).toEqual([
        'Another lot of this site already uses this number.',
      ]);

      await app.submit('#lot-form');
      app.http
        .expectOne('/api/sites/site-1/lots/lot-1')
        .flush(null, { status: 500, statusText: 'Server Error' });
      await app.settle();
      expect(app.text('#lot-form [role="alert"]')).toBe('Something went wrong. Please try again.');
    });

    it('closes the form on cancel', async () => {
      await open();
      await app.click('[data-testid="add-lot"]');

      await app.click('#lot-form button[type="button"]');

      expect(app.element.querySelector('#lot-form')).toBeNull();
    });

    it.each([
      ['', null],
      ['  ', null],
      ['1845000', 1845000],
      [' 318720,5 ', 318720.5],
    ])('reads the amount "%s"', (text, amount) => {
      expect(toAmount(text)).toBe(amount);
    });
  });

  describe('contractors', () => {
    it('adds a contractor and gives it a free lot', async () => {
      await open();
      await app.click('[data-testid="add-lot"]');
      await app.click('[data-testid="add-contractor"]');
      expect(app.element.querySelector('#lot-form')).toBeNull();
      expect(app.text('#contractor-form h3')).toBe('New contractor');
      expect(checkbox('01').disabled).toBe(true);
      expect(app.texts('#contractor-form fieldset label')[0]).toBe(
        '01 · Earthworks and structure (held by Atlas Structures)',
      );
      expect(checkbox('06').disabled).toBe(false);

      await app.type('contractor-companyName', 'Brightline Painting');
      await app.type('contractor-contactEmail', 'jade.morel@example.com');
      await app.click('#contractor-lot-06');
      await app.submit('#contractor-form');
      const request = app.http.expectOne('/api/sites/site-1/contractors');
      expect(request.request.method).toBe('POST');
      expect(request.request.body).toEqual({
        companyName: 'Brightline Painting',
        registrationNumber: '',
        address: '',
        contactName: '',
        contactEmail: 'jade.morel@example.com',
        phone: '',
        lotIds: ['lot-6'],
      });
      request.flush(linden);
      await app.settle();

      expect(app.element.querySelector('#contractor-form')).toBeNull();
    });

    it('edits a contractor and changes its lots', async () => {
      await open();
      await app.click('[data-testid="edit-contractor-1"]');
      expect(app.text('#contractor-form h3')).toBe('Edit Clearwall Interiors');
      expect(value('contractor-address')).toBe('4 Rue des Artisans, Lyon');
      expect(value('contractor-contactName')).toBe('');
      expect(checkbox('05').checked).toBe(true);
      expect(checkbox('05').disabled).toBe(false);

      await app.click('#contractor-lot-05');
      await app.click('#contractor-lot-06');
      await app.submit('#contractor-form');
      const request = app.http.expectOne('/api/sites/site-1/contractors/contractor-2');
      expect(request.request.method).toBe('PUT');
      expect(request.request.body).toEqual({
        companyName: 'Clearwall Interiors',
        registrationNumber: '',
        address: '4 Rue des Artisans, Lyon',
        contactName: '',
        contactEmail: '',
        phone: '+33 4 00 00 00 01',
        lotIds: ['lot-6'],
      });
      request.flush(linden);
      await app.settle();

      expect(app.element.querySelector('#contractor-form')).toBeNull();
    });

    it('fills the form again when another contractor is chosen', async () => {
      await open();
      await app.click('[data-testid="edit-contractor-0"]');
      expect(value('contractor-registrationNumber')).toBe('000 000 000 00012');
      expect(value('contractor-contactEmail')).toBe('claire.martin@example.com');

      await app.click('[data-testid="add-contractor"]');
      expect(value('contractor-companyName')).toBe('');
      expect(checkbox('01').checked).toBe(false);
    });

    it('asks for the company name and a valid email', async () => {
      await open();
      await app.click('[data-testid="add-contractor"]');
      await app.type('contractor-contactEmail', 'not-an-email');

      await app.submit('#contractor-form');

      expect(app.texts('#contractor-form pm-field-errors p')).toEqual([
        'Enter the company name.',
        'Enter a valid email address.',
      ]);
    });

    it('shows the API messages and general errors', async () => {
      await open();
      await app.click('[data-testid="add-contractor"]');
      await app.type('contractor-companyName', 'Brightline Painting');

      await app.submit('#contractor-form');
      app.http
        .expectOne('/api/sites/site-1/contractors')
        .flush(
          { errors: { lotIds: ['Lot 01 is already held by Atlas Structures.'] } },
          { status: 400, statusText: 'Bad Request' },
        );
      await app.settle();
      expect(app.texts('#contractor-form pm-field-errors p')).toEqual([
        'Lot 01 is already held by Atlas Structures.',
      ]);

      await app.submit('#contractor-form');
      app.http
        .expectOne('/api/sites/site-1/contractors')
        .flush(null, { status: 500, statusText: 'Server Error' });
      await app.settle();
      expect(app.text('#contractor-form [role="alert"]')).toBe(
        'Something went wrong. Please try again.',
      );
    });

    it('explains that lots come first on an empty site', async () => {
      await open(harbour);
      await app.click('[data-testid="add-contractor"]');

      expect(app.text('#contractor-form fieldset p')).toBe(
        'Add lots to the site to give them out.',
      );
    });

    it('shows a contractor without lots and closes the form on cancel', async () => {
      await open({
        ...linden,
        contractors: [{ ...linden.contractors[0], lotIds: [], contactName: null }],
      });
      expect(app.texts('[data-testid="held-lots"]')).toEqual(['No lots yet']);
      expect(app.texts('[data-testid="contact"]')).toEqual(['claire.martin@example.com']);

      await app.click('[data-testid="edit-contractor-0"]');
      await app.click('[data-testid="edit-lot-01"]');
      expect(app.element.querySelector('#contractor-form')).toBeNull();
      await app.click('[data-testid="add-contractor"]');
      await app.click('#contractor-form button[type="button"]');

      expect(app.element.querySelector('#contractor-form')).toBeNull();
    });
  });
});
