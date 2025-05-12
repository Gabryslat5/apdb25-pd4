using Cwiczenia9_pd.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
//builder.Services.AddScoped<ITripsService, TripsService>();
//builder.Services.AddScoped<IClientsService, ClientsService>();

builder.Services.AddScoped<IWarehouseService, WarehouseService>();

builder.Services.AddSwaggerGen();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();