namespace ProrataManager.Application.Tests;

public sealed class FieldErrorsTests
{
    [Fact]
    public void Required_trims_or_reports_a_blank_or_long_value()
    {
        var errors = new FieldErrors();

        Assert.Equal("Atlas", errors.Required("name", " Atlas ", 10, "Name is required."));
        Assert.Empty(errors);

        Assert.Null(errors.Required("name", " ", 10, "Name is required."));
        Assert.Equal(["Name is required."], errors["name"]);

        Assert.Null(errors.Required("code", "too long", 3, "Code is required."));
        Assert.Equal(["Use at most 3 characters."], errors["code"]);
    }

    [Fact]
    public void Optional_turns_blank_into_null_and_reports_a_long_value()
    {
        var errors = new FieldErrors();

        Assert.Null(errors.Optional("phone", "  ", 5));
        Assert.Equal("0400", errors.Optional("phone", " 0400 ", 5));
        Assert.Empty(errors);

        Assert.Null(errors.Optional("phone", "040000", 5));
        Assert.Equal(["Use at most 5 characters."], errors["phone"]);
    }

    [Theory]
    [InlineData("thirdparty", ManagerKind.ThirdParty)]
    [InlineData(" Contractor ", ManagerKind.Contractor)]
    public void Choice_reads_an_enum_name_ignoring_case(string value, ManagerKind expected)
    {
        var errors = new FieldErrors();

        Assert.Equal(expected, errors.Choice<ManagerKind>("kind", value, "Choose a kind."));
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Supplier")]
    [InlineData("1")]
    public void Choice_reports_an_unknown_name(string? value)
    {
        var errors = new FieldErrors();

        Assert.Null(errors.Choice<ManagerKind>("kind", value, "Choose a kind."));
        Assert.Equal(["Choose a kind."], errors["kind"]);
    }
}
