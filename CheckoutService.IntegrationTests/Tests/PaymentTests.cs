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

    [TestCase("4111111111111111", "APPROVED")]
    [TestCase("4000000000000002", "DECLINED")]
    public async Task Payment_Updates_PaymentStatus(string cardNumber, string expectedStatus)
    {
        var cfg = TestConfig.Load();
        var sql = TestConfig.SqlConnectionString(cfg);

        await DbCleanup.ResetAsync(sql);

        // 1) Create sale via /checkout
        var checkoutRes = await _api.CheckoutAsync(new[]
        {
            new CheckoutItem("ABC", 12.34m),
            new CheckoutItem("XYZ", 1.00m),
        });

        var saleId = checkoutRes.SaleId;
        var amount = checkoutRes.Total;

        // 2) Pay that exact saleId
        var payRes = await _api.PayAsync(saleId, cardNumber, amount);

        if (expectedStatus == "APPROVED")
        {
            Assert.That(payRes.IsSuccessStatusCode, Is.True,
                $"Expected APPROVED payment to succeed but got {(int)payRes.StatusCode} {payRes.ReasonPhrase}");
        }
        else if (expectedStatus == "DECLINED")
        {
            Assert.That((int)payRes.StatusCode, Is.EqualTo(402),
                $"Expected DECLINED payment to return 402 but got {(int)payRes.StatusCode} {payRes.ReasonPhrase}");
        }
        else
        {
            Assert.Fail($"Unexpected expectedStatus '{expectedStatus}' in test data.");
        }

        // 3) Assert that exact row
        await _db.AssertPaymentStatusAsync(saleId, expectedStatus);
    }
}