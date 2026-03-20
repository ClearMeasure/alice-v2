using DbUp;
using DbUp.Engine;
using DbUp.Helpers;
using DbUp.Support;
using JetBrains.Annotations;
using Microsoft.Data.SqlClient;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ClearMeasure.Bootcamp.Database.Console;

/// <summary>
/// Drops the target database if it exists, recreates it, then runs Create, Update, Everytime, and TestData scripts.
/// </summary>
[UsedImplicitly]
public class RebuildDatabaseCommand() : AbstractDatabaseCommand("Rebuild")
{
    protected override bool EnsureDatabaseExistsBeforeExecute() => false;

    protected override int ExecuteInternal(CommandContext context, DatabaseOptions options, string connectionString, CancellationToken cancellationToken)
    {
        DropTargetDatabaseIfExists(options, cancellationToken);
        EnsureDatabase.For.SqlDatabase(connectionString, new QuietLog());

        var scriptDir = GetScriptDirectory(options);

        // 1) RunOnce scripts: Create + Update (journaled)
        var createAndUpdateEngine = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsFromFileSystem(Path.Join(scriptDir, "Create"))
            .WithScriptsFromFileSystem(Path.Join(scriptDir, "Update"))
            .JournalToSqlTable("dbo", "SchemaVersions")
            .LogTo(new QuietLog())
            .Build();

        var createAndUpdateResult = createAndUpdateEngine.PerformUpgrade();
        if (!createAndUpdateResult.Successful)
        {
            return Fail(createAndUpdateResult.Error?.ToString() ?? "Could not run scripts to rebuild database.");
        }

        // 2) RunAlways scripts: things to re-apply each run (procs/views/perms)
        var everytimeEngine = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsFromFileSystem(Path.Join(scriptDir, "Everytime"),
                new SqlScriptOptions { ScriptType = ScriptType.RunAlways })
            .JournalTo(new NullJournal())
            .LogTo(new QuietLog())
            .Build();

        var everytimeResult = everytimeEngine.PerformUpgrade();
        if (!everytimeResult.Successful)
        {
            return Fail(everytimeResult.Error?.ToString() ?? "Failed to re-apply RunAlways scripts.");
        }

        // 3) Optional test data pass (journaled or not, your choice)
        var testDataEngine = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsFromFileSystem(Path.Join(scriptDir, "TestData"))
            .JournalToSqlTable("dbo", "SchemaVersions")
            .LogTo(new QuietLog())
            .Build();

        var testDataResult = testDataEngine.PerformUpgrade();
        if (!testDataResult.Successful)
        {
            return Fail(testDataResult.Error?.ToString() ?? "Failed to run TestData scripts.");
        }

        AnsiConsole.MarkupLine($"[green]Finished rebuilding {options.DatabaseName}.[/]");
        return 0;
    }

    /// <summary>
    /// Terminates other sessions, drops the database if present, and leaves it absent so DbUp can create a clean database.
    /// </summary>
    private static void DropTargetDatabaseIfExists(DatabaseOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var connection = new SqlConnection(GetMasterConnectionString(options));
        connection.Open();

        using var command = new SqlCommand(
            """
            IF EXISTS (SELECT 1 FROM sys.databases WHERE name = @dbName)
            BEGIN
                DECLARE @alter nvarchar(max) = N'ALTER DATABASE ' + QUOTENAME(@dbName) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE';
                EXEC sp_executesql @alter;
                DECLARE @drop nvarchar(max) = N'DROP DATABASE ' + QUOTENAME(@dbName);
                EXEC sp_executesql @drop;
            END
            """,
            connection);
        command.Parameters.AddWithValue("@dbName", options.DatabaseName);
        command.CommandTimeout = 120;
        command.ExecuteNonQuery();
    }
}
