using Flights.Api.Mcp;
using Flights.Api.Models;
using Flights.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.AI;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddSingleton<FlightService>();

// Add authentication with Microsoft Entra ID
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        
        // Add event handlers for debugging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validated successfully");
                var claims = context.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}");
                Console.WriteLine($"Claims: {string.Join(", ", claims ?? Array.Empty<string>())}");
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var token = context.Request.Headers.Authorization.ToString();
                Console.WriteLine($"Token received: {(string.IsNullOrEmpty(token) ? "None" : "Present")}");
                return Task.CompletedTask;
            }
        };
    }, options =>
    {
        builder.Configuration.Bind("AzureAd", options);
    });

builder.Services.AddAuthorization();

// Add CORS policy to allow any web application to call the API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<FlightTools>();

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

// Enable CORS
app.UseCors();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map MCP endpoints
app.MapMcp("/mcp")
.RequireAuthorization();

// Search flights endpoint
app.MapGet("/flights/search", (
    FlightService flightService,
    string? origin,
    string? destination,
    DateOnly? departureDate,
    int maxResults = 10) =>
{
    var request = new FlightSearchRequest(origin, destination, departureDate, maxResults);
    var flights = flightService.SearchFlights(request);
    return Results.Ok(flights);
})
.WithName("SearchFlights")
.WithDescription("Search for flights by origin, destination, and departure date")
.RequireAuthorization();

// Get flight by flight number
app.MapGet("/flights/{flightNumber}", (FlightService flightService, string flightNumber) =>
{
    var flight = flightService.GetFlightByNumber(flightNumber);
    return flight is not null ? Results.Ok(flight) : Results.NotFound();
})
.WithName("GetFlightByNumber")
.WithDescription("Get detailed information about a specific flight by flight number")
.RequireAuthorization();

// Get all available origins
app.MapGet("/flights/airports/origins", (FlightService flightService) =>
{
    var origins = flightService.GetAvailableOrigins();
    return Results.Ok(origins);
})
.WithName("GetAvailableOrigins")
.WithDescription("Get list of all available departure airports")
.RequireAuthorization();

// Get all available destinations
app.MapGet("/flights/airports/destinations", (FlightService flightService) =>
{
    var destinations = flightService.GetAvailableDestinations();
    return Results.Ok(destinations);
})
.WithName("GetAvailableDestinations")
.WithDescription("Get list of all available destination airports")
.RequireAuthorization();

app.Run();
