using FluentValidation;
using Malayisha.Application.Common.Authorization;
using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;
using MediatR;

namespace Malayisha.Application.Features.Auth.DeleteAccount;

[AuthorizeRoles(UserRole.Sender, UserRole.Transporter)]
public sealed record DeleteAccountCommand(Guid UserId) : IRequest<Result>;

internal sealed class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
    }
}
