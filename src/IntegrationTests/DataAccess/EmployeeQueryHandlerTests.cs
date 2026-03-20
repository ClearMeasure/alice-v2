using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class EmployeeQueryHandlerTests
{
    [Test]
    public async Task Handle_ByUserName_ReturnsMatchingEmployee()
    {
        new DatabaseTests().Clean();

        var one = new Employee("first-user", "First User");
        var two = new Employee("second-user", "Second User");

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(one);
            context.Add(two);
            context.SaveChanges();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new EmployeeQueryHandler(dataContext);

        var employee = await handler.Handle(new EmployeeByUserNameQuery("first-user"));

        employee.Id.ShouldBe(one.Id);
        employee.FullName.ShouldBe("First User");
    }

    [Test]
    public async Task Handle_GetAll_ReturnsEmployeesSortedByFullName()
    {
        new DatabaseTests().Clean();

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(new Employee("third-user", "Zulu Person"));
            context.Add(new Employee("first-user", "Alpha Person"));
            context.Add(new Employee("second-user", "Middle Person"));
            context.SaveChanges();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new EmployeeQueryHandler(dataContext);

        var employees = await handler.Handle(new EmployeeGetAllQuery());

        employees.Select(e => e.FullName).ShouldBe(["Alpha Person", "Middle Person", "Zulu Person"]);
    }
}
