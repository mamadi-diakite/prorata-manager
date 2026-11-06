import { ManagerKind } from '../organization/organization.models';

export type UserRole = 'ManagerAdmin' | 'ManagerStaff';

export interface CurrentUser {
  readonly id: string;
  readonly email: string;
  readonly displayName: string;
  readonly role: UserRole;
  readonly organizationId: string;
}

export interface LoginRequest {
  readonly email: string;
  readonly password: string;
}

/** Sign-up creates the manager organization together with its first admin. */
export interface SignUpRequest {
  readonly organizationName: string;
  readonly organizationKind: ManagerKind | '';
  readonly displayName: string;
  readonly email: string;
  readonly password: string;
  readonly confirmPassword: string;
}

export interface ChangePasswordRequest {
  readonly currentPassword: string;
  readonly newPassword: string;
  readonly confirmPassword: string;
}

export interface LoginResponse {
  readonly accessToken: string;
  readonly expiresAtUtc: string;
  readonly user: CurrentUser;
}
