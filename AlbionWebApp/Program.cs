using AlbionWebApp.Data;
using AlbionWebApp.Options;
using AlbionWebApp.Services;
using AlbionWebApp.Models;
using Microsoft.EntityFrameworkCore;
using AlbionWebApp.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurações da AODP (seção "Aodp" do appsettings) via Options pattern.
builder.Services.Configure<AodpOptions>(
    builder.Configuration.GetSection(AodpOptions.SectionName));

builder.Services.AddScoped<SearchItemsService>();
builder.Services.AddScoped<ItemLabelRepository>();


builder.Services.AddHttpClient();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Search Itens API v1")
    );
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
