using Flights.Api.Models;
using Flights.Api.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Flights.Api.Mcp;

/// <summary>
/// Flight tools for MCP server
/// </summary>
[McpServerToolType]
public class FlightTools
{
    private readonly FlightService _flightService;
    private readonly ILogger<FlightTools> _logger;

    public FlightTools(FlightService flightService, ILogger<FlightTools> logger)
    {
        _flightService = flightService;
        _logger = logger;
        _logger.LogInformation("FlightTools MCP server initialized");
    }

    [McpServerTool, Description("Search for flights by origin, destination, and departure date. Date is optional, if the user doesn't provide it, don't ask for it. Returns a list of available flights matching the criteria.")]
    public IEnumerable<Flight> SearchFlights(
        [Description("Departure airport or city (e.g., 'New York', 'JFK', 'Los Angeles')")] string? origin = null,
        [Description("Arrival airport or city (e.g., 'London', 'LHR', 'Tokyo')")] string? destination = null,
        [Description("Departure date in YYYY-MM-DD format (e.g., '2025-01-20')")] DateOnly? departureDate = null,
        [Description("Maximum number of flights to return")] int maxResults = 10)
    {
        _logger.LogInformation(
            "MCP Tool 'SearchFlights' invoked - Origin: {Origin}, Destination: {Destination}, DepartureDate: {DepartureDate}, MaxResults: {MaxResults}",
            origin ?? "(not specified)",
            destination ?? "(not specified)",
            departureDate?.ToString() ?? "(not specified)",
            maxResults);

        var request = new FlightSearchRequest(origin, destination, departureDate, maxResults);
        var results = _flightService.SearchFlights(request);
        
        var resultList = results.ToList();
        _logger.LogInformation("MCP Tool 'SearchFlights' returning {ResultCount} flights", resultList.Count);
        
        return resultList;
    }

    [McpServerTool, Description("Get detailed information about a specific flight using its flight number. Returns flight details including airline, schedule, price, and status.")]
    public Flight? GetFlightDetails(
        [Description("The flight number (e.g., 'BA112', 'AA100', 'UA32')")] string flightNumber)
    {
        _logger.LogInformation("MCP Tool 'GetFlightDetails' invoked - FlightNumber: {FlightNumber}", flightNumber);
        
        var flight = _flightService.GetFlightByNumber(flightNumber);
        
        if (flight != null)
        {
            _logger.LogInformation(
                "MCP Tool 'GetFlightDetails' found flight: {FlightNumber} - {Airline} from {Origin} to {Destination}",
                flight.FlightNumber, flight.Airline, flight.Origin, flight.Destination);
        }
        else
        {
            _logger.LogWarning("MCP Tool 'GetFlightDetails' - Flight not found: {FlightNumber}", flightNumber);
        }
        
        return flight;
    }

    [McpServerTool, Description("Get a list of all available departure airports/cities in the system.")]
    public IEnumerable<string> GetAirportsOrigins()
    {
        _logger.LogInformation("MCP Tool 'GetAirportsOrigins' invoked");
        
        var origins = _flightService.GetAvailableOrigins();
        
        var originList = origins.ToList();
        _logger.LogInformation("MCP Tool 'GetAirportsOrigins' returning {OriginCount} origins", originList.Count);
        
        return originList;
    }

    [McpServerTool, Description("Get a list of all available destination airports/cities in the system.")]
    public IEnumerable<string> GetAirportsDestinations()
    {
        _logger.LogInformation("MCP Tool 'GetAirportsDestinations' invoked");
        
        var destinations = _flightService.GetAvailableDestinations();
        
        var destinationList = destinations.ToList();
        _logger.LogInformation("MCP Tool 'GetAirportsDestinations' returning {DestinationCount} destinations", destinationList.Count);
        
        return destinationList;
    }
}
