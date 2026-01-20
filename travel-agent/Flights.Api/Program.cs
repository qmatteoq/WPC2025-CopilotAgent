using Flights.Api.Mcp;
using Flights.Api.Models;
using Flights.Api.Services;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.AI;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddSingleton<FlightService>();

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

// Configure OpenAPI with metadata for both REST APIs and MCP endpoints
builder.Services.AddOpenApi(options =>
{
    // Add schema transformer to fix Azure API Management compatibility
    // Azure APIM doesn't support OpenAPI 3.1 type arrays like ["integer", "string"]
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        // Fix type arrays - Azure APIM expects single type, not array
        // JsonSchemaType is a flags enum, so we check if it has multiple types set
        if (schema.Type is not null)
        {
            var type = schema.Type.Value;
            
            // Check if both Integer and String flags are set (common for query params)
            if (type.HasFlag(JsonSchemaType.Integer) && type.HasFlag(JsonSchemaType.String))
            {
                // Keep only Integer for numeric types
                schema.Type = JsonSchemaType.Integer;
                // Remove the pattern that was added for string parsing
                schema.Pattern = null;
            }
            else if (type.HasFlag(JsonSchemaType.Number) && type.HasFlag(JsonSchemaType.String))
            {
                // Keep only Number for numeric types
                schema.Type = JsonSchemaType.Number;
                schema.Pattern = null;
            }
        }
        
        return Task.CompletedTask;
    });
    
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Flights API",
            Version = "v1",
            Description = "Flight search and booking API with MCP (Model Context Protocol) support.\n\n" +
                          "## REST API Endpoints\n" +
                          "Standard REST endpoints for flight search and retrieval.\n\n" +
                          "## MCP Endpoints\n" +
                          "This API also exposes MCP tools at `/mcp` for AI agent integration:\n\n" +
                          "- **SearchFlights**: Search for flights by origin, destination, and departure date\n" +
                          "- **GetFlightDetails**: Get detailed information about a specific flight\n" +
                          "- **GetAirportsOrigins**: Get list of all available departure airports\n" +
                          "- **GetAirportsDestinations**: Get list of all available destination airports\n\n" +
                          "MCP endpoints use the Model Context Protocol for AI tool calling.",
            Contact = new OpenApiContact
            {
                Name = "Flights API Support"
            }
        };

        // Add tags to the document
        if (document.Tags is not null)
        {
            document.Tags.Add(new OpenApiTag
            {
                Name = "Flights",
                Description = "Flight search and retrieval operations"
            });
            document.Tags.Add(new OpenApiTag
            {
                Name = "MCP",
                Description = "Model Context Protocol endpoints for AI agent integration. Connect to /mcp using HTTP streaming transport."
            });
        }

        // Add MCP endpoint to paths (MCP uses HTTP streaming transport, not auto-documented by ASP.NET)
        var mcpTag = new OpenApiTagReference("MCP");
        
        var mcpSsePathItem = new OpenApiPathItem
        {
            Operations = new Dictionary<HttpMethod, OpenApiOperation>
            {
                [HttpMethod.Get] = new OpenApiOperation
                {
                    Tags = new HashSet<OpenApiTagReference> { mcpTag },
                    Summary = "MCP HTTP streaming endpoint",
                    Description = "Establishes an HTTP streaming connection for MCP communication using the Streamable HTTP transport. " +
                                  "This endpoint is used by MCP clients to maintain a persistent connection and receive messages from the server. " +
                                  "The client should send JSON-RPC messages to the /mcp/message endpoint.",
                    OperationId = "McpHttpStream",
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "HTTP streaming connection established. Uses chunked transfer encoding for persistent connection."
                        },
                        ["401"] = new OpenApiResponse
                        {
                            Description = "Unauthorized - Valid authentication token required"
                        }
                    }
                }
            }
        };

        var mcpMessagePathItem = new OpenApiPathItem
        {
            Operations = new Dictionary<HttpMethod, OpenApiOperation>
            {
                [HttpMethod.Post] = new OpenApiOperation
                {
                    Tags = new HashSet<OpenApiTagReference> { mcpTag },
                    Summary = "MCP Message endpoint",
                    Description = "Sends JSON-RPC messages to the MCP server. This endpoint handles tool invocations " +
                                  "and other MCP protocol messages. Available tools:\n\n" +
                                  "- **SearchFlights**: Search flights by origin, destination, and date\n" +
                                  "- **GetFlightDetails**: Get details for a specific flight number\n" +
                                  "- **GetAirportsOrigins**: List all departure airports\n" +
                                  "- **GetAirportsDestinations**: List all destination airports",
                    OperationId = "McpMessage",
                    RequestBody = new OpenApiRequestBody
                    {
                        Description = "JSON-RPC 2.0 message for MCP protocol",
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Type = JsonSchemaType.Object,
                                    Properties = new Dictionary<string, IOpenApiSchema>
                                    {
                                        ["jsonrpc"] = new OpenApiSchema
                                        {
                                            Type = JsonSchemaType.String,
                                            Description = "JSON-RPC version, must be \"2.0\""
                                        },
                                        ["id"] = new OpenApiSchema
                                        {
                                            Type = JsonSchemaType.String,
                                            Description = "Request identifier"
                                        },
                                        ["method"] = new OpenApiSchema
                                        {
                                            Type = JsonSchemaType.String,
                                            Description = "MCP method name (e.g., \"tools/call\", \"tools/list\")"
                                        },
                                        ["params"] = new OpenApiSchema
                                        {
                                            Type = JsonSchemaType.Object,
                                            Description = "Method parameters"
                                        }
                                    }
                                }
                            }
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "JSON-RPC response with result or error"
                        },
                        ["401"] = new OpenApiResponse
                        {
                            Description = "Unauthorized - Valid authentication token required"
                        }
                    }
                }
            }
        };

        document.Paths["/mcp"] = mcpSsePathItem;
        document.Paths["/mcp/message"] = mcpMessagePathItem;

        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
// OpenAPI and Scalar UI are available in all environments
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("Flights API");
    options.WithTheme(ScalarTheme.BluePlanet);
    options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

//app.UseHttpsRedirection();

// Enable CORS
app.UseCors();

// Map MCP endpoints - Authentication handled by APIM
app.MapMcp("/mcp");

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
.WithSummary("Search for available flights")
.WithDescription("Search for flights by origin, destination, and departure date. All parameters are optional.")
.WithTags("Flights");

// Get flight by flight number
app.MapGet("/flights/{flightNumber}", (FlightService flightService, string flightNumber) =>
{
    var flight = flightService.GetFlightByNumber(flightNumber);
    return flight is not null ? Results.Ok(flight) : Results.NotFound();
})
.WithName("GetFlightByNumber")
.WithSummary("Get flight details by flight number")
.WithDescription("Get detailed information about a specific flight by its flight number (e.g., 'BA112', 'AA100')")
.WithTags("Flights");

// Get all available origins
app.MapGet("/flights/airports/origins", (FlightService flightService) =>
{
    var origins = flightService.GetAvailableOrigins();
    return Results.Ok(origins);
})
.WithName("GetAvailableOrigins")
.WithSummary("Get all available departure airports")
.WithDescription("Returns a list of all departure airports/cities available in the system")
.WithTags("Flights");

// Get all available destinations
app.MapGet("/flights/airports/destinations", (FlightService flightService) =>
{
    var destinations = flightService.GetAvailableDestinations();
    return Results.Ok(destinations);
})
.WithName("GetAvailableDestinations")
.WithSummary("Get all available destination airports")
.WithDescription("Returns a list of all destination airports/cities available in the system")
.WithTags("Flights");

app.Run();
