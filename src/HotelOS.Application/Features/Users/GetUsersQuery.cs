using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Application.Features.Auth;
using HotelOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Users;

/// <summary>Paged list of staff users (Administrator only).</summary>
public class GetUsersQuery : PagedQueryBase, IRequest<PagedResult<UserDto>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly IUnitOfWork _uow;
    public GetUsersQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var query = _uow.Repository<User>().Query().Include(u => u.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(u => u.Username.ToLower().Contains(term)
                                  || u.FullName.ToLower().Contains(term)
                                  || u.Email.ToLower().Contains(term));
        }

        query = request.Descending
            ? query.OrderByDescending(u => u.Username)
            : query.OrderBy(u => u.Username);

        var total = await query.CountAsync(ct);
        var users = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var items = users
            .Select(u => new UserDto(u.Id, u.Username, u.Email, u.FullName, u.Role.Name, u.IsActive))
            .ToList();

        return new PagedResult<UserDto>(items, request.Page, request.PageSize, total);
    }
}
