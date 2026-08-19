using Bff.Api.Services;
using Bff.Application;
using Bff.Domain.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(); // all [ApiController] classes are auto-discovered
builder.Services.AddOpenApi();
builder.Services.AddBffApplication();
builder.Services.AddHttpClient<IEventClient, EventClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["EventApi:BaseUrl"]!);
});
builder.Services.AddHttpClient<IAgendaClient, AgendaClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AgendaApi:BaseUrl"]!);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();                        // OpenAPI JSON at /openapi/v1.json
app.MapScalarApiReference();             // Scalar API reference UI at /scalar/v1

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
