var builder = WebApplication.CreateBuilder(args);

//1. Servicios
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("EventClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5135");
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddStandardResilienceHandler();

builder.Services.AddHttpClient("DiscountClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5153");
    client.Timeout=TimeSpan.FromSeconds(5);
})
.AddStandardResilienceHandler();

var app = builder.Build();

//2. Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

//3. Endpoints y Paralelismo
app.MapPost("/api/bookings", static async (int EventId, int Tickets, string DiscountCode, IHttpClientFactory factory) =>
{
var eventClient = factory.CreateClient("EventClient");
var discountClient = factory.CreateClient("DiscountClient");
try
{
    //Lectura en paralelo
    var eventTask = eventClient.GetFromJsonAsync<EventDto>($"/api/events/{EventId}");
    var discountTask = discountClient.GetFromJsonAsync<DiscountDto>($"/api/discounts/{DiscountCode}");

    await Task.WhenAll(eventTask, discountTask);

    var eventData = eventTask.Result;
    var discountData = discountTask.Result;

        var total = (eventData.PrecioBase * eventData.Quantity) - (eventData.PrecioBase * discountData.Porcentaje);

        //Reservar sillas
        var reserveResponse = await eventClient.PostAsJsonAsync("/api/events/reserve", new { EventId, Quantity = Tickets });
        if (!reserveResponse.IsSuccessStatusCode)
            return Results.BadRequest("No hay sillas suficientes o el evento no existe.");

        try
        {
            //Simulación de pago
            bool paymentSuccess = new Random().Next(1, 10) > 5;
            if (!paymentSuccess) throw new Exception("Fondos insuficientes en la tarjeta de crédito.");
            return Results.Ok(new { Status = "Éxito", Message = "¡Disfruta el concierto ITM!" });
        }
        catch (Exception ex)
        {
            //Compensación
            Console.WriteLine($"[SAGA] Error en pago: {ex.Message}. Liberando sillas...");
            await eventClient.PostAsJsonAsync("/api/events/release", new { EventId, Quantity = Tickets });
            return Results.Problem("Tu pago fue rechazado. No te preocupes, no te cobramos y tus sillas fueron liberadas.");
        }
    }
    catch (Exception ext)
    {
        return Results.Problem($"Error en el ecosistema distribuido: {ext.Message}");
    }
});

//4. Modelos locales
internal record EventDto (int EventId, string Nombre, int PrecioBase, int Quantity);
internal record DiscountDto (string Codigo, decimal Porcentaje);