using FluentValidation;

namespace Malayisha.Application.Features.DeliveryRequest.ListDeliveryRequests;

internal sealed class ListDeliveryRequestsQueryValidator : AbstractValidator<ListDeliveryRequestsQuery>
{
    public ListDeliveryRequestsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, DeliveryRequestValidation.MaxPageSize);
    }
}
