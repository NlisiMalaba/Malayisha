using Malayisha.Domain.Common;
using ApplicationResult = Malayisha.Application.Common.Result<object>;

namespace Malayisha.Application.Common;

internal static class ResultResponseFactory
{
    public static TResponse ValidationFailed<TResponse>(string errorCode)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Invalid(errorCode);
        }

        if (TryGetGenericResultType(responseType, out var genericResultType))
        {
            var invalid = genericResultType.GetMethod(nameof(Result<object>.Invalid), [typeof(string)])!;
            return (TResponse)invalid.Invoke(null, [errorCode])!;
        }

        throw Unsupported(responseType);
    }

    public static TResponse Forbidden<TResponse>()
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Forbidden();
        }

        if (TryGetGenericResultType(responseType, out var genericResultType))
        {
            var forbidden = genericResultType.GetMethod(nameof(Result<object>.Forbidden), Type.EmptyTypes)!;
            return (TResponse)forbidden.Invoke(null, null)!;
        }

        throw Unsupported(responseType);
    }

    public static TResponse Unauthorized<TResponse>()
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Unauthorized();
        }

        if (TryGetGenericResultType(responseType, out var genericResultType))
        {
            var unauthorized = genericResultType.GetMethod(nameof(Result<object>.Unauthorized), Type.EmptyTypes)!;
            return (TResponse)unauthorized.Invoke(null, null)!;
        }

        throw Unsupported(responseType);
    }

    private static bool TryGetGenericResultType(Type responseType, out Type genericResultType)
    {
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            genericResultType = responseType;
            return true;
        }

        genericResultType = null!;
        return false;
    }

    private static InvalidOperationException Unsupported(Type responseType) =>
        new($"Response type {responseType.Name} does not support result-based pipeline errors.");
}
