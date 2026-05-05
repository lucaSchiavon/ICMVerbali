using System.Data;
using Dapper;

namespace ICMVerbali.Web.Data;

// Registra i type handler Dapper necessari al dominio. Idempotente.
// Da invocare una sola volta in startup (Program.cs) e nei contesti di test.
public static class DapperConfiguration
{
    private static int _initialized;

    public static void Initialize()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1) return;
        SqlMapper.AddTypeHandler(new DateOnlyHandler());
        SqlMapper.AddTypeHandler(new NullableDateOnlyHandler());
    }

    // SQL Server 'date' arriva come DateTime via Microsoft.Data.SqlClient: il
    // handler converte in entrambe le direzioni. Senza questo Dapper 2.1.x
    // alza NotSupportedException su DateOnly come parametro.
    private sealed class DateOnlyHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override DateOnly Parse(object value) => value switch
        {
            DateTime dt => DateOnly.FromDateTime(dt),
            DateOnly d => d,
            string s => DateOnly.Parse(s),
            _ => throw new InvalidCastException($"Impossibile convertire {value.GetType()} in DateOnly."),
        };

        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.DbType = DbType.Date;
            parameter.Value = value.ToDateTime(TimeOnly.MinValue);
        }
    }

    private sealed class NullableDateOnlyHandler : SqlMapper.TypeHandler<DateOnly?>
    {
        public override DateOnly? Parse(object? value) => value switch
        {
            null => null,
            DateTime dt => DateOnly.FromDateTime(dt),
            DateOnly d => d,
            string s => DateOnly.Parse(s),
            _ => throw new InvalidCastException($"Impossibile convertire {value.GetType()} in DateOnly?."),
        };

        public override void SetValue(IDbDataParameter parameter, DateOnly? value)
        {
            parameter.DbType = DbType.Date;
            parameter.Value = value.HasValue ? value.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
        }
    }
}
