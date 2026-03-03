using Microsoft.AspNetCore.Http.HttpResults;
using System.Linq.Expressions;

var builder = WebApplication.CreateBuilder(args);

//1. Agregar los servicios al contenedor
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

//2. Configurar el pipeline de la aplicación
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//3. Simulación de base de datos
var EventDB = new List<EventDto>
{
    new (1,"Concierto ITM",50000,100)
};

//4. Definir los endpoints de la API
app.MapGet("/api/events/{id}", (int id) =>
{
    var item = EventDB.FirstOrDefault(b => b.EventId == id);

    return item is not null ? Results.Ok(item) : Results.NotFound();
})
.WithName("GetEvent")
.WithOpenApi();

app.MapPost("/api/events/reserve", (int EventId, int Quantity, EventDto request) =>
{
    var item = EventDB.FirstOrDefault(p => p.EventId == EventId);
    if (item is null || item.Quantity < Quantity) return Results.BadRequest("Cantidad de sillas insufientes.");

    var index = EventDB.IndexOf(item);
    EventDB[index] = item with { Quantity = item.Quantity - request.Quantity };
    Console.WriteLine($"[COMPENSACIÓN] Se llevaron {Quantity} unidades del producto. Nuevo Stock: {EventDB[index].Quantity}");
    return Results.Ok(new { Message = "Las sillas fueron reservadas.", EventDto = EventDB[index] } );
});

app.MapPost("/api/events/release", (int EventId, int Quantity, EventDto request) =>
{
    var item = EventDB.FirstOrDefault(p => p.EventId == EventId);
    if (item is null) return Results.NotFound();

    var index = EventDB.IndexOf(item);
    EventDB[index] = item with { Quantity = item.Quantity + request.Quantity };
    Console.WriteLine($"[COMPENSACIÓN] Se devolvieron {Quantity} unidades del producto. Nuevo Stock: {EventDB[index].Quantity}");
    return Results.Ok(new {Message = "Las sillas fueron liberadas.", CurrentStock = EventDB[index] });
});

app.Run();