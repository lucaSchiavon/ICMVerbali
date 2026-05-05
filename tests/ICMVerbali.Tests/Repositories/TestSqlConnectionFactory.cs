using System.Data.Common;
using ICMVerbali.Web.Data;
using Microsoft.Data.SqlClient;

namespace ICMVerbali.Tests.Repositories;

// Factory di test che apre connessioni al DB ICMVerbaliDb di sviluppo.
// Implementa la stessa interfaccia di produzione (ISqlConnectionFactory)
// senza richiedere IConfiguration.
//
// Questi test sono integrazione (toccano un SQL Server reale), non unit test
// puri. Per evitare polluzione del DB ogni test fa cleanup nel finally.
internal sealed class TestSqlConnectionFactory : ISqlConnectionFactory
{
    public const string ConnectionString =
        @"Server=.\SQLEXPRESS;Database=ICMVerbaliDb;Trusted_Connection=True;TrustServerCertificate=True;";

    static TestSqlConnectionFactory()
    {
        DapperConfiguration.Initialize();
    }

    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
