using Npgsql;
using StageTix.Data;

namespace StageTix.Tests.Data;

public class PostgresConnectionStringTests
{
    [Fact]
    public void A_cloud_url_is_converted_to_the_npgsql_format()
    {
        var result = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Normalize(
            "postgresql://app_user:p%40ss@ep-cool-1.eu-central-1.aws.neon.tech/stagetix?sslmode=require&channel_binding=require"));

        Assert.Equal("ep-cool-1.eu-central-1.aws.neon.tech", result.Host);
        Assert.Equal(5432, result.Port);
        Assert.Equal("stagetix", result.Database);
        Assert.Equal("app_user", result.Username);
        Assert.Equal("p@ss", result.Password);
        Assert.Equal(SslMode.Require, result.SslMode);
    }

    [Fact]
    public void A_url_with_an_explicit_port_keeps_it()
    {
        var result = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Normalize("postgres://u:p@db.internal:6543/app"));
        Assert.Equal(6543, result.Port);
    }

    [Fact]
    public void A_connection_string_already_in_npgsql_format_is_unchanged()
    {
        var value = new NpgsqlConnectionStringBuilder { Host = "localhost", Port = 5432, Database = "stagetix" }.ConnectionString;
        Assert.Equal(value, PostgresConnectionString.Normalize(value));
    }
}
