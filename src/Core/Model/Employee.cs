namespace ClearMeasure.Bootcamp.Core.Model;

public class Employee : EntityBase<Employee>, IComparable<Employee>
{
    public Employee()
    {
        UserName = string.Empty;
        FullName = string.Empty;
    }

    public Employee(string userName, string fullName)
    {
        UserName = userName;
        FullName = fullName;
    }

    public override Guid Id { get; set; } = Guid.NewGuid();

    public string UserName { get; set; }

    public string FullName { get; set; }

    public int CompareTo(Employee? other)
    {
        return string.Compare(FullName, other?.FullName, StringComparison.Ordinal);
    }

    public override string ToString()
    {
        return FullName;
    }
}
