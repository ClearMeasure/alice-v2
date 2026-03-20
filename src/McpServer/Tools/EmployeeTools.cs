using System.ComponentModel;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ModelContextProtocol.Server;

namespace ClearMeasure.Bootcamp.McpServer.Tools;

[McpServerToolType]
public class EmployeeTools
{
    [McpServerTool(Name = "list-employees"), Description("Lists all employees in the system with their identifier, username, and full name.")]
    public static async Task<string> ListEmployees(IBus bus)
    {
        var employees = await bus.Send(new EmployeeGetAllQuery());
        return JsonSerializer.Serialize(employees.Select(FormatEmployee).ToArray(),
            new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool(Name = "get-employee"), Description("Retrieves a single employee by username.")]
    public static async Task<string> GetEmployee(
        IBus bus,
        [Description("The employee username")] string username)
    {
        try
        {
            var employee = await bus.Send(new EmployeeByUserNameQuery(username));
            return JsonSerializer.Serialize(FormatEmployee(employee),
                new JsonSerializerOptions { WriteIndented = true });
        }
        catch (InvalidOperationException)
        {
            return $"No employee found with username '{username}'.";
        }
    }

    private static object FormatEmployee(Employee employee) => new
    {
        employee.Id,
        employee.UserName,
        employee.FullName
    };
}
