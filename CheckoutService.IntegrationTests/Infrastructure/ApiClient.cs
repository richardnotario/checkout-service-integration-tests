using System.Net.Http.Json;

namespace CheckoutService.IntegrationTests.Infrastructure;

public sealed class ApiClient : IDisposable
{
    private readonly HttpClient _http;

    public ApiClient(string baseUrl)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public async Task<CheckoutResponse> CheckoutAsync(IEnumerable<CheckoutItem> items)
    {
        var res = await _http.PostAsJsonAsync("/checkout", new { items });
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<CheckoutResponse>();
        return body ?? throw new InvalidOperationException("Checkout response body was null.");
    }

    public Task<HttpResponseMessage> PayAsync(long saleId, string cardNumber, decimal amount)
    {
        return _http.PostAsJsonAsync("/payment", new { saleId, cardNumber, amount });
    }

    // Optional wrappers (only keep if you still want the old method names)
    public Task<CheckoutResponse> PostCheckoutAsync(IEnumerable<CheckoutItem> items) => CheckoutAsync(items);

    public async Task PostPaymentAsync(PaymentRequest req)
    {
        var res = await PayAsync(req.SaleId, req.CardNumber, req.Amount);
        res.EnsureSuccessStatusCode();
    }

    public void Dispose() => _http.Dispose();
}

public sealed record CheckoutItem(string sku, decimal price);
public sealed record CheckoutResponse(long SaleId, decimal Total);

public sealed record PaymentRequest(long SaleId, string CardNumber, decimal Amount);