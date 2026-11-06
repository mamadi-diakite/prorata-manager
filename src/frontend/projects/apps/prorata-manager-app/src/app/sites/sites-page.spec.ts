import { AppHarness } from '../../testing/app-harness';
import { lindenSummary } from '../../testing/site-fixtures';

describe('Sites page', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  async function open(role: 'ManagerAdmin' | 'ManagerStaff' = 'ManagerAdmin'): Promise<void> {
    app.signIn(role);
    await app.open('/sites');
  }

  it('needs a signed-in user', async () => {
    await app.open('/sites');

    expect(app.router.url).toBe('/login?returnUrl=%2Fsites');
  });

  it('lists the sites with their status, dates and amounts', async () => {
    await open();
    expect(app.text('[role="status"]')).toBe('Loading the sites…');

    app.http.expectOne('/api/sites').flush([lindenSummary]);
    await app.settle();

    expect(app.texts('tbody td')).toEqual([
      'Linden Court housing12 Avenue des Tilleuls, Lyon',
      'In progress',
      'Jan 4, 2027',
      'Jun 30, 2028',
      '3',
      '2',
      '€2,163,720.50',
    ]);
    expect(app.element.querySelector('tbody a')?.getAttribute('href')).toBe('/sites/site-1');
    expect(app.text('a[href="/sites/new"]')).toBe('New site');
  });

  it('says when there are no sites yet', async () => {
    await open();

    app.http.expectOne('/api/sites').flush([]);
    await app.settle();

    expect(app.text('[data-testid="no-sites"]')).toBe('No sites yet.');
  });

  it('does not offer staff members to create a site', async () => {
    await open('ManagerStaff');

    app.http.expectOne('/api/sites').flush([]);
    await app.settle();

    expect(app.element.querySelector('a[href="/sites/new"]')).toBeNull();
  });

  it('says when the sites cannot be loaded', async () => {
    await open();

    app.http.expectOne('/api/sites').flush(null, { status: 500, statusText: 'Server Error' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('The sites could not be loaded. Please try again.');
  });
});
