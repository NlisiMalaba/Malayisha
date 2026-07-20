using Malayisha.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Behaviors;

internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Handling {RequestName}", requestName);

        var response = await next(cancellationToken);

        if (response is IResultResponse resultResponse)
        {
            if (resultResponse.IsSuccess)
            {
                logger.LogInformation("Handled {RequestName} successfully", requestName);
            }
            else
            {
                logger.LogWarning(
                    "Handled {RequestName} with error {ErrorCode}",
                    requestName,
                    resultResponse.ErrorCode);
            }
        }
        else
        {
            logger.LogInformation("Handled {RequestName}", requestName);
        }

        return response;
    }
}
