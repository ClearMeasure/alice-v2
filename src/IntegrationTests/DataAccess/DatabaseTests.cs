using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class DatabaseTests
{
    public void Clean()
    {
        new DatabaseEmptier(TestHost.GetRequiredService<DbContext>().Database).DeleteAllData();
    }
}