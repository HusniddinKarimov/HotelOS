using HotelOS.Application.Abstractions;
using HotelOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Users;

public record TechnicianDto(Guid Id, string FullName);

/// <summary>Lists maintenance technicians available for assignment.</summary>
public record GetTechniciansQuery : IRequest<IReadOnlyList<TechnicianDto>>;

public class GetTechniciansQueryHandler : IRequestHandler<GetTechniciansQuery, IReadOnlyList<TechnicianDto>>
{
    private readonly IUnitOfWork _uow;
    public GetTechniciansQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<TechnicianDto>> Handle(GetTechniciansQuery request, CancellationToken ct)
    {
        var techs = await _uow.Repository<User>().Query()
            .Include(u => u.Role)
            .Where(u => u.IsActive && u.Role.Name == RoleNames.MaintenanceStaff)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

        return techs.Select(u => new TechnicianDto(u.Id, u.FullName)).ToList();
    }
}
