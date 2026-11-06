export type SiteStatus = 'Preparation' | 'InProgress' | 'HandedOver' | 'Closed';

export type LotFamily = 'Structure' | 'FinishingWorks' | 'BuildingServices';

export interface Option<T extends string> {
  readonly value: T;
  readonly label: string;
}

/** In the order a site goes through them. */
export const SITE_STATUSES: readonly Option<SiteStatus>[] = [
  { value: 'Preparation', label: 'Preparation' },
  { value: 'InProgress', label: 'In progress' },
  { value: 'HandedOver', label: 'Handed over' },
  { value: 'Closed', label: 'Closed' },
];

export const LOT_FAMILIES: readonly Option<LotFamily>[] = [
  { value: 'Structure', label: 'Structure' },
  { value: 'FinishingWorks', label: 'Finishing works' },
  { value: 'BuildingServices', label: 'Building services' },
];

export function labelOf<T extends string>(options: readonly Option<T>[], value: T): string {
  return options.find((option) => option.value === value)!.label;
}

/** The statuses a site can still move to. */
export function laterStatuses(status: SiteStatus): readonly Option<SiteStatus>[] {
  return SITE_STATUSES.slice(SITE_STATUSES.findIndex((option) => option.value === status) + 1);
}

export interface SiteSummary {
  readonly id: string;
  readonly name: string;
  readonly address: string;
  readonly status: SiteStatus;
  /** ISO date, e.g. `2027-01-04`. */
  readonly startDate: string;
  readonly plannedHandoverDate: string;
  readonly lotCount: number;
  readonly contractorCount: number;
  readonly totalContractAmount: number;
}

export interface Lot {
  readonly id: string;
  readonly number: string;
  readonly name: string;
  readonly family: LotFamily;
  readonly contractorId: string | null;
  readonly contractorName: string | null;
  /** Euros excluding VAT, or `null` until recorded. */
  readonly contractAmount: number | null;
}

export interface Contractor {
  readonly id: string;
  readonly companyName: string;
  readonly registrationNumber: string | null;
  readonly address: string | null;
  readonly contactName: string | null;
  readonly contactEmail: string | null;
  readonly phone: string | null;
  readonly lotIds: readonly string[];
  readonly contractAmount: number;
}

export interface ContractTotals {
  readonly total: number;
  readonly structure: number;
  readonly finishingWorks: number;
  readonly buildingServices: number;
  readonly lotsWithoutAmount: number;
  readonly lotsWithoutContractor: number;
}

export interface Site {
  readonly id: string;
  readonly name: string;
  readonly address: string;
  readonly projectOwner: string;
  readonly projectManager: string;
  readonly startDate: string;
  readonly plannedHandoverDate: string;
  readonly status: SiteStatus;
  readonly lots: readonly Lot[];
  readonly contractors: readonly Contractor[];
  readonly totals: ContractTotals;
}

export interface SiteRequest {
  readonly name: string;
  readonly address: string;
  readonly projectOwner: string;
  readonly projectManager: string;
  readonly startDate: string | null;
  readonly plannedHandoverDate: string | null;
}

export interface LotRequest {
  readonly number: string;
  readonly name: string;
  readonly family: LotFamily | '';
  readonly contractAmount: number | null;
}

export interface ContractorRequest {
  readonly companyName: string;
  readonly registrationNumber: string;
  readonly address: string;
  readonly contactName: string;
  readonly contactEmail: string;
  readonly phone: string;
  readonly lotIds: readonly string[];
}
