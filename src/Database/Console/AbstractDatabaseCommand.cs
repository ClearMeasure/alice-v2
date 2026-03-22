using DbUp;
using Microsoft.Data.SqlClient;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.Console;

public abstract class AbstractDatabaseCommand(string action) : Command<DatabaseOptions>
{
    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly string Action = action;


    protected static string GetScriptDirectory(DatabaseOptions options)
    {
        return Path.GetFullPath(options.ScriptDir);
    }

    public override int Execute(CommandContext context, DatabaseOptions options, CancellationToken cancellationToken)
    {
        ShowOptionsOnConsole(options);
        var connectionString = GetConnectionString(options);
        try
        {
            if (EnsureDatabaseExistsBeforeExecute())
            {
                EnsureDatabase.For.SqlDatabase(connectionString, new QuietLog());
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return -1;
        }

        return ExecuteInternal(context, options, connectionString, cancellationToken);
    }

    // ReSharper disable UnusedParameter.Global
    protected abstract int ExecuteInternal(CommandContext context, DatabaseOptions options, string connectionString, CancellationToken cancellationToken);
    // ReSharper restore UnusedParameter.Global

    protected static string GetConnectionString(DatabaseOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.DatabaseServer))
        {
            var envConnStr = Environment.GetEnvironmentVariable("ConnectionStrings__SqlConnectionString");
            if (!string.IsNullOrWhiteSpace(envConnStr))
                return envConnStr;
        }

        // Determine if this is a local server reached through the AppHost-managed host mapping.
        var serverName = (options.DatabaseServer ?? string.Empty).Trim();
        var isLocalServer = serverName.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                           serverName.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                           serverName.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
                           serverName.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                           serverName.StartsWith("localhost", StringComparison.OrdinalIgnoreCase);

        // Format DataSource to use TCP on port 1433 when a port is not already specified.
        var dataSource = options.DatabaseServer ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(dataSource))
        {
            if (!dataSource.Contains(',') && !dataSource.Contains(':') && !dataSource.Contains('\\'))
            {
                dataSource = $"{dataSource},1433";
            }
        }
        
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = dataSource,
            InitialCatalog = options.DatabaseName,
            ConnectTimeout = 60 
        };

        // Configure encryption and certificate trust based on server location
        // These must be explicitly set to ensure DbUp preserves them when creating master connections
        if (isLocalServer)
        {
            // Local AppHost-managed servers: don't encrypt, trust certificate
            builder.Encrypt = false;
            builder.TrustServerCertificate = true;
        }
        else
        {
            // Remote servers or Azure SQL Database: encrypt, don't trust certificate.
            builder.Encrypt = true;
            builder.TrustServerCertificate = false;
        }

        if (string.IsNullOrWhiteSpace(options.DatabaseUser))
        {
            // Use Windows Integrated Security
            builder.IntegratedSecurity = true;
        }
        else
        {
            // Use SQL Server Authentication
            if (string.IsNullOrWhiteSpace(options.DatabasePassword))
            {
                throw new ArgumentException("DatabasePassword is required when DatabaseUser is provided", "DatabasePassword");
            }
            
            builder.IntegratedSecurity = false;
            builder.UserID = options.DatabaseUser;
            builder.Password = options.DatabasePassword;
        }



        return builder.ToString();
    }

    /// <summary>
    /// When <see langword="true"/>, the target database is created if missing before <see cref="ExecuteInternal"/> runs.
    /// </summary>
    protected virtual bool EnsureDatabaseExistsBeforeExecute() => true;

    /// <summary>
    /// Connection string to <c>master</c> using the same server and credentials as <see cref="GetConnectionString"/>.
    /// </summary>
    protected static string GetMasterConnectionString(DatabaseOptions options)
    {
        var builder = new SqlConnectionStringBuilder(GetConnectionString(options))
        {
            InitialCatalog = "master"
        };
        return builder.ToString();
    }

    protected static int Fail(string message, int code = -1)
    {
        AnsiConsole.MarkupLine($"[red]{message.EscapeMarkup()}[/]");
        return code;
    }

    private void ShowOptionsOnConsole(DatabaseOptions options)
    {
        // Suppressed for clean build output; details available at DEBUG log level
    }
}
