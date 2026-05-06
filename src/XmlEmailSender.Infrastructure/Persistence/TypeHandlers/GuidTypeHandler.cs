using System.Data;
using Dapper;

namespace XmlEmailSender.Infrastructure.Persistence.TypeHandlers;

/// <summary>
/// SQLite no tiene un tipo Guid nativo: lo guarda como TEXT.
/// Sin un type handler, Dapper falla al materializar Guid desde string.
/// Este handler resuelve la conversión en ambos sentidos para SQLite y SQL Server.
/// </summary>
internal sealed class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override Guid Parse(object value) => value switch
    {
        Guid g => g,
        string s => Guid.Parse(s),
        byte[] b => new Guid(b),
        _ => throw new DataException($"No se puede convertir {value?.GetType().Name ?? "null"} a Guid.")
    };

    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        // SQLite serializa el Guid como TEXT con formato "D" (sin llaves).
        parameter.Value = value.ToString();
    }
}

internal sealed class NullableGuidTypeHandler : SqlMapper.TypeHandler<Guid?>
{
    public override Guid? Parse(object value)
    {
        if (value is null || value is DBNull) return null;
        return value switch
        {
            Guid g => g,
            string s => string.IsNullOrEmpty(s) ? null : Guid.Parse(s),
            byte[] b => new Guid(b),
            _ => throw new DataException($"No se puede convertir {value.GetType().Name} a Guid?.")
        };
    }

    public override void SetValue(IDbDataParameter parameter, Guid? value)
    {
        parameter.Value = value?.ToString() ?? (object)DBNull.Value;
    }
}
