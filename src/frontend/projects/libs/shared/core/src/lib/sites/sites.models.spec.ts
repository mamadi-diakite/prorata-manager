import { labelOf, laterStatuses, LOT_FAMILIES, SITE_STATUSES } from './sites.models';

describe('site options', () => {
  it('reads the label of a value', () => {
    expect(labelOf(LOT_FAMILIES, 'FinishingWorks')).toBe('Finishing works');
    expect(labelOf(SITE_STATUSES, 'HandedOver')).toBe('Handed over');
  });

  it.each([
    ['Preparation', ['InProgress', 'HandedOver', 'Closed']],
    ['HandedOver', ['Closed']],
    ['Closed', []],
  ] as const)('lets a site in %s move forward only', (status, expected) => {
    expect(laterStatuses(status).map((option) => option.value)).toEqual(expected);
  });
});
