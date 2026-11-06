import { Organization } from '@prorata-manager/shared/core';
import { AppHarness } from '../../testing/app-harness';

const organization: Organization = {
  id: 'organization-1',
  name: 'Atlas Structures',
  kind: 'Contractor',
  registrationNumber: '000 000 000 00012',
  address: null,
  phone: null,
};

describe('Organization page', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  function value(id: string): string {
    return app.element.querySelector<HTMLInputElement>(`#${id}`)!.value;
  }

  function checked(id: string): boolean {
    return app.element.querySelector<HTMLInputElement>(`#${id}`)!.checked;
  }

  async function openAs(role: 'ManagerAdmin' | 'ManagerStaff' = 'ManagerAdmin'): Promise<void> {
    app.signIn(role);
    await app.open('/organization');
    expect(app.text('[role="status"]')).toBe('Loading…');
    app.http.expectOne('/api/organization').flush(organization);
    await app.settle();
  }

  it('needs a signed-in user', async () => {
    await app.open('/organization');

    expect(app.router.url).toBe('/login?returnUrl=%2Forganization');
  });

  it('shows the details of the organization', async () => {
    await openAs();

    expect(value('name')).toBe('Atlas Structures');
    expect(checked('kind-Contractor')).toBe(true);
    expect(checked('kind-ThirdParty')).toBe(false);
    expect(value('registrationNumber')).toBe('000 000 000 00012');
    expect(value('address')).toBe('');
    expect(app.element.querySelector('[data-testid="read-only"]')).toBeNull();
  });

  it('lets the admin say the organization is a third party and save', async () => {
    await openAs();
    await app.type('name', 'Prism Site Services');
    await app.check('kind-ThirdParty');
    await app.type('address', '8 Rue de l’Exemple, Lyon');

    await app.submit();
    const request = app.http.expectOne('/api/organization');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      name: 'Prism Site Services',
      kind: 'ThirdParty',
      registrationNumber: '000 000 000 00012',
      address: '8 Rue de l’Exemple, Lyon',
      phone: '',
    });
    request.flush({
      ...organization,
      name: 'Prism Site Services',
      kind: 'ThirdParty',
      registrationNumber: null,
      address: '8 Rue de l’Exemple, Lyon',
    });
    await app.settle();

    expect(app.text('[role="status"]')).toBe('The details have been saved.');
    expect(checked('kind-ThirdParty')).toBe(true);
    expect(value('registrationNumber')).toBe('');
  });

  it('shows the API messages next to each field', async () => {
    await openAs();
    await app.type('phone', '0'.repeat(40));

    await app.submit();
    app.http
      .expectOne('/api/organization')
      .flush(
        { errors: { phone: ['Use at most 30 characters.'] } },
        { status: 400, statusText: 'Bad Request' },
      );
    await app.settle();

    expect(app.texts('pm-field-errors p')).toEqual(['Use at most 30 characters.']);
    expect(app.element.querySelector('[role="status"]')).toBeNull();
  });

  it('shows a general message when saving fails', async () => {
    await openAs();

    await app.submit();
    app.http
      .expectOne('/api/organization')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('Something went wrong. Please try again.');
  });

  it('asks for the name before saving', async () => {
    await openAs();
    await app.type('name', '');

    await app.submit();

    expect(app.texts('pm-field-errors p')).toEqual(['Enter the organization name.']);
  });

  it('shows the details read-only to staff members', async () => {
    await openAs('ManagerStaff');

    expect(app.text('[data-testid="read-only"]')).toBe(
      'Only a manager admin can change these details.',
    );
    expect(app.element.querySelector<HTMLInputElement>('#name')!.disabled).toBe(true);
    expect(app.element.querySelector('form button[type="submit"]')).toBeNull();
  });

  it('says when the organization cannot be loaded', async () => {
    app.signIn();
    await app.open('/organization');

    app.http
      .expectOne('/api/organization')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe(
      'The organization could not be loaded. Please try again.',
    );
  });
});
