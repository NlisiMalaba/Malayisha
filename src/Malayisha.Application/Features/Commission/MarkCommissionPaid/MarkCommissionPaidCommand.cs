using FluentValidation;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Commission.MarkCommissionPaid;

public sealed record MarkCommissionPaidCommand(Guid CommissionRecordId, Guid AdminUserId)
    : IRequest<Result<CommissionDto>>;

internal sealed class MarkCommissionPaidCommandValidator : AbstractValidator<MarkCommissionPaidCommand>
{
    public MarkCommissionPaidCommandValidator()
    {
        RuleFor(command => command.CommissionRecordId).NotEmpty();
        RuleFor(command => command.AdminUserId).NotEmpty();
    }
}

internal sealed class MarkCommissionPaidCommandHandler(
    ICommissionRecordRepository commissionRecordRepository,
    IAuditLogRepository auditLogRepository,
    TimeProvider timeProvider,
    ILogger<MarkCommissionPaidCommandHandler> logger)
    : IRequestHandler<MarkCommissionPaidCommand, Result<CommissionDto>>
{
    public async Task<Result<CommissionDto>> Handle(
        MarkCommissionPaidCommand request,
        CancellationToken cancellationToken)
    {
        var record = await commissionRecordRepository.FindByIdForUpdateAsync(
            request.CommissionRecordId,
            cancellationToken);

        if (record is null)
        {
            return Result<CommissionDto>.Error(CommissionErrorCodes.CommissionRecordNotFound);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var paidResult = record.MarkPaid(request.AdminUserId, nowUtc);
        if (paidResult.IsError)
        {
            return Result<CommissionDto>.Error(CommissionErrorCodes.InvalidCommissionStatus);
        }

        await auditLogRepository.AddAsync(
            AuditLog.Create(
                Guid.NewGuid(),
                request.AdminUserId,
                CommissionAuditActions.Paid,
                CommissionAuditActions.TargetType,
                record.Id,
                nowUtc),
            cancellationToken);

        await commissionRecordRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Commission record {CommissionRecordId} marked paid by admin {AdminUserId} for booking {BookingId}",
            record.Id,
            request.AdminUserId,
            record.BookingId);

        return Result<CommissionDto>.Success(CommissionMappings.ToDto(record));
    }
}
