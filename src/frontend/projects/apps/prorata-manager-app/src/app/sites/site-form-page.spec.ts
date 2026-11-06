import { AppHarness } from '../../testing/app-harness';
import { harbour, linden } from '../../testing/site-fixtures';

describe('Site form page', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  function value(id: string): string {
    return app.element.querySelector<HTMLInputElement>(`#${id}`)!.value;
  }

  async function fill(): Promise<void> {
    await app.type('name', 'Harbour Point offices');
    await app.type('address', '3 Quai du Port, Marseille');
    await app.type('projectOwner', 'Bayline Estates');
    await app.type('projectManager', 'Lumen Engineering');
    await app.type('startDate', '2027-03-01');
    await app.type('plannedHandoverDate', '2028-09-30');
  }

  describe('new site', () => {
    beforeEach(async () => {
      app.signIn();
      await app.open('/sites/new');
    });

    it('creates the site and opens it', async () => {
      expect(app.text('h1')).toBe('New site');
      await fill();

      await app.submit();
      const request = app.http.expectOne('/api/sites');
      expect(request.request.method).toBe('POST');
      expect(request.request.body).toEqual({
        name: 'Harbour Point offices',
        address: '3 Quai du Port, Marseille',
        projectOwner: 'Bayline Estates',
        projectManager: 'Lumen Engineering',
        startDate: '2027-03-01',
        plannedHandoverDate: '2028-09-30',
      });
      request.flush(harbour, { status: 201, statusText: 'Created' });
      await app.settle();

      expect(app.router.url).toBe('/sites/site-2');
      app.http.expectOne('/api/sites/site-2').flush(harbour);
    });

    it('asks for every field when empty', async () => {
      await app.submit();

      expect(app.texts('pm-field-errors p')).toEqual([
        'Enter the site name.',
        'Enter the address.',
        'Enter the project owner.',
        'Enter the project manager.',
        'Enter the start date.',
        'Enter the planned handover date.',
      ]);
    });

    it('shows the API messages next to each field', async () => {
      await fill();

      await app.submit();
      app.http
        .expectOne('/api/sites')
        .flush(
          { errors: { plannedHandoverDate: ['Planned handover must be after the start date.'] } },
          { status: 400, statusText: 'Bad Request' },
        );
      await app.settle();

      expect(app.texts('pm-field-errors p')).toEqual([
        'Planned handover must be after the start date.',
      ]);
    });

    it('shows a general message when the API fails', async () => {
      await fill();

      await app.submit();
      app.http.expectOne('/api/sites').flush(null, { status: 500, statusText: 'Server Error' });
      await app.settle();

      expect(app.text('[role="alert"]')).toBe('Something went wrong. Please try again.');
    });

    it('goes back to the list on cancel', async () => {
      expect(app.element.querySelector('form a')?.getAttribute('href')).toBe('/sites');
    });
  });

  it('is only for manager admins', async () => {
    app.signIn('ManagerStaff');

    await app.open('/sites/new');

    expect(app.router.url).toBe('/sites');
    app.http.expectOne('/api/sites').flush([]);
  });

  describe('edit site', () => {
    beforeEach(async () => {
      app.signIn();
      await app.open('/sites/site-1/edit');
    });

    it('fills the form and saves the changes', async () => {
      expect(app.text('[role="status"]')).toBe('Loading the site…');
      app.http.expectOne('/api/sites/site-1').flush(linden);
      await app.settle();

      expect(app.text('h1')).toBe('Edit site');
      expect(value('name')).toBe('Linden Court housing');
      expect(value('startDate')).toBe('2027-01-04');
      expect(app.element.querySelector('form a')?.getAttribute('href')).toBe('/sites/site-1');

      await app.type('projectManager', 'Lumen Engineering');
      await app.submit();
      const request = app.http.expectOne('/api/sites/site-1');
      expect(request.request.method).toBe('PUT');
      expect(request.request.body).toEqual({
        name: 'Linden Court housing',
        address: '12 Avenue des Tilleuls, Lyon',
        projectOwner: 'Cedar Homes',
        projectManager: 'Lumen Engineering',
        startDate: '2027-01-04',
        plannedHandoverDate: '2028-06-30',
      });
      request.flush({ ...linden, projectManager: 'Lumen Engineering' });
      await app.settle();

      expect(app.router.url).toBe('/sites/site-1');
      app.http.expectOne('/api/sites/site-1').flush(linden);
    });

    it('says when the site does not exist', async () => {
      app.http.expectOne('/api/sites/site-1').flush(null, { status: 404, statusText: 'Not Found' });
      await app.settle();

      expect(app.text('[role="alert"]')).toBe('This site does not exist.');
    });

    it('says when the site cannot be loaded', async () => {
      app.http
        .expectOne('/api/sites/site-1')
        .flush(null, { status: 500, statusText: 'Server Error' });
      await app.settle();

      expect(app.text('[role="alert"]')).toBe('The site could not be loaded. Please try again.');
    });
  });
});
