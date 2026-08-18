using Event.Application.UseCases;
using Event.Domain.Ports;
using Event.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(); // all [ApiController] classes are auto-discovered
builder.Services.AddOpenApi();

builder.Services.AddDbContext<EventDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("EventDb")));
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<CreateEventUseCase>();
builder.Services.AddScoped<GetEventByIdUseCase>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();                        // OpenAPI JSON at /openapi/v1.json
app.MapScalarApiReference();             // Scalar API reference UI at /scalar/v1

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
