using FluentValidation;
using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Rooms;

/// <summary>Adds a new physical room to the inventory.</summary>
public record CreateRoomCommand(int Number, int Floor, int RoomTypeId, bool NearElevator) : IRequest<RoomDto>;

public class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator()
    {
        RuleFor(x => x.Number).InclusiveBetween(1, 9999);
        RuleFor(x => x.Floor).InclusiveBetween(0, 99);
        RuleFor(x => x.RoomTypeId).GreaterThan(0);
    }
}

public class CreateRoomCommandHandler : IRequestHandler<CreateRoomCommand, RoomDto>
{
    private readonly IUnitOfWork _uow;
    public CreateRoomCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<RoomDto> Handle(CreateRoomCommand request, CancellationToken ct)
    {
        var rooms = _uow.Repository<Room>();
        var types = _uow.Repository<RoomType>();

        if (await rooms.AnyAsync(r => r.Number == request.Number, ct))
            throw new ConflictException($"Room {request.Number} already exists.");

        var type = await types.GetByIdInt(request.RoomTypeId, ct)
            ?? throw new NotFoundException("RoomType", request.RoomTypeId);

        var room = new Room
        {
            Number = request.Number,
            Floor = request.Floor,
            RoomTypeId = type.Id,
            NearElevator = request.NearElevator,
            Status = RoomStatus.Clean,
            LastCleanedAt = DateTime.UtcNow
        };

        await rooms.AddAsync(room, ct);
        await _uow.SaveChangesAsync(ct);

        room.RoomType = type;
        return room.ToDto();
    }
}

/// <summary>Small helper to fetch int-keyed lookups via the generic repository.</summary>
internal static class RepoIntExtensions
{
    public static Task<RoomType?> GetByIdInt(this IGenericRepository<RoomType> repo, int id, CancellationToken ct) =>
        repo.FirstOrDefaultAsync(x => x.Id == id, ct);
}
