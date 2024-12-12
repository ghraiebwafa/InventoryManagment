using InventoryConsumer.Models;
using InventoryConsumer.Services;
using Microsoft.EntityFrameworkCore;
using OpenSearch.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IOpenSearchClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var uriString = configuration["OpenSearch:Uri"];

    if (string.IsNullOrEmpty(uriString))
    {
        throw new ArgumentNullException(nameof(uriString), "OpenSearch URI is not configured. Please check your appsettings.json or environment variables.");
    }

    var uri = new Uri(uriString);

    try
    {
        var settings = new ConnectionSettings(uri)
            .DefaultIndex("inventory-updates")
            .BasicAuthentication(
                configuration["OpenSearch:Username"],
                configuration["OpenSearch:Password"])
            .EnableDebugMode();

        return new OpenSearchClient(settings);
    }
    catch (Exception ex)
    {
        
        throw new InvalidOperationException("Error configuring OpenSearch client.", ex);
    }
});

builder.Services.AddHostedService<ConsumerService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();