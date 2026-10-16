using Npgsql;

namespace StageTix.Data;

/// <summary>
/// Cloud providers (Neon, Render) give the database address as a URL:
///   postgresql://user:password@host:5432/dbname?sslmode=require
/// Npgsql expects "Host=...;Username=...;..." instead. This accepts both formats.
/// </summary>
public static class PostgresConnectionString
{
    public static string Normalize(string connectionString)
    {
        var value = connectionString.Trim();
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null
        };

        // Keep the provider's SSL requirement (e.g. ?sslmode=require).
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && parts[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase) &&
                Enum.TryParse<SslMode>(parts[1], ignoreCase: true, out var sslMode))
            {
                builder.SslMode = sslMode;
            }
        }

        return builder.ConnectionString;
    }
}
