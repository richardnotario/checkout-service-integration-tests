using Dapper;
using Microsoft.Data.SqlClient;
using NUnit.Framework;

namespace CheckoutService.IntegrationTests.Infrastructure;

public sealed class DbAsserts
{
    private readonly string _connString;

    public DbAsserts(string connString) => _connString = connString;

    public async Task AssertCheckoutPersistedAsync(long saleId, decimal expectedTotal, IReadOnlyList<CheckoutItem> expectedItems)
    {
        await using var con = new SqlConnection(_connString);
        await con.OpenAsync();

        var hdr = await con.QuerySingleOrDefaultAsync<SalesHdr>(
            @"SELECT id, total, payment_status AS PaymentStatus
              FROM sales_hdr
              WHERE id=@id",
            new { id = saleId });

        Assert.That(hdr, Is.Not.Null, "sales_hdr row not found for saleId.");
        Assert.Multiple(() =>
        {
            Assert.That(hdr!.Id, Is.EqualTo(saleId));
            Assert.That(hdr.Total, Is.EqualTo(expectedTotal));
            Assert.That(hdr.PaymentStatus, Is.Null, "payment_status should be NULL after /checkout.");
        });

        var lines = (await con.QueryAsync<SalesLin>(
            @"SELECT line_no AS [LineNo],
                    sku     AS [Sku],
                    price   AS [Price]
            FROM sales_lin
            WHERE hdr_id=@id
            ORDER BY line_no",
            new { id = saleId })).ToList();

        Assert.That(lines.Count, Is.EqualTo(expectedItems.Count), "sales_lin count mismatch.");

        for (var i = 0; i < expectedItems.Count; i++)
        {
            var expected = expectedItems[i];
            var actual = lines[i];

            Assert.Multiple(() =>
            {
                Assert.That(actual.LineNo, Is.EqualTo(i + 1));
                Assert.That(actual.Sku, Is.EqualTo(expected.sku));
                Assert.That(actual.Price, Is.EqualTo(expected.price));
            });
        }
    }

    public async Task AssertPaymentStatusAsync(long saleId, string expectedStatus)
    {
        await using var con = new SqlConnection(_connString);
        await con.OpenAsync();

        var status = await con.ExecuteScalarAsync<string?>(
            "SELECT payment_status FROM sales_hdr WHERE id=@id",
            new { id = saleId });

        Assert.That(status, Is.EqualTo(expectedStatus),
            $"Expected payment_status='{expectedStatus}' for saleId={saleId}.");
    }

    private sealed record SalesHdr(long Id, decimal Total, string? PaymentStatus);
    private sealed record SalesLin(int LineNo, string Sku, decimal Price);
}
