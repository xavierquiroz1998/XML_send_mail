using System.Data;
using System.Globalization;
using Dapper;

namespace XmlEmailSender.Infrastructure.Persistence.TypeHandlers;

/// <summary>
/// SQLite guarda DateTime como TEXT en formato ISO-8601 (estándar de Microsoft.Data.Sqlite).
/// Cuando el cliente lo solicita como DateTime, el provider ya parsea — pero al cargarlo
/// dentro de filas dinámicas / propiedades reflexivas conviene tener este handler para
/// formatos no estándar y para escribir siempre con la "o" (round-trip) preservando precisión.
/// </summary>
internal sealed class DateTimeTypeHandler : SqlMapper.TypeHandler<DateTime>
{
    public override DateTime Parse(object value) => value switch
    {
        DateTime dt => dt,
        string s => DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        _ => throw new DataException($"No se puede convertir {value?.GetType().Name ?? "null"} a DateTime.")
    };

    public override void SetValue(IDbDataParameter parameter, DateTime value)
    {
        parameter.Value = value.ToString("o", CultureInfo.InvariantCulture);
    }
}

internal sealed class NullableDateTimeTypeHandler : SqlMapper.TypeHandler<DateTime?>
{
    public override DateTime? Parse(object value)
    {
        if (value is null || value is DBNull) return null;
        return value switch
        {
            DateTime dt => dt,
            string s => string.IsNullOrEmpty(s) ? null : DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            _ => throw new DataException($"No se puede convertir {value.GetType().Name} a DateTime?.")
        };
    }

    public override void SetValue(IDbDataParameter parameter, DateTime? value)
    {
        parameter.Value = value?.ToString("o", CultureInfo.InvariantCulture) ?? (object)DBNull.Value;
    }
}
