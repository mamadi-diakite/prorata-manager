namespace ProrataManager.Domain;

/// <summary>Amounts in euros, with two decimals at most.</summary>
public static class Money
{
    public static readonly decimal MaxAmount = 999_999_999.99m;

    public static bool IsValidAmount(decimal value) =>
        value > 0 && value <= MaxAmount && decimal.Round(value, 2) == value;

    internal static decimal Amount(decimal value, string paramName) =>
        IsValidAmount(value)
            ? value
            : throw new ArgumentOutOfRangeException(
                paramName, value, $"Amount must be positive, at most {MaxAmount}, with two decimals at most.");
}
