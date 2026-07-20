using Malayisha.Domain.Enums;

namespace Malayisha.Application.Abstractions.Persistence;

public sealed record CommissionReportCriteria(
    CommissionStatus? Status,
    DateTime? FromCompletionDateUtc,
    DateTime? ToCompletionDateUtc);
