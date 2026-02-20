using NUnit.Framework;
using CheckoutService.IntegrationTests.Infrastructure;

namespace CheckoutService.IntegrationTests.Tests;

[TestFixture]
public sealed class CheckoutTests
{
    private ApiClient _api = default!;
    private DbAsserts _db = default!;

    [SetUp]
    public void SetUp()
    {
        var cfg = TestConfig.Load();
        _api = new ApiClient(TestConfig.ApiBaseUrl(cfg));
        _db = new DbAsserts(TestConfig.SqlConnectionString(cfg));
    }

    [TearDown]
    public void TearDown() => _api.Dispose();

    [Test(Description = "TC-01: /checkout creates sales_hdr + sales_lin rows and returns SaleId/Total.")]
    public async Task Checkout_Persists_Header_And_Lines()
    {
        var items = new List<CheckoutItem>
        {
            new("ABC", 12.34m),
            new("XYZ", 1.00m)
        };

        var res = await _api.CheckoutAsync(items);

        Assert.Multiple(() =>
        {
            Assert.That(res.saleId, Is.GreaterThan(0));
            Assert.That(res.total, Is.EqualTo(13.34m));
        });

        await _db.AssertCheckoutPersistedAsync(res.saleId, 13.34m, items);
    }
}
