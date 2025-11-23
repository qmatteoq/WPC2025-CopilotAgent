namespace Flights.Api.Models;

public record Flight(
    string FlightNumber,
    string Airline,
    string Origin,
    string Destination,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    decimal Price,
    string AircraftType,
    int AvailableSeats,
    FlightStatus Status
);

public enum FlightStatus
{
    OnTime,
    Delayed,
    Cancelled,
    Boarding,
    Departed,
    Landed
}

public record FlightSearchRequest(
    string? Origin = null,
    string? Destination = null,
    DateOnly? DepartureDate = null,
    int MaxResults = 10
);
