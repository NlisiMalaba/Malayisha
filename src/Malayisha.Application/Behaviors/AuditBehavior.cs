using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common.Authorization;
using Malayisha.Domain.Common;
using Malayisha.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Behaviors;

internal sealed class AuditBehavior<TRequest, TResponse>(
    IAuditLogRepository auditLogRepository,
    TimeProvider timeProvider,
    ILogger<AuditBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (request is not IAuditableAdminCommand auditableCommand)
        {
            return response;
        }

        if (response is not IResultResponse resultResponse || !resultResponse.IsSuccess)
        {
            return response;
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        await auditLogRepository.AddAsync(
            AuditLog.Create(
                Guid.NewGuid(),
                auditableCommand.AdminUserId,
                auditableCommand.AuditAction,
                auditableCommand.TargetType,
                auditableCommand.TargetId,
                nowUtc,
                auditableCommand.MetadataJson),
            cancellationToken);

        await auditLogRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Audit log {AuditAction} recorded for {TargetType} {TargetId} by admin {AdminUserId}",
            auditableCommand.AuditAction,
            auditableCommand.TargetType,
            auditableCommand.TargetId,
            auditableCommand.AdminUserId);

        return response;
    }
}
