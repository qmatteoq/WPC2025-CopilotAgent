using Flights.Api.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Flights.Api.Services;

public class FlightService
{
    private readonly List<Flight> _flights;
    private readonly ILogger<FlightService> _logger;

    public FlightService(ILogger<FlightService> logger)
    {
        _logger = logger;
        _flights = LoadFlightsFromJson();
        _logger.LogInformation("FlightService initialized with {FlightCount} flights", _flights.Count);
    }

    public IEnumerable<Flight> SearchFlights(FlightSearchRequest request)
    {
        _logger.LogInformation(
            "SearchFlights called - Origin: {Origin}, Destination: {Destination}, DepartureDate: {DepartureDate}, MaxResults: {MaxResults}",
            request.Origin ?? "(not specified)",
            request.Destination ?? "(not specified)",
            request.DepartureDate?.ToString() ?? "(not specified)",
            request.MaxResults);

        var query = _flights.AsEnumerable();

        if (!string.IsNullOrEmpty(request.Origin))
        {
            query = query.Where(f => f.Origin.Contains(request.Origin, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(request.Destination))
        {
            query = query.Where(f => f.Destination.Contains(request.Destination, StringComparison.OrdinalIgnoreCase));
        }

        if (request.DepartureDate.HasValue)
        {
            query = query.Where(f => DateOnly.FromDateTime(f.DepartureTime) == request.DepartureDate.Value);
        }

        var results = query.Take(request.MaxResults).ToList();
        
        _logger.LogInformation("SearchFlights returning {ResultCount} flights", results.Count);
        
        return results;
    }

    public Flight? GetFlightByNumber(string flightNumber)
    {
        _logger.LogInformation("GetFlightByNumber called - FlightNumber: {FlightNumber}", flightNumber);
        
        var flight = _flights.FirstOrDefault(f => f.FlightNumber.Equals(flightNumber, StringComparison.OrdinalIgnoreCase));
        
        if (flight != null)
        {
            _logger.LogInformation("GetFlightByNumber found flight: {FlightNumber} - {Airline} from {Origin} to {Destination}", 
                flight.FlightNumber, flight.Airline, flight.Origin, flight.Destination);
        }
        else
        {
            _logger.LogWarning("GetFlightByNumber - Flight not found: {FlightNumber}", flightNumber);
        }
        
        return flight;
    }

    public IEnumerable<string> GetAvailableOrigins()
    {
        _logger.LogInformation("GetAvailableOrigins called");
        
        var origins = _flights.Select(f => f.Origin).Distinct().OrderBy(o => o).ToList();
        
        _logger.LogInformation("GetAvailableOrigins returning {OriginCount} origins", origins.Count);
        
        return origins;
    }

    public IEnumerable<string> GetAvailableDestinations()
    {
        _logger.LogInformation("GetAvailableDestinations called");
        
        var destinations = _flights.Select(f => f.Destination).Distinct().OrderBy(d => d).ToList();
        
        _logger.LogInformation("GetAvailableDestinations returning {DestinationCount} destinations", destinations.Count);
        
        return destinations;
    }

    private List<Flight> LoadFlightsFromJson()
    {
        try
        {
            var jsonFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "flights.json");
            _logger.LogInformation("Loading flights from: {FilePath}", jsonFilePath);
            
            if (!File.Exists(jsonFilePath))
            {
                _logger.LogError("Flights data file not found at: {FilePath}", jsonFilePath);
                return new List<Flight>();
            }

            var jsonContent = File.ReadAllText(jsonFilePath);
            var flightDataList = JsonSerializer.Deserialize<List<FlightData>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (flightDataList == null)
            {
                _logger.LogError("Failed to deserialize flights data");
                return new List<Flight>();
            }

            var flights = new List<Flight>();
            var baseDate = DateTime.Today;

            foreach (var data in flightDataList)
            {
                var departureTime = baseDate.AddDays(data.DayOffset).AddHours(data.HourOffset);
                var arrivalTime = departureTime.AddHours(data.DurationHours);

                flights.Add(new Flight(
                    data.FlightNumber,
                    data.Airline,
                    data.Origin,
                    data.Destination,
                    departureTime,
                    arrivalTime,
                    data.Price,
                    data.Aircraft,
                    data.AvailableSeats,
                    Enum.Parse<FlightStatus>(data.Status)
                ));
            }

            _logger.LogInformation("Successfully loaded {FlightCount} flights from JSON", flights.Count);
            return flights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading flights from JSON");
            return new List<Flight>();
        }
    }

    private class FlightData
    {
        public string FlightNumber { get; set; } = string.Empty;
        public string Airline { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public double DurationHours { get; set; }
        public string Aircraft { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int AvailableSeats { get; set; }
        public string Status { get; set; } = string.Empty;
        public int DayOffset { get; set; }
        public double HourOffset { get; set; }
    }
}
