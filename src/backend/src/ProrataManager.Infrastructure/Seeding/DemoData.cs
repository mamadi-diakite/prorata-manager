namespace ProrataManager.Infrastructure.Seeding;

/// <summary>Fictional manager organizations, sites, contractors and amounts. All names are made up.</summary>
public static class DemoData
{
    public static IReadOnlyList<DemoOrganization> Organizations { get; } =
    [
        new(
            "Atlas Structures",
            ManagerKind.Contractor,
            "claire.martin@example.com",
            "Claire Martin",
            [
                new(
                    "Linden Court housing",
                    "12 Avenue des Tilleuls, 69003 Lyon",
                    "Cedar Homes",
                    "North Arc Architects",
                    new DateOnly(2026, 3, 2),
                    new DateOnly(2027, 9, 30),
                    SiteStatus.InProgress,
                    [
                        new("Atlas Structures", "Claire Martin", "claire.martin@example.com"),
                        new("Summit Roofing", "Marc Dubois", "marc.dubois@example.com"),
                        new("Clearwall Interiors", "Sofia Petit", "sofia.petit@example.com"),
                        new("Brightline Electrics", "Yanis Moreau", "yanis.moreau@example.com"),
                        new("Aquaflow Services", "Emma Laurent", "emma.laurent@example.com"),
                    ],
                    [
                        new("01", "Earthworks and structure", LotFamily.Structure, "Atlas Structures", 1_845_000.00m),
                        new("02", "Roofing and waterproofing", LotFamily.Structure, "Summit Roofing", 286_450.00m),
                        new("05", "Partitions and ceilings", LotFamily.FinishingWorks, "Clearwall Interiors", 318_720.50m),
                        new("06", "Painting", LotFamily.FinishingWorks, null, null),
                        new("08", "Electrical works", LotFamily.BuildingServices, "Brightline Electrics", 412_380.45m),
                        new("09", "Plumbing, heating and ventilation", LotFamily.BuildingServices, "Aquaflow Services", 538_900.00m),
                    ]),
                new(
                    "Harbour Point offices",
                    "4 Quai des Docks, 13002 Marseille",
                    "Bayline Property",
                    "Studio Meridian",
                    new DateOnly(2027, 2, 1),
                    new DateOnly(2028, 12, 15),
                    SiteStatus.Preparation,
                    [new("Atlas Structures", "Claire Martin", "claire.martin@example.com")],
                    [
                        new("01", "Structure", LotFamily.Structure, "Atlas Structures", 2_310_000.00m),
                        new("04", "Floor and wall finishes", LotFamily.FinishingWorks, null, null),
                    ]),
            ]),
        new(
            "Prism Site Services",
            ManagerKind.ThirdParty,
            "hugo.lambert@example.com",
            "Hugo Lambert",
            [
                new(
                    "Maple School extension",
                    "27 Rue des Erables, 44000 Nantes",
                    "Westfield Town Council",
                    "Lumen Engineering",
                    new DateOnly(2025, 9, 1),
                    new DateOnly(2026, 10, 30),
                    SiteStatus.HandedOver,
                    [
                        new("Keystone Builders", "Paul Girard", "paul.girard@example.com"),
                        new("Fine Finish Workshop", "Lea Roux", "lea.roux@example.com"),
                        new("Voltaic Systems", "Nina Fontaine", "nina.fontaine@example.com"),
                    ],
                    [
                        new("01", "Structural works", LotFamily.Structure, "Keystone Builders", 964_200.00m),
                        new("03", "Joinery", LotFamily.FinishingWorks, "Fine Finish Workshop", 142_615.80m),
                        new("04", "Painting and flooring", LotFamily.FinishingWorks, "Fine Finish Workshop", 98_330.00m),
                        new("07", "Electrical and fire safety", LotFamily.BuildingServices, "Voltaic Systems", 205_470.25m),
                    ]),
            ]),
    ];
}

public sealed record DemoOrganization(
    string Name,
    ManagerKind Kind,
    string AdminEmail,
    string AdminName,
    IReadOnlyList<DemoSite> Sites);

public sealed record DemoSite(
    string Name,
    string Address,
    string ProjectOwner,
    string ProjectManager,
    DateOnly StartDate,
    DateOnly PlannedHandoverDate,
    SiteStatus Status,
    IReadOnlyList<DemoContractor> Contractors,
    IReadOnlyList<DemoLot> Lots);

public sealed record DemoContractor(string CompanyName, string ContactName, string ContactEmail);

/// <param name="Contractor">Company name of the contractor holding the lot; null when not assigned yet.</param>
/// <param name="ContractAmount">Excluding VAT; null when not recorded yet.</param>
public sealed record DemoLot(string Number, string Name, LotFamily Family, string? Contractor, decimal? ContractAmount);
