using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess.Messaging;
using ClearMeasure.HostedEndpoint;
using ClearMeasure.HostedEndpoint.Configuration;
using Worker.Messaging;

namespace Worker;

public class WorkerEndpoint : ClearHostedEndpoint
{
    private const string EndpointName = "BackgroundProcessing";
    private const string SchemaName = "nServiceBus";

    public WorkerEndpoint(IConfiguration configuration) : base(configuration)
    {
        EndpointOptions = new()
        {
            EndpointName = EndpointName,
            EnableInstallers = true,
            EnableMetrics = true,
            EnableOutbox = true,
            MaxConcurrency = Environment.ProcessorCount * 2,
            ImmediateRetryCount = 3,
            DelayedRetryCount = 3
        };

        SqlPersistenceOptions = new()
        {
            ConnectionString = Configuration.GetConnectionString("SqlConnectionString"),
            Schema = SchemaName,
            EnableSagaPersistence = false,
            EnableSubscriptionStorage = true
        };
    }

    protected override EndpointOptions EndpointOptions { get; }

    protected override SqlPersistenceOptions SqlPersistenceOptions { get; }

    protected override void ConfigureTransport(EndpointConfiguration endpointConfiguration)
    {
        endpointConfiguration.EnableOpenTelemetry();

        var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
        transport.ConnectionString(SqlPersistenceOptions.ConnectionString);
        transport.DefaultSchema(SqlPersistenceOptions.Schema);
        transport.Transactions(TransportTransactionMode.TransactionScope);
        transport.Transport.TransportTransactionMode = TransportTransactionMode.ReceiveOnly;

        var conventions = new MessagingConventions();
        endpointConfiguration.Conventions().Add(conventions);
    }

    protected override void RegisterDependencyInjection(IServiceCollection services)
    {
        var apiUrl = Configuration["RemotableBus:ApiUrl"]
                     ?? throw new InvalidOperationException("RemotableBus:ApiUrl configuration is required.");

        services.AddHttpClient();
        services.AddSingleton<IBus>(sp =>
            new RemotableBus(sp.GetRequiredService<HttpClient>(), apiUrl));
    }
}
