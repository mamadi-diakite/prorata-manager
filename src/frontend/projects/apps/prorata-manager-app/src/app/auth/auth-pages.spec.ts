import { AppHarness, demoSession } from '../../testing/app-harness';

describe('Auth pages', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  describe('login', () => {
    it('asks for the missing fields without calling the API', async () => {
      await app.open('/login');

      await app.submit();

      expect(app.texts('pm-field-errors p')).toEqual([
        'Enter a valid email address.',
        'Enter your password.',
      ]);
    });

    it('shows the API message when the credentials are wrong', async () => {
      await app.open('/login');
      await app.type('email', 'claire.martin@example.com');
      await app.type('password', 'wrong');

      await app.submit();
      expect(app.button().disabled).toBe(true);
      const request = app.http.expectOne('/api/auth/login');
      expect(request.request.body).toEqual({
        email: 'claire.martin@example.com',
        password: 'wrong',
      });
      request.flush(
        { title: 'Email or password is not correct.' },
        { status: 401, statusText: 'Unauthorized' },
      );
      await app.settle();

      expect(app.text('[role="alert"]')).toBe('Email or password is not correct.');
      expect(app.button().disabled).toBe(false);
    });

    it('signs in and goes back to the page the user wanted', async () => {
      await app.open('/account/password');
      expect(app.router.url).toBe('/login?returnUrl=%2Faccount%2Fpassword');

      await app.type('email', 'claire.martin@example.com');
      await app.type('password', 'TestDemo123!');
      await app.submit();
      app.http.expectOne('/api/auth/login').flush(demoSession);
      await app.settle();

      expect(app.router.url).toBe('/account/password');
      expect(app.text('[data-testid="user-name"]')).toBe('Claire Martin');
    });

    it('goes home when the return address leaves the app', async () => {
      await app.open('/login?returnUrl=%2F%2Fevil.example.com');
      await app.type('email', 'claire.martin@example.com');
      await app.type('password', 'TestDemo123!');

      await app.submit();
      app.http.expectOne('/api/auth/login').flush(demoSession);
      await app.settle();

      expect(app.router.url).toBe('/');
    });

    it('explains that the session has expired', async () => {
      await app.open('/login?expired=true');

      expect(app.text('[role="status"]')).toBe('Your session has expired. Please log in again.');
    });

    it('is not shown to signed-in users', async () => {
      app.signIn();

      await app.open('/login');

      expect(app.router.url).toBe('/');
    });
  });

  describe('sign-up', () => {
    async function fill(): Promise<void> {
      await app.open('/sign-up');
      await app.type('organizationName', 'Prism Site Services');
      await app.check('organizationKind-ThirdParty');
      await app.type('displayName', 'Hugo Lambert');
      await app.type('email', 'hugo.lambert@example.com');
      await app.type('password', 'short');
      await app.type('confirmPassword', 'other');
    }

    it('offers both kinds of account manager', async () => {
      await app.open('/sign-up');

      expect(app.texts('fieldset label')).toEqual([
        'A contractor working on the sites',
        'A third party, such as a consulting firm',
      ]);
    });

    it('shows the API messages next to each field', async () => {
      await fill();

      await app.submit();
      app.http.expectOne('/api/auth/sign-up').flush(
        {
          errors: {
            password: ['Use at least 8 characters.'],
            confirmPassword: ['Confirmation does not match the password.'],
          },
        },
        { status: 400, statusText: 'Bad Request' },
      );
      await app.settle();

      expect(app.texts('pm-field-errors p')).toEqual([
        'Use at least 8 characters.',
        'Confirmation does not match the password.',
      ]);
    });

    it('creates the organization, signs in and goes home', async () => {
      await fill();

      await app.submit();
      const request = app.http.expectOne('/api/auth/sign-up');
      expect(request.request.body).toEqual({
        organizationName: 'Prism Site Services',
        organizationKind: 'ThirdParty',
        displayName: 'Hugo Lambert',
        email: 'hugo.lambert@example.com',
        password: 'short',
        confirmPassword: 'other',
      });
      request.flush(demoSession, { status: 201, statusText: 'Created' });
      await app.settle();

      expect(app.router.url).toBe('/');
      expect(app.text('[data-testid="user-name"]')).toBe('Claire Martin');
    });

    it('shows a general message when the API fails', async () => {
      await fill();

      await app.submit();
      app.http
        .expectOne('/api/auth/sign-up')
        .flush(null, { status: 500, statusText: 'Server Error' });
      await app.settle();

      expect(app.text('[role="alert"]')).toBe('Something went wrong. Please try again.');
    });

    it('asks for every field when empty', async () => {
      await app.open('/sign-up');

      await app.submit();

      expect(app.texts('pm-field-errors p')).toEqual([
        'Enter your organization name.',
        'Say whether your organization is a contractor or a third party.',
        'Enter your name.',
        'Enter a valid email address.',
        'Choose a password.',
        'Confirm your password.',
      ]);
    });
  });

  describe('change password', () => {
    beforeEach(async () => {
      app.signIn();
      await app.open('/account/password');
      await app.type('currentPassword', 'TestDemo123!');
      await app.type('newPassword', 'NewPass456');
      await app.type('confirmPassword', 'NewPass456');
    });

    it('shows the API messages next to each field', async () => {
      await app.submit();
      const request = app.http.expectOne('/api/auth/change-password');
      expect(request.request.headers.get('Authorization')).toBe('Bearer token-1');
      request.flush(
        { errors: { currentPassword: ['Current password is not correct.'] } },
        { status: 400, statusText: 'Bad Request' },
      );
      await app.settle();

      expect(app.texts('pm-field-errors p')).toEqual(['Current password is not correct.']);
      expect(app.element.querySelector('[role="status"]')).toBeNull();
    });

    it('confirms the change and clears the form', async () => {
      await app.submit();
      app.http
        .expectOne('/api/auth/change-password')
        .flush(null, { status: 204, statusText: 'No Content' });
      await app.settle();

      expect(app.text('[role="status"]')).toBe('Your password has been changed.');
      expect(app.element.querySelector<HTMLInputElement>('#currentPassword')!.value).toBe('');
      expect(app.texts('pm-field-errors p')).toEqual([]);
    });

    it('asks for every field when empty', async () => {
      await app.type('currentPassword', '');
      await app.type('newPassword', '');
      await app.type('confirmPassword', '');

      await app.submit();

      expect(app.texts('pm-field-errors p')).toEqual([
        'Enter your current password.',
        'Choose a new password.',
        'Confirm your new password.',
      ]);
    });

    it('shows a general message when the API fails', async () => {
      await app.submit();
      app.http
        .expectOne('/api/auth/change-password')
        .flush(null, { status: 500, statusText: 'Server Error' });
      await app.settle();

      expect(app.text('[role="alert"]')).toBe('Something went wrong. Please try again.');
    });

    it('sends the user to login when the session is refused', async () => {
      await app.submit();
      app.http
        .expectOne('/api/auth/change-password')
        .flush(null, { status: 401, statusText: 'Unauthorized' });
      await app.settle();

      expect(app.router.url).toBe('/login?expired=true');
      expect(app.text('[role="status"]')).toBe('Your session has expired. Please log in again.');
    });
  });
});
