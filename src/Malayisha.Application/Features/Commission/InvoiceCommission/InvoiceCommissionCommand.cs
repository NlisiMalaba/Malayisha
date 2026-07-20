using FluentValidation;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Application.Common.Authorization;
using Malayisha.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Commission.InvoiceCommission;

[AuthorizeRoles(UserRole.Admin)]
public sealed record InvoiceCommissionCommand(Guid CommissionRecordId, Guid AdminUserId)
    : IRequest<Result<CommissionDto>>, IAuditableAdminCommand
{
    public Guid TargetId => CommissionRecordId;

    public string AuditAction => CommissionAuditActions.Invoiced;

    public string TargetType => CommissionAuditActions.TargetType;
}

internal sealed class InvoiceCommissionCommandValidator : AbstractValidator<InvoiceCommissionCommand>
{
    public InvoiceCommissionCommandValidator()
    {
        RuleFor(command => command.CommissionRecordId).NotEmpty();
        RuleFor(command => command.AdminUserId).NotEmpty();
    }
}

internal sealed class InvoiceCommissionCommandHandler(
    ICommissionRecordRepository commissionRecordRepository,
    TimeProvider timeProvider,
    ILogger<InvoiceCommissionCommandHandler> logger)
    : IRequestHandler<InvoiceCommissionCommand, Result<CommissionDto>>
{
    public async Task<Result<CommissionDto>> Handle(
        InvoiceCommissionCommand request,
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
        var invoiceResult = record.MarkInvoiced(request.AdminUserId, nowUtc);
        if (invoiceResult.IsError)
        {
            return Result<CommissionDto>.Error(CommissionErrorCodes.InvalidCommissionStatus);
        }

        await commissionRecordRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Commission record {CommissionRecordId} invoiced by admin {AdminUserId} for booking {BookingId}",
            record.Id,
            request.AdminUserId,
            record.BookingId);

        return Result<CommissionDto>.Success(CommissionMappings.ToDto(record));
    }
}
