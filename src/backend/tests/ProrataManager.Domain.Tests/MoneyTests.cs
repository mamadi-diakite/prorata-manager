namespace ProrataManager.Domain.Tests;

public sealed class MoneyTests
{
    [Theory]
    [InlineData("0.01")]
    [InlineData("1250000")]
    [InlineData("48750.5")]
    [InlineData("999999999.99")]
    public void Accepts_positive_amounts_with_two_decimals_at_most(string value)
    {
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        Assert.True(Money.IsValidAmount(amount));
        Assert.Equal(amount, Money.Amount(amount, "amount"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-10")]
    [InlineData("10.005")]
    [InlineData("1000000000")]
    public void Rejects_zero_negative_too_precise_or_too_large_amounts(string value)
    {
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        Assert.False(Money.IsValidAmount(amount));
        Assert.Equal("amount", Assert.Throws<ArgumentOutOfRangeException>(() => Money.Amount(amount, "amount")).ParamName);
    }
}
