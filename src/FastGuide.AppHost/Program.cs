var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddSqlite("fastguide-db");

var api = builder.AddProject<Projects.FastGuide_Api>("fastguide-api")
    .WithReference(database)
    .WithEnvironment("ConnectionStrings__FastGuide", "Data Source=fastguide.db")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

builder.AddProject<Projects.FastGuide_Ingestion>("fastguide-ingestion")
    .WithReference(database)
    .WithReference(api)
    .WithEnvironment("ConnectionStrings__FastGuide", "Data Source=fastguide.db");

builder.Build().Run();
