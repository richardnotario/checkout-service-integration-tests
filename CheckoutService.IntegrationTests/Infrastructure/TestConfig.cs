using Microsoft.Extensions.Configuration;

namespace CheckoutService.IntegrationTests.Infrastructure;

public static class TestConfig
{
    public static IConfiguration Load() =>
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

    public static string ApiBaseUrl(IConfiguration cfg) =>
        cfg["ApiBaseUrl"] ?? "http://127.0.0.1:8080";

    public static string SqlConnectionString(IConfiguration cfg) =>
        cfg.GetSection("Sql")["ConnectionString"]
        ?? throw new InvalidOperationException("Missing Sql:ConnectionString in appsettings.json or env vars.");
}
