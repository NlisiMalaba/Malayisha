namespace Malayisha.Infrastructure.Options;

public sealed class BookingWorkflowOptions
{
    public const string SectionName = "BookingWorkflow";

    public int AutoCompleteAfterHours { get; set; } = 48;

    public decimal CommissionRate { get; set; } = 0.10m;
}
