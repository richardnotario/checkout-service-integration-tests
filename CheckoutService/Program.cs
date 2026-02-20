using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Dapper;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<SqlOptions>(builder.Configuration.GetSection("Sql"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<SqlOptions>>().Value);
builder.Services.AddHttpClient();
    
var app = builder.Build();

app.MapPost("/checkout", async (CheckoutRequest req, SqlOptions cfg) =>
{
    await using var con = new SqlConnection(cfg.ConnectionString);
    await con.OpenAsync();

    using var tx = con.BeginTransaction();
    var hdrId = await con.ExecuteScalarAsync<long>(
        "INSERT INTO sales_hdr(sale_date,total) OUTPUT inserted.id VALUES (GETUTCDATE(), @total)",
        new { total = req.Items.Sum(i => i.Price) }, tx);

    foreach (var (sku, price, idx) in req.Items.Select((i, i2) => (i.Sku, i.Price, i2)))
    {
        await con.ExecuteAsync(
            "INSERT INTO sales_lin(hdr_id,line_no,sku,price) VALUES (@h,@n,@s,@p)",
            new { h = hdrId, n = idx + 1, s = sku, p = price }, tx);
    }
    tx.Commit();

    return Results.Ok(new { SaleId = hdrId, Total = req.Items.Sum(i => i.Price) });
});

app.MapPost("/payment", async (PaymentRequest req, HttpClient http, SqlOptions cfg) =>
{
    var res = await http.PostAsJsonAsync("http://payment-gateway:8080/pay", req);
    var body = await res.Content.ReadFromJsonAsync<PaymentResult>();

    await using var con = new SqlConnection(cfg.ConnectionString);
    await con.ExecuteAsync(
        "UPDATE sales_hdr SET payment_status=@s WHERE id=@id",
        new { s = body!.Status, id = req.SaleId });

    return Results.StatusCode((int)res.StatusCode);
});

app.Run();

record CheckoutRequest(List<CheckoutItem> Items);
record CheckoutItem(string Sku, decimal Price);
record PaymentRequest(long SaleId, string CardNumber, decimal Amount);
record PaymentResult(string Status);
class SqlOptions { public string ConnectionString { get; set; } = default!; }