var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Flights_Api>("flights-api");

var agent = builder.AddProject<Projects.TravelAgent>("travelagent")
    .WithReference(api);

builder.AddDevTunnel("agent")
    .WithReference(agent)
    .WithAnonymousAccess();

builder.AddDevTunnel("flightsapi")
    .WithReference(api)
    .WithAnonymousAccess();

builder.Build().Run();
