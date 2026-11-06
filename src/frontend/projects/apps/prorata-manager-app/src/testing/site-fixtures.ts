import { Site, SiteSummary } from '@prorata-manager/shared/core';

export const lindenSummary: SiteSummary = {
  id: 'site-1',
  name: 'Linden Court housing',
  address: '12 Avenue des Tilleuls, Lyon',
  status: 'InProgress',
  startDate: '2027-01-04',
  plannedHandoverDate: '2028-06-30',
  lotCount: 3,
  contractorCount: 2,
  totalContractAmount: 2163720.5,
};

/** A site with a lot held by each contractor, and a lot without contractor or amount. */
export const linden: Site = {
  id: 'site-1',
  name: 'Linden Court housing',
  address: '12 Avenue des Tilleuls, Lyon',
  projectOwner: 'Cedar Homes',
  projectManager: 'North Arc Architects',
  startDate: '2027-01-04',
  plannedHandoverDate: '2028-06-30',
  status: 'InProgress',
  lots: [
    {
      id: 'lot-1',
      number: '01',
      name: 'Earthworks and structure',
      family: 'Structure',
      contractorId: 'contractor-1',
      contractorName: 'Atlas Structures',
      contractAmount: 1845000,
    },
    {
      id: 'lot-5',
      number: '05',
      name: 'Partitions and ceilings',
      family: 'FinishingWorks',
      contractorId: 'contractor-2',
      contractorName: 'Clearwall Interiors',
      contractAmount: 318720.5,
    },
    {
      id: 'lot-6',
      number: '06',
      name: 'Painting',
      family: 'FinishingWorks',
      contractorId: null,
      contractorName: null,
      contractAmount: null,
    },
  ],
  contractors: [
    {
      id: 'contractor-1',
      companyName: 'Atlas Structures',
      registrationNumber: '000 000 000 00012',
      address: null,
      contactName: 'Claire Martin',
      contactEmail: 'claire.martin@example.com',
      phone: null,
      lotIds: ['lot-1'],
      contractAmount: 1845000,
    },
    {
      id: 'contractor-2',
      companyName: 'Clearwall Interiors',
      registrationNumber: null,
      address: '4 Rue des Artisans, Lyon',
      contactName: null,
      contactEmail: null,
      phone: '+33 4 00 00 00 01',
      lotIds: ['lot-5'],
      contractAmount: 318720.5,
    },
  ],
  totals: {
    total: 2163720.5,
    structure: 1845000,
    finishingWorks: 318720.5,
    buildingServices: 0,
    lotsWithoutAmount: 1,
    lotsWithoutContractor: 1,
  },
};

/** A new site in preparation, with nothing on it yet. */
export const harbour: Site = {
  id: 'site-2',
  name: 'Harbour Point offices',
  address: '3 Quai du Port, Marseille',
  projectOwner: 'Bayline Estates',
  projectManager: 'Lumen Engineering',
  startDate: '2027-03-01',
  plannedHandoverDate: '2028-09-30',
  status: 'Preparation',
  lots: [],
  contractors: [],
  totals: {
    total: 0,
    structure: 0,
    finishingWorks: 0,
    buildingServices: 0,
    lotsWithoutAmount: 0,
    lotsWithoutContractor: 0,
  },
};
