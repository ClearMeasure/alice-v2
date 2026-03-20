using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class EmployeeTests
{
    [Test]
    public void Constructor_WithValues_SetsProperties()
    {
        var employee = new Employee("bobjoe", "Bob Joe");

        employee.UserName.ShouldBe("bobjoe");
        employee.FullName.ShouldBe("Bob Joe");
        employee.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public void ToString_WithFullName_ReturnsFullName()
    {
        var employee = new Employee("bobjoe", "Bob Joe");

        employee.ToString().ShouldBe("Bob Joe");
    }

    [Test]
    public void CompareTo_WithDifferentNames_SortsByFullName()
    {
        var alpha = new Employee("alpha", "Alpha User");
        var beta = new Employee("beta", "Beta User");

        alpha.CompareTo(beta).ShouldBeLessThan(0);
        beta.CompareTo(alpha).ShouldBeGreaterThan(0);
    }

    [Test]
    public void Equality_WithMatchingIdentifiers_IsTrue()
    {
        var employee1 = new Employee("one", "Employee One");
        var employee2 = new Employee("two", "Employee Two") { Id = employee1.Id };

        employee1.ShouldBe(employee2);
        (employee1 == employee2).ShouldBeTrue();
    }
}
