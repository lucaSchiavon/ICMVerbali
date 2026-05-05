using System.Data.Common;

namespace ICMVerbali.Web.Data;

public interface ISqlConnectionFactory
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
}
