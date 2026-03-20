using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings;

[TestFixture]
public class EmployeeMappingTests
{
    [Test]
    public void ShouldPersistEmployeeShape()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("persisted-user", "Persisted User");

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.SaveChanges();
        }

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydratedEmployee = context.Set<Employee>()
                .Single(e => e.Id == employee.Id);

            rehydratedEmployee.Id.ShouldBe(employee.Id);
            rehydratedEmployee.UserName.ShouldBe("persisted-user");
            rehydratedEmployee.FullName.ShouldBe("Persisted User");
        }
    }
}
