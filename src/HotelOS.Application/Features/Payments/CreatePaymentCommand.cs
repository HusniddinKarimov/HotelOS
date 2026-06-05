using FluentValidation;
using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Application.Features.Billing;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Payments;

/// <summary>Records a payment against a bill and settles the bill when fully paid.</summary>
public record CreatePaymentCommand(Guid BillId, string Method, decimal Amount, string? Reference) : IRequest<BillDto>;

public class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.BillId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Method).Must(m => Enum.TryParse<PaymentMethod>(m, true, out _))
            .WithMessage("Method must be Cash, Card or BankTransfer.");
    }
}

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, BillDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IRealtimeNotifier _realtime;

    public CreatePaymentCommandHandler(IUnitOfWork uow, IRealtimeNotifier realtime)
    {
        _uow = uow;
        _realtime = realtime;
    }

    public async Task<BillDto> Handle(CreatePaymentCommand request, CancellationToken ct)
    {
        var bill = await _uow.Repository<Bill>().Query(tracking: true)
            .Include(b => b.Items).Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == request.BillId, ct)
            ?? throw new NotFoundException("Bill", request.BillId);

        if (bill.Status == BillStatus.Cancelled)
            throw new ConflictException("Cannot take payment on a cancelled bill.");

        await _uow.Repository<Payment>().AddAsync(new Payment
        {
            BillId = bill.Id,
            Method = Enum.Parse<PaymentMethod>(request.Method, true),
            Status = PaymentStatus.Completed,
            Amount = request.Amount,
            Reference = request.Reference,
            PaidAt = DateTime.UtcNow
        }, ct);

        // Settle the bill if this payment covers the balance.
        if (bill.Paid + request.Amount >= bill.Total)
            bill.Status = BillStatus.Paid;

        await _uow.SaveChangesAsync(ct);

        await _realtime.NotifyAsync(NotificationType.PaymentCompleted,
            $"Payment of £{request.Amount:0.00} received via {request.Method}.", ct: ct);

        var fresh = await _uow.Repository<Bill>().Query()
            .Include(b => b.Items).Include(b => b.Payments)
            .FirstAsync(b => b.Id == bill.Id, ct);
        return fresh.ToDto();
    }
}
