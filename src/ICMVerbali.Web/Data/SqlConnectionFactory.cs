using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace ICMVerbali.Web.Data;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Default non configurata. Impostala in user-secrets, " +
                "appsettings.Development.json o variabile d'ambiente ConnectionStrings__Default.");

        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException(
                "ConnectionStrings:Default e' valorizzata ma vuota.");
    }

    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
