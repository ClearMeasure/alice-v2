using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.IntegrationTests;

[TestFixture]
public class ZDataLoader
{
    [Test]
    public void LoadData()
    {
        new DataAccess.DatabaseTests().Clean();
        var db = TestHost.GetRequiredService<DbContext>();

        foreach (var employee in BuildEmployees())
        {
            db.Add(employee);
        }

        db.SaveChanges();
        db.Dispose();
    }

    private static IEnumerable<Employee> BuildEmployees()
    {
        yield return new Employee("jpalermo", "Jeffrey Palermo");
        yield return new Employee("sspaniel", "Sean Spaniel");
        yield return new Employee("hsimpson", "Homer Simpson");
        yield return new Employee("tlovejoy", "Timothy Lovejoy Jr");
        yield return new Employee("gwillie", "Groundskeeper Willie MacDougal");
        yield return new Employee("nflanders", "Ned Flanders");
    }

    public Employee CreateUser()
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        var employee = TestHost.Faker<Employee>();
        employee.UserName = "current" + employee.UserName;
        if (string.IsNullOrWhiteSpace(employee.FullName))
        {
            employee.FullName = employee.UserName;
        }
        context.Add(employee);
        context.SaveChanges();
        return employee;
    }
}
