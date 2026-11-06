namespace ProrataManager.Domain.Tests;

public sealed class ContractorTests
{
    private static readonly Guid SiteId = Guid.NewGuid();

    [Fact]
    public void Creates_a_contractor_with_its_company_details()
    {
        var contractor = new Contractor(
            SiteId,
            new ContractorDetails(" Brightline Electrics ", " 000 000 000 00034 ", " 5 Rue Volta ", " Hugo Lambert ", " Hugo.Lambert@Example.com ", " 0400000000 "),
            TestDates.Now);

        Assert.NotEqual(Guid.Empty, contractor.Id);
        Assert.Equal(SiteId, contractor.SiteId);
        Assert.Equal("Brightline Electrics", contractor.CompanyName);
        Assert.Equal("000 000 000 00034", contractor.RegistrationNumber);
        Assert.Equal("5 Rue Volta", contractor.Address);
        Assert.Equal("Hugo Lambert", contractor.ContactName);
        Assert.Equal("hugo.lambert@example.com", contractor.ContactEmail);
        Assert.Equal("0400000000", contractor.Phone);
        Assert.Equal(TestDates.Now, contractor.CreatedAtUtc);
    }

    [Fact]
    public void Only_the_company_name_is_required()
    {
        var contractor = new Contractor(SiteId, new ContractorDetails("Brightline Electrics", ContactEmail: " "), TestDates.Now);

        Assert.Null(contractor.RegistrationNumber);
        Assert.Null(contractor.Address);
        Assert.Null(contractor.ContactName);
        Assert.Null(contractor.ContactEmail);
        Assert.Null(contractor.Phone);
    }

    [Fact]
    public void Rejects_invalid_values()
    {
        Assert.Equal("siteId", Assert.Throws<ArgumentException>(
            () => new Contractor(Guid.Empty, new ContractorDetails("Brightline"), TestDates.Now)).ParamName);
        Assert.Equal("createdAtUtc", Assert.Throws<ArgumentException>(
            () => new Contractor(SiteId, new ContractorDetails("Brightline"), TestDates.Local)).ParamName);
        Assert.Equal("CompanyName", Assert.Throws<ArgumentException>(
            () => new Contractor(SiteId, new ContractorDetails(" "), TestDates.Now)).ParamName);
        Assert.Equal("ContactEmail", Assert.Throws<ArgumentException>(
            () => new Contractor(SiteId, new ContractorDetails("Brightline", ContactEmail: "hugo@"), TestDates.Now)).ParamName);
        Assert.Throws<ArgumentNullException>(() => new Contractor(SiteId, null!, TestDates.Now));
    }

    [Fact]
    public void Update_details_replaces_every_field_or_nothing()
    {
        var contractor = new Contractor(SiteId, new ContractorDetails("Brightline Electrics"), TestDates.Now);

        contractor.UpdateDetails(new ContractorDetails("Brightline", "123", "Address", "Hugo", "hugo@example.com", "0400"));
        Assert.Equal("Brightline", contractor.CompanyName);
        Assert.Equal("hugo@example.com", contractor.ContactEmail);

        Assert.Throws<ArgumentException>(() => contractor.UpdateDetails(
            new ContractorDetails("Renamed", Phone: new string('1', Contractor.PhoneMaxLength + 1))));
        Assert.Equal("Brightline", contractor.CompanyName);
        Assert.Equal("0400", contractor.Phone);
    }

    [Theory]
    [InlineData("hugo@example.com", true)]
    [InlineData("hugo@", false)]
    [InlineData(null, false)]
    public void Is_valid_email_matches_the_constructor_rule(string? email, bool expected)
    {
        Assert.Equal(expected, Contractor.IsValidEmail(email));
    }
}
