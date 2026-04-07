using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add API project
var api = builder.AddProject("fastguide-api", "../FastGuide.Api/FastGuide.Api.csproj")
    .WithEnvironment("ConnectionStrings__FastGuide", "Data Source=fastguide.db")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

// Add Ingestion worker
builder.AddProject("fastguide-ingestion", "../FastGuide.Ingestion/FastGuide.Ingestion.csproj")
    .WithReference(api)
    .WithEnvironment("ConnectionStrings__FastGuide", "Data Source=fastguide.db");

builder.Build().Run();
