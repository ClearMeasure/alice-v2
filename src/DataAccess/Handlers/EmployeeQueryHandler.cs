using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class EmployeeQueryHandler(DataContext context)
    : IRequestHandler<EmployeeByUserNameQuery, Employee>,
        IRequestHandler<EmployeeGetAllQuery, Employee[]>
{
    public async Task<Employee> Handle(EmployeeByUserNameQuery request,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<Employee>()
            .SingleAsync(emp => emp.UserName == request.Username, cancellationToken);
    }

    public async Task<Employee[]> Handle(EmployeeGetAllQuery request, CancellationToken cancellationToken = default)
    {
        return await context.Set<Employee>()
            .OrderBy(e => e.FullName)
            .ToArrayAsync(cancellationToken);
    }
}
