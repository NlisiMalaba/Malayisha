using FluentValidation;
using Malayisha.Api.Contracts.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Malayisha.Api.Filters;

public sealed class ValidationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ValidationException validationException)
        {
            return;
        }

        var errors = validationException.Errors
            .Select(error => new
            {
                error.PropertyName,
                ErrorCode = string.IsNullOrWhiteSpace(error.ErrorCode) ? "ValidationFailed" : error.ErrorCode,
                error.ErrorMessage
            })
            .ToArray();

        context.Result = new BadRequestObjectResult(new
        {
            ErrorCode = "ValidationFailed",
            Errors = errors
        });

        context.ExceptionHandled = true;
    }
}
