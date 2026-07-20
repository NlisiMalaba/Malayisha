using Malayisha.Application.Validation;
using MediatR;

namespace Malayisha.Application.Behaviors;

internal sealed class SanitizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        InputSanitizer.SanitizeInstance(request);
        return await next(cancellationToken);
    }
}
