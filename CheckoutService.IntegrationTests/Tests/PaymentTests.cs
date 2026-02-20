using NUnit.Framework;
using CheckoutService.IntegrationTests.Infrastructure;

namespace CheckoutService.IntegrationTests.Tests;

[TestFixture]
public sealed class PaymentTests
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

    [TestCase("4111111111111111", "APPROVED", Description = "TC-02: valid card results in APPROVED")]
    [TestCase("4000000000000002", "DECLINED", Description = "TC-03: invalid card results in DECLINED")]
    public async Task Payment_Updates_PaymentStatus(string cardNumber, string expectedStatus)
    {
        var items = new List<CheckoutItem>
        {
            new("ABC", 12.34m),
            new("XYZ", 1.00m)
        };

        var checkout = await _api.CheckoutAsync(items);

        var payRes = await _api.PayAsync(checkout.saleId, cardNumber, checkout.total);
        Assert.That(payRes.IsSuccessStatusCode, Is.True, "Expected 2xx from /payment.");

        await _db.AssertPaymentStatusAsync(checkout.saleId, expectedStatus);
    }
}
