using Dapper;
using Microsoft.Data.SqlClient;

namespace CheckoutService.IntegrationTests.Infrastructure;

public static class DbCleanup
{
    public static async Task ResetAsync(string connectionString)
    {
        await using var con = new SqlConnection(connectionString);
        await con.OpenAsync();

        // order matters due to FK
        await con.ExecuteAsync("DELETE FROM sales_lin;");
        await con.ExecuteAsync("DELETE FROM sales_hdr;");
    }
}
