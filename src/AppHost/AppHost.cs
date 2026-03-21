var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithDataVolume();

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
