var builder = DistributedApplication.CreateBuilder(args);

// Use the same container name, port, and password as the build script (BuildFunctions.ps1
// Get-ContainerName / Get-SqlServerPassword) so 'PrivateBuild.ps1' and Aspire share one
// SQL Server Docker container.  ContainerLifetime.Persistent keeps the container alive
// between Aspire restarts so data is preserved across sessions.
var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sql = builder.AddSqlServer("sql", sqlPassword)
    .WithContainerName("aisoftwarefactory-mssql")
    .WithHostPort(1433)
    .WithLifetime(ContainerLifetime.Persistent);

var sqlDb = sql.AddDatabase("SqlConnectionString", databaseName: "AISoftwareFactory");

var appInsights = builder.AddConnectionString("AppInsights");

var migrations = builder.AddProject<Projects.Database>("database")
    .WithReference(sqlDb)
    .WaitFor(sql)
    .WithArgs("update");

builder.AddProject<Projects.UI_Server>("ui-server")
    .WithReference(sqlDb)
    .WithReference(appInsights)
    .WaitForCompletion(migrations);

builder.AddProject<Projects.Worker>("worker")
    .WithReference(sqlDb)
    .WithReference(appInsights)
    .WaitForCompletion(migrations);

builder.Build().Run();
