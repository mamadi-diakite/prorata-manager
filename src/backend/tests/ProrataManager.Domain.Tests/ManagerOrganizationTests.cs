namespace ProrataManager.Domain.Tests;

public sealed class ManagerOrganizationTests
{
    [Fact]
    public void Creates_an_organization_without_optional_details()
    {
        var organization = new ManagerOrganization(" Atlas Structures ", ManagerKind.Contractor, TestDates.Now);

        Assert.NotEqual(Guid.Empty, organization.Id);
        Assert.Equal("Atlas Structures", organization.Name);
        Assert.Equal(ManagerKind.Contractor, organization.Kind);
        Assert.Null(organization.RegistrationNumber);
        Assert.Null(organization.Address);
        Assert.Null(organization.Phone);
        Assert.Equal(TestDates.Now, organization.CreatedAtUtc);
    }

    [Fact]
    public void Rejects_invalid_values()
    {
        Assert.Equal("name", Assert.Throws<ArgumentException>(
            () => new ManagerOrganization(" ", ManagerKind.Contractor, TestDates.Now)).ParamName);
        Assert.Equal("kind", Assert.Throws<ArgumentOutOfRangeException>(
            () => new ManagerOrganization("Atlas", (ManagerKind)7, TestDates.Now)).ParamName);
        Assert.Equal("createdAtUtc", Assert.Throws<ArgumentException>(
            () => new ManagerOrganization("Atlas", ManagerKind.Contractor, TestDates.Local)).ParamName);
    }

    [Fact]
    public void Update_details_replaces_every_field()
    {
        var organization = new ManagerOrganization("Atlas", ManagerKind.Contractor, TestDates.Now);

        organization.UpdateDetails(" Prism Site Services ", ManagerKind.ThirdParty, " 000 000 000 00012 ", " 8 Rue de l'Exemple, Lyon ", " +33 4 00 00 00 00 ");

        Assert.Equal("Prism Site Services", organization.Name);
        Assert.Equal(ManagerKind.ThirdParty, organization.Kind);
        Assert.Equal("000 000 000 00012", organization.RegistrationNumber);
        Assert.Equal("8 Rue de l'Exemple, Lyon", organization.Address);
        Assert.Equal("+33 4 00 00 00 00", organization.Phone);

        organization.UpdateDetails("Prism Site Services", ManagerKind.ThirdParty, null, " ", null);
        Assert.Null(organization.RegistrationNumber);
        Assert.Null(organization.Address);
    }

    [Fact]
    public void Update_details_changes_nothing_when_a_value_is_invalid()
    {
        var organization = new ManagerOrganization("Atlas", ManagerKind.Contractor, TestDates.Now);

        Assert.Throws<ArgumentException>(() => organization.UpdateDetails(
            "Renamed", ManagerKind.ThirdParty, null, null, new string('1', ManagerOrganization.PhoneMaxLength + 1)));

        Assert.Equal("Atlas", organization.Name);
        Assert.Equal(ManagerKind.Contractor, organization.Kind);
    }
}
