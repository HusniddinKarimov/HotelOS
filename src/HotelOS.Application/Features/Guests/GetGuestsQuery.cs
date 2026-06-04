using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Guests;

/// <summary>Paged, searchable list of guests.</summary>
public class GetGuestsQuery : PagedQueryBase, IRequest<PagedResult<GuestDto>>;

public class GetGuestsQueryHandler : IRequestHandler<GetGuestsQuery, PagedResult<GuestDto>>
{
    private readonly IUnitOfWork _uow;
    public GetGuestsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<GuestDto>> Handle(GetGuestsQuery request, CancellationToken ct)
    {
        var query = _uow.Repository<Guest>().Query();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(g => g.FullName.ToLower().Contains(term)
                                  || g.Email.ToLower().Contains(term)
                                  || g.Phone.Contains(term));
        }

        query = request.Descending
            ? query.OrderByDescending(g => g.FullName)
            : query.OrderBy(g => g.FullName);

        var total = await query.CountAsync(ct);
        var guests = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<GuestDto>(guests.Select(g => g.ToDto()).ToList(), request.Page, request.PageSize, total);
    }
}
