using FluentValidation;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Application.Common.Authorization;
using Malayisha.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Commission.GetCommissionReport;

[AuthorizeRoles(UserRole.Admin)]
public sealed record GetCommissionReportQuery(
    CommissionStatus? Status,
    DateTime? FromCompletionDateUtc,
    DateTime? ToCompletionDateUtc) : IRequest<Result<IReadOnlyList<CommissionDto>>>;

internal sealed class GetCommissionReportQueryValidator : AbstractValidator<GetCommissionReportQuery>
{
    public GetCommissionReportQueryValidator()
    {
        RuleFor(query => query)
            .Must(query =>
                !query.FromCompletionDateUtc.HasValue
                || !query.ToCompletionDateUtc.HasValue
                || query.FromCompletionDateUtc.Value <= query.ToCompletionDateUtc.Value)
            .WithErrorCode(CommissionErrorCodes.InvalidDateRange);
    }
}

internal sealed class GetCommissionReportQueryHandler(
    ICommissionRecordRepository commissionRecordRepository,
    ILogger<GetCommissionReportQueryHandler> logger)
    : IRequestHandler<GetCommissionReportQuery, Result<IReadOnlyList<CommissionDto>>>
{
    public async Task<Result<IReadOnlyList<CommissionDto>>> Handle(
        GetCommissionReportQuery request,
        CancellationToken cancellationToken)
    {
        var records = await commissionRecordRepository.ListByCriteriaAsync(
            new CommissionReportCriteria(
                request.Status,
                request.FromCompletionDateUtc,
                request.ToCompletionDateUtc),
            cancellationToken);

        var dtos = records
            .Select(CommissionMappings.ToDto)
            .ToList();

        logger.LogInformation(
            "Retrieved {CommissionCount} commission records for report (status={Status}, from={From}, to={To})",
            dtos.Count,
            request.Status,
            request.FromCompletionDateUtc,
            request.ToCompletionDateUtc);

        return Result<IReadOnlyList<CommissionDto>>.Success(dtos);
    }
}
