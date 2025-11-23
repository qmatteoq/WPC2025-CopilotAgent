using Flights.Api.Models;
using Microsoft.Extensions.Logging;

namespace Flights.Api.Services;

public class FlightService
{
    private readonly List<Flight> _flights;
    private readonly ILogger<FlightService> _logger;

    public FlightService(ILogger<FlightService> logger)
    {
        _logger = logger;
        _flights = GenerateMockFlights();
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

    private static List<Flight> GenerateMockFlights()
    {
        var baseDate = DateTime.Today;
        var flights = new List<Flight>();

        // Fixed mock flight data with predictable dates and times
        var mockFlights = new[]
        {
            // Today's flights
            (baseDate.AddHours(6), "BA112", "British Airways", "New York (JFK)", "London (LHR)", 7.5, "Boeing 777", 850, 25, FlightStatus.OnTime),
            (baseDate.AddHours(8), "AA100", "American Airlines", "New York (JFK)", "London (LHR)", 7.0, "Boeing 787", 950, 15, FlightStatus.Boarding),
            (baseDate.AddHours(10), "JL61", "Japan Airlines", "Los Angeles (LAX)", "Tokyo (NRT)", 11.0, "Boeing 787", 1200, 30, FlightStatus.OnTime),
            (baseDate.AddHours(12), "UA32", "United Airlines", "Los Angeles (LAX)", "Tokyo (NRT)", 10.5, "Boeing 777", 1150, 10, FlightStatus.Delayed),
            (baseDate.AddHours(14), "AF83", "Air France", "San Francisco (SFO)", "Paris (CDG)", 11.0, "Airbus A350", 1100, 20, FlightStatus.OnTime),
            (baseDate.AddHours(16), "EK201", "Emirates", "New York (JFK)", "Dubai (DXB)", 13.0, "Airbus A380", 1500, 5, FlightStatus.OnTime),
            (baseDate.AddHours(18), "B66", "JetBlue", "New York (JFK)", "Los Angeles (LAX)", 6.0, "Airbus A321", 350, 40, FlightStatus.OnTime),
            (baseDate.AddHours(20), "AS8", "Alaska Airlines", "Seattle (SEA)", "Boston (BOS)", 5.5, "Boeing 737", 450, 35, FlightStatus.OnTime),
            
            // Tomorrow's flights
            (baseDate.AddDays(1).AddHours(7), "BA112", "British Airways", "New York (JFK)", "London (LHR)", 7.5, "Boeing 777", 900, 30, FlightStatus.OnTime),
            (baseDate.AddDays(1).AddHours(9), "AA100", "American Airlines", "New York (JFK)", "London (LHR)", 7.0, "Boeing 787", 875, 22, FlightStatus.OnTime),
            (baseDate.AddDays(1).AddHours(11), "SQ37", "Singapore Airlines", "Los Angeles (LAX)", "Singapore (SIN)", 17.0, "Airbus A350", 1800, 12, FlightStatus.OnTime),
            (baseDate.AddDays(1).AddHours(13), "LH454", "Lufthansa", "San Francisco (SFO)", "Frankfurt (FRA)", 11.5, "Airbus A380", 1250, 18, FlightStatus.OnTime),
            (baseDate.AddDays(1).AddHours(15), "DL145", "Delta", "Seattle (SEA)", "Amsterdam (AMS)", 9.5, "Airbus A330", 950, 25, FlightStatus.OnTime),
            (baseDate.AddDays(1).AddHours(17), "EI137", "Aer Lingus", "Boston (BOS)", "Dublin (DUB)", 6.5, "Airbus A330", 650, 28, FlightStatus.OnTime),
            (baseDate.AddDays(1).AddHours(19), "UA1", "United Airlines", "San Francisco (SFO)", "New York (JFK)", 5.5, "Boeing 757", 400, 32, FlightStatus.OnTime),
            
            // Day after tomorrow
            (baseDate.AddDays(2).AddHours(6), "IB6251", "Iberia", "Chicago (ORD)", "Madrid (MAD)", 9.0, "Airbus A330", 1050, 20, FlightStatus.OnTime),
            (baseDate.AddDays(2).AddHours(8), "LA8084", "LATAM", "Miami (MIA)", "São Paulo (GRU)", 8.5, "Boeing 767", 750, 15, FlightStatus.OnTime),
            (baseDate.AddDays(2).AddHours(10), "UA870", "United Airlines", "Houston (IAH)", "Sydney (SYD)", 17.5, "Boeing 787", 1950, 8, FlightStatus.OnTime),
            (baseDate.AddDays(2).AddHours(12), "DL68", "Delta", "Atlanta (ATL)", "Rome (FCO)", 10.0, "Airbus A330", 1100, 18, FlightStatus.OnTime),
            (baseDate.AddDays(2).AddHours(14), "LH489", "Lufthansa", "Denver (DEN)", "Munich (MUC)", 10.5, "Airbus A350", 1200, 22, FlightStatus.OnTime),
            (baseDate.AddDays(2).AddHours(16), "AA1203", "American Airlines", "Chicago (ORD)", "Miami (MIA)", 3.0, "Boeing 737", 320, 45, FlightStatus.OnTime),
            (baseDate.AddDays(2).AddHours(18), "AA1345", "American Airlines", "Dallas (DFW)", "San Francisco (SFO)", 3.5, "Airbus A321", 380, 38, FlightStatus.OnTime),
            
            // 3 days from now
            (baseDate.AddDays(3).AddHours(7), "BA112", "British Airways", "New York (JFK)", "London (LHR)", 7.5, "Boeing 777", 825, 28, FlightStatus.OnTime),
            (baseDate.AddDays(3).AddHours(9), "JL61", "Japan Airlines", "Los Angeles (LAX)", "Tokyo (NRT)", 11.0, "Boeing 787", 1175, 20, FlightStatus.OnTime),
            (baseDate.AddDays(3).AddHours(11), "AF83", "Air France", "San Francisco (SFO)", "Paris (CDG)", 11.0, "Airbus A350", 1075, 24, FlightStatus.OnTime),
            (baseDate.AddDays(3).AddHours(13), "EK201", "Emirates", "New York (JFK)", "Dubai (DXB)", 13.0, "Airbus A380", 1450, 10, FlightStatus.OnTime),
            (baseDate.AddDays(3).AddHours(15), "SQ37", "Singapore Airlines", "Los Angeles (LAX)", "Singapore (SIN)", 17.0, "Airbus A350", 1750, 14, FlightStatus.OnTime),
            (baseDate.AddDays(3).AddHours(17), "B66", "JetBlue", "New York (JFK)", "Los Angeles (LAX)", 6.0, "Airbus A321", 365, 42, FlightStatus.OnTime),
            
            // 4 days from now
            (baseDate.AddDays(4).AddHours(8), "AA100", "American Airlines", "New York (JFK)", "London (LHR)", 7.0, "Boeing 787", 920, 19, FlightStatus.OnTime),
            (baseDate.AddDays(4).AddHours(10), "UA32", "United Airlines", "Los Angeles (LAX)", "Tokyo (NRT)", 10.5, "Boeing 777", 1125, 16, FlightStatus.OnTime),
            (baseDate.AddDays(4).AddHours(12), "LH454", "Lufthansa", "San Francisco (SFO)", "Frankfurt (FRA)", 11.5, "Airbus A380", 1225, 21, FlightStatus.OnTime),
            (baseDate.AddDays(4).AddHours(14), "DL145", "Delta", "Seattle (SEA)", "Amsterdam (AMS)", 9.5, "Airbus A330", 925, 27, FlightStatus.OnTime),
            (baseDate.AddDays(4).AddHours(16), "EI137", "Aer Lingus", "Boston (BOS)", "Dublin (DUB)", 6.5, "Airbus A330", 625, 30, FlightStatus.OnTime),
            (baseDate.AddDays(4).AddHours(18), "AS8", "Alaska Airlines", "Seattle (SEA)", "Boston (BOS)", 5.5, "Boeing 737", 425, 33, FlightStatus.OnTime),
            
            // 5 days from now
            (baseDate.AddDays(5).AddHours(7), "IB6251", "Iberia", "Chicago (ORD)", "Madrid (MAD)", 9.0, "Airbus A330", 1025, 23, FlightStatus.OnTime),
            (baseDate.AddDays(5).AddHours(9), "LA8084", "LATAM", "Miami (MIA)", "São Paulo (GRU)", 8.5, "Boeing 767", 725, 18, FlightStatus.OnTime),
            (baseDate.AddDays(5).AddHours(11), "UA870", "United Airlines", "Houston (IAH)", "Sydney (SYD)", 17.5, "Boeing 787", 1900, 11, FlightStatus.OnTime),
            (baseDate.AddDays(5).AddHours(13), "DL68", "Delta", "Atlanta (ATL)", "Rome (FCO)", 10.0, "Airbus A330", 1075, 20, FlightStatus.OnTime),
            (baseDate.AddDays(5).AddHours(15), "LH489", "Lufthansa", "Denver (DEN)", "Munich (MUC)", 10.5, "Airbus A350", 1175, 25, FlightStatus.OnTime),
            (baseDate.AddDays(5).AddHours(17), "UA1", "United Airlines", "San Francisco (SFO)", "New York (JFK)", 5.5, "Boeing 757", 415, 30, FlightStatus.OnTime),
            
            // 6 days from now
            (baseDate.AddDays(6).AddHours(6), "BA112", "British Airways", "New York (JFK)", "London (LHR)", 7.5, "Boeing 777", 875, 26, FlightStatus.OnTime),
            (baseDate.AddDays(6).AddHours(8), "JL61", "Japan Airlines", "Los Angeles (LAX)", "Tokyo (NRT)", 11.0, "Boeing 787", 1225, 17, FlightStatus.OnTime),
            (baseDate.AddDays(6).AddHours(10), "AF83", "Air France", "San Francisco (SFO)", "Paris (CDG)", 11.0, "Airbus A350", 1125, 22, FlightStatus.OnTime),
            (baseDate.AddDays(6).AddHours(12), "EK201", "Emirates", "New York (JFK)", "Dubai (DXB)", 13.0, "Airbus A380", 1475, 9, FlightStatus.OnTime),
            (baseDate.AddDays(6).AddHours(14), "SQ37", "Singapore Airlines", "Los Angeles (LAX)", "Singapore (SIN)", 17.0, "Airbus A350", 1775, 13, FlightStatus.OnTime),
            (baseDate.AddDays(6).AddHours(16), "AA1203", "American Airlines", "Chicago (ORD)", "Miami (MIA)", 3.0, "Boeing 737", 335, 44, FlightStatus.OnTime),
            (baseDate.AddDays(6).AddHours(18), "AA1345", "American Airlines", "Dallas (DFW)", "San Francisco (SFO)", 3.5, "Airbus A321", 395, 36, FlightStatus.OnTime),
        };

        foreach (var (departureTime, flightNumber, airline, origin, destination, durationHours, aircraft, price, availableSeats, status) in mockFlights)
        {
            var arrivalTime = departureTime.AddHours(durationHours);

            flights.Add(new Flight(
                flightNumber,
                airline,
                origin,
                destination,
                departureTime,
                arrivalTime,
                price,
                aircraft,
                availableSeats,
                status
            ));
        }

        return flights;
    }
}
