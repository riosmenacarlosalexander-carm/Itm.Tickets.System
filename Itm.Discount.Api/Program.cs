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

//3. Simulación de la base de datos
var DiscountDB = new List<DiscountDto>
{
    new ("ITM50",0.5m)
};

//4. Definir los endpoints de la API
app.MapGet("/api/discounts/{code}", (string code) =>
{
    var item = DiscountDB.FirstOrDefault(t => t.Codigo.ToString() == code);

    return item is not null ? Results.Ok(item) : Results.NotFound();
})
    .WithName("GetDiscount")
    .WithOpenApi();
app.Run();