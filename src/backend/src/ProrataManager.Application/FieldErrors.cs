namespace ProrataManager.Application;

/// <summary>Collects validation messages by request field, in camelCase.</summary>
internal sealed class FieldErrors : Dictionary<string, string[]>
{
    /// <returns>The trimmed value, or null when it is blank or too long.</returns>
    public string? Required(string field, string? value, int maxLength, string requiredMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            this[field] = [requiredMessage];
            return null;
        }

        return MaxLength(field, value.Trim(), maxLength);
    }

    /// <returns>The trimmed value, or null when it is blank or too long.</returns>
    public string? Optional(string field, string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : MaxLength(field, value.Trim(), maxLength);

    /// <summary>Reads an enum from its name, ignoring case.</summary>
    public TEnum? Choice<TEnum>(string field, string? value, string message)
        where TEnum : struct, Enum
    {
        var name = Enum.GetNames<TEnum>()
            .FirstOrDefault(n => string.Equals(n, value?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (name is null)
        {
            this[field] = [message];
            return null;
        }

        return Enum.Parse<TEnum>(name);
    }

    private string? MaxLength(string field, string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        this[field] = [$"Use at most {maxLength} characters."];
        return null;
    }
}
