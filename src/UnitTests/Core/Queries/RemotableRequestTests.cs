using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.UI.Client.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Queries;

public class RemotableRequestTests
{
    [Test]
    public void ShouldSerialize()
    {
        AssertRemotable(new ForecastQuery());
        AssertRemotable(new[] { ObjectMother.Faker<WeatherForecast>() });
        AssertRemotable(new HealthCheckRemotableRequest());
        AssertRemotable(HealthStatus.Degraded);
        AssertRemotable(new EmployeeGetAllQuery());
        AssertRemotable(new EmployeeByUserNameQuery("jsmith"));
    }

    [Test]
    public void ShouldBeRemotableCompatible()
    {
        AssertRemotable(new ServerHealthCheckQuery());

        var employee = ObjectMother.Faker<Employee>();
        AssertRemotable(employee);
    }

    public static object AssertRemotable(object theObject)
    {
        var rehydratedQuery = SimulateRemoteObject(theObject);

        ObjectMother.AssertAllProperties(theObject, rehydratedQuery);
        rehydratedQuery.GetType().FullName.ShouldBe(theObject.GetType().FullName);
        return rehydratedQuery;
    }

    public static object SimulateRemoteObject(object theObject)
    {
        var json = new WebServiceMessage(theObject).GetJson();
        var message = JsonSerializer.Deserialize<WebServiceMessage>(json);
        var rehydratedQuery = message!.GetBodyObject();
        return rehydratedQuery;
    }

    public static T SimulateRemoteObject<T>(T theObject)
    {
        return (T)SimulateRemoteObject((object)theObject!);
    }
}
