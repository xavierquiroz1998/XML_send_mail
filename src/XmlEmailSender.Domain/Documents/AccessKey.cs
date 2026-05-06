using XmlEmailSender.Domain.Common;

namespace XmlEmailSender.Domain.Documents;

public sealed record AccessKey
{
    public string Value { get; }

    private AccessKey(string value) => Value = value;

    public static Result<AccessKey> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<AccessKey>(
                Error.Validation("AccessKey.Empty", "La clave de acceso es obligatoria."));

        var trimmed = value.Trim();

        if (trimmed.Length != 49)
            return Result.Failure<AccessKey>(
                Error.Validation("AccessKey.Length", "La clave de acceso debe tener 49 dígitos."));

        if (!trimmed.All(char.IsDigit))
            return Result.Failure<AccessKey>(
                Error.Validation("AccessKey.NotNumeric", "La clave de acceso debe contener solo dígitos."));

        if (!IsValidCheckDigit(trimmed))
            return Result.Failure<AccessKey>(
                Error.Validation("AccessKey.CheckDigit", "El dígito verificador de la clave de acceso es inválido."));

        return Result.Success(new AccessKey(trimmed));
    }

    /// <summary>
    /// Materializa una clave previamente validada (lectura desde la base).
    /// No revalida el dígito verificador para evitar coste en lecturas masivas.
    /// </summary>
    public static AccessKey FromTrustedSource(string value) => new(value);

    private static bool IsValidCheckDigit(string key)
    {
        var digits = key.Substring(0, 48);
        var providedCheckDigit = int.Parse(key.Substring(48, 1));

        int[] weights = { 2, 3, 4, 5, 6, 7 };
        int sum = 0;
        int weightIndex = 0;

        for (int i = digits.Length - 1; i >= 0; i--)
        {
            sum += int.Parse(digits[i].ToString()) * weights[weightIndex];
            weightIndex = (weightIndex + 1) % weights.Length;
        }

        int mod = sum % 11;
        int calculatedCheckDigit = 11 - mod;

        if (calculatedCheckDigit == 11) calculatedCheckDigit = 0;
        if (calculatedCheckDigit == 10) calculatedCheckDigit = 1;

        return calculatedCheckDigit == providedCheckDigit;
    }

    public override string ToString() => Value;
    public static implicit operator string(AccessKey key) => key.Value;
}
