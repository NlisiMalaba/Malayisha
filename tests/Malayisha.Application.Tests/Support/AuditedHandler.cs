using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Application.Tests.Support;

internal sealed class AuditedHandler<TRequest, TResponse>(
    IRequestHandler<TRequest, TResponse> inner,
    IAuditLogRepository auditLogRepository,
    TimeProvider timeProvider)
    where TRequest : IRequest<TResponse>
{
    private readonly AuditBehavior<TRequest, TResponse> _auditBehavior = new(
        auditLogRepository,
        timeProvider,
        NullLogger<AuditBehavior<TRequest, TResponse>>.Instance);

    public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default) =>
        _auditBehavior.Handle(
            request,
            ct => inner.Handle(request, ct),
            cancellationToken);
}
