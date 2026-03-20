using AutoBogus;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UnitTests;

internal class BogusOverrides : AutoGeneratorOverride
{
    public override bool CanOverride(AutoGenerateContext context)
    {
        return true;
    }

    public override void Generate(AutoGenerateOverrideContext context)
    {
        if (context.Instance is Employee employee && string.IsNullOrWhiteSpace(employee.FullName))
        {
            employee.FullName = $"{context.Faker.Name.FirstName()} {context.Faker.Name.LastName()}";
        }
    }
}
