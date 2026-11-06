/** An account manager is one of the site's contractors or a third party. */
export type ManagerKind = 'Contractor' | 'ThirdParty';

export const MANAGER_KINDS: readonly { readonly value: ManagerKind; readonly label: string }[] = [
  { value: 'Contractor', label: 'A contractor working on the sites' },
  { value: 'ThirdParty', label: 'A third party, such as a consulting firm' },
];

export interface Organization {
  readonly id: string;
  readonly name: string;
  readonly kind: ManagerKind;
  readonly registrationNumber: string | null;
  readonly address: string | null;
  readonly phone: string | null;
}

export interface UpdateOrganizationRequest {
  readonly name: string;
  readonly kind: ManagerKind | '';
  readonly registrationNumber: string;
  readonly address: string;
  readonly phone: string;
}
