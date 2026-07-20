using FsCheck.Xunit;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Commission;
using Malayisha.Application.Features.Commission.GetCommissionReport;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Application.Tests;

public sealed class CommissionPropertyTests
{
    private static readonly DateTime BaselineUtc = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid AdminUserId = BuildUserId(unchecked((int)0xABCDEF01));

    [Property(MaxTest = 100)]
    public bool Property31_CommissionRecordCorrectness(
        int amountSeed,
        int bookingSeed,
        int transporterSeed,
        int completionSeed)
    {
        var wholePart = (Math.Abs(amountSeed) % 50_000) + 1;
        var fractionalPart = Math.Abs(amountSeed) % 100;
        var agreedPrice = wholePart + fractionalPart / 100m;

        var recordId = BuildGuid(bookingSeed ^ transporterSeed);
        var bookingId = BuildGuid(bookingSeed);
        var transporterUserId = BuildUserId(transporterSeed);
        var completionDate = BaselineUtc.AddHours(Math.Abs(completionSeed) % 10_000);

        var record = CommissionRecord.Create(
            recordId,
            bookingId,
            transporterUserId,
            agreedPrice,
            CommissionConstants.StandardCommissionRate,
            completionDate);

        var expectedAmount = decimal.Round(
            agreedPrice * CommissionConstants.StandardCommissionRate,
            2,
            MidpointRounding.AwayFromZero);

        return record.Id == recordId
               && record.BookingId == bookingId
               && record.TransporterUserId == transporterUserId
               && record.AgreedPriceZar == agreedPrice
               && record.CommissionRate == CommissionConstants.StandardCommissionRate
               && record.CommissionAmountZar == expectedAmount
               && record.Status == CommissionStatus.Pending
               && record.CompletionDateUtc == completionDate
               && record.UpdatedByAdminUserId is null
               && record.UpdatedAtUtc is null;
    }

    [Property(MaxTest = 100)]
    public bool Property32_CommissionFilterCorrectness(
        int recordCountSeed,
        int statusSeed,
        int dateSeed)
    {
        return RunCommissionFilterCorrectnessAsync(recordCountSeed, statusSeed, dateSeed)
            .GetAwaiter()
            .GetResult();
    }

    private static async Task<bool> RunCommissionFilterCorrectnessAsync(
        int recordCountSeed,
        int statusSeed,
        int dateSeed)
    {
        var harness = CommissionTestHarness.Create();
        var recordCount = (Math.Abs(recordCountSeed) % 6) + 3;
        var seededRecords = new List<SeededCommissionRecord>(recordCount);

        for (var index = 0; index < recordCount; index++)
        {
            var targetStatus = (CommissionStatus)((Math.Abs(statusSeed + index) % 3) + 1);
            var completionDate = BaselineUtc.AddDays(Math.Abs(dateSeed + index * 3) % 40);
            var agreedPrice = (Math.Abs(dateSeed * 11 + index) % 5000) + 100m;

            var record = CommissionRecord.Create(
                BuildGuid(dateSeed + index + 1),
                BuildGuid(dateSeed + index + 10_001),
                BuildUserId(transporterSeed: index + 100),
                agreedPrice,
                CommissionConstants.StandardCommissionRate,
                completionDate);

            ApplyStatus(record, targetStatus, completionDate);

            await harness.Repository.AddAsync(record);
            seededRecords.Add(new SeededCommissionRecord(record, completionDate));
        }

        var unfiltered = await harness.ReportHandler.Handle(
            new GetCommissionReportQuery(null, null, null),
            CancellationToken.None);

        if (unfiltered.IsError
            || unfiltered.Value is null
            || unfiltered.Value.Count != recordCount)
        {
            return false;
        }

        foreach (var status in Enum.GetValues<CommissionStatus>())
        {
            var expectedIds = seededRecords
                .Where(item => item.Record.Status == status)
                .Select(item => item.Record.Id)
                .ToHashSet();

            var filtered = await harness.ReportHandler.Handle(
                new GetCommissionReportQuery(status, null, null),
                CancellationToken.None);

            if (filtered.IsError
                || filtered.Value is null
                || filtered.Value.Count != expectedIds.Count
                || filtered.Value.Any(dto => dto.Status != status)
                || filtered.Value.Any(dto => !expectedIds.Contains(dto.Id)))
            {
                return false;
            }
        }

        var minDate = seededRecords.Min(item => item.CompletionDateUtc);
        var maxDate = seededRecords.Max(item => item.CompletionDateUtc);
        var midpoint = minDate.AddTicks((maxDate - minDate).Ticks / 2);

        var expectedInRange = seededRecords
            .Where(item => item.CompletionDateUtc >= minDate && item.CompletionDateUtc <= midpoint)
            .Select(item => item.Record.Id)
            .ToHashSet();

        var dateFiltered = await harness.ReportHandler.Handle(
            new GetCommissionReportQuery(null, minDate, midpoint),
            CancellationToken.None);

        if (dateFiltered.IsError
            || dateFiltered.Value is null
            || dateFiltered.Value.Count != expectedInRange.Count
            || dateFiltered.Value.Any(dto => dto.CompletionDateUtc < minDate || dto.CompletionDateUtc > midpoint)
            || dateFiltered.Value.Any(dto => !expectedInRange.Contains(dto.Id)))
        {
            return false;
        }

        var combinedStatus = CommissionStatus.Pending;
        var expectedCombined = seededRecords
            .Where(item =>
                item.Record.Status == combinedStatus
                && item.CompletionDateUtc >= minDate
                && item.CompletionDateUtc <= midpoint)
            .Select(item => item.Record.Id)
            .ToHashSet();

        var combinedFiltered = await harness.ReportHandler.Handle(
            new GetCommissionReportQuery(combinedStatus, minDate, midpoint),
            CancellationToken.None);

        if (combinedFiltered.IsError
            || combinedFiltered.Value is null
            || combinedFiltered.Value.Count != expectedCombined.Count
            || combinedFiltered.Value.Any(dto =>
                dto.Status != combinedStatus
                || dto.CompletionDateUtc < minDate
                || dto.CompletionDateUtc > midpoint)
            || combinedFiltered.Value.Any(dto => !expectedCombined.Contains(dto.Id)))
        {
            return false;
        }

        for (var index = 1; index < dateFiltered.Value.Count; index++)
        {
            if (dateFiltered.Value[index].CompletionDateUtc > dateFiltered.Value[index - 1].CompletionDateUtc)
            {
                return false;
            }
        }

        return true;
    }

    private static void ApplyStatus(
        CommissionRecord record,
        CommissionStatus targetStatus,
        DateTime completionDate)
    {
        switch (targetStatus)
        {
            case CommissionStatus.Pending:
                return;
            case CommissionStatus.Invoiced:
                _ = record.MarkInvoiced(AdminUserId, completionDate.AddHours(1));
                return;
            case CommissionStatus.Paid:
                _ = record.MarkInvoiced(AdminUserId, completionDate.AddHours(1));
                _ = record.MarkPaid(AdminUserId, completionDate.AddHours(2));
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(targetStatus), targetStatus, null);
        }
    }

    private static Guid BuildGuid(int seed)
    {
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, 4), seed);
        BitConverter.TryWriteBytes(bytes.AsSpan(4, 4), seed * 7);
        BitConverter.TryWriteBytes(bytes.AsSpan(8, 4), seed ^ 0x01020304);
        BitConverter.TryWriteBytes(bytes.AsSpan(12, 4), ~seed);
        return new Guid(bytes);
    }

    private static Guid BuildUserId(int transporterSeed)
    {
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, 4), transporterSeed);
        BitConverter.TryWriteBytes(bytes.AsSpan(4, 4), transporterSeed ^ 0x5A5A5A5A);
        BitConverter.TryWriteBytes(bytes.AsSpan(8, 4), transporterSeed * 31);
        BitConverter.TryWriteBytes(bytes.AsSpan(12, 4), ~transporterSeed);
        return new Guid(bytes);
    }

    private readonly record struct SeededCommissionRecord(CommissionRecord Record, DateTime CompletionDateUtc);

    private sealed class CommissionTestHarness
    {
        private CommissionTestHarness(
            InMemoryCommissionRecordRepository repository,
            GetCommissionReportQueryHandler reportHandler)
        {
            Repository = repository;
            ReportHandler = reportHandler;
        }

        public InMemoryCommissionRecordRepository Repository { get; }
        public GetCommissionReportQueryHandler ReportHandler { get; }

        public static CommissionTestHarness Create()
        {
            var repository = new InMemoryCommissionRecordRepository();
            return new CommissionTestHarness(
                repository,
                new GetCommissionReportQueryHandler(
                    repository,
                    NullLogger<GetCommissionReportQueryHandler>.Instance));
        }
    }

    private sealed class InMemoryCommissionRecordRepository : ICommissionRecordRepository
    {
        private readonly List<CommissionRecord> _items = [];

        public Task<bool> ExistsForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Any(item => item.BookingId == bookingId));

        public Task<CommissionRecord?> FindByIdForUpdateAsync(
            Guid commissionRecordId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(item => item.Id == commissionRecordId));

        public Task<IReadOnlyList<CommissionRecord>> ListByCriteriaAsync(
            CommissionReportCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            var query = _items.AsEnumerable();

            if (criteria.Status.HasValue)
            {
                query = query.Where(item => item.Status == criteria.Status.Value);
            }

            if (criteria.FromCompletionDateUtc.HasValue)
            {
                query = query.Where(item => item.CompletionDateUtc >= criteria.FromCompletionDateUtc.Value);
            }

            if (criteria.ToCompletionDateUtc.HasValue)
            {
                query = query.Where(item => item.CompletionDateUtc <= criteria.ToCompletionDateUtc.Value);
            }

            var results = query
                .OrderByDescending(item => item.CompletionDateUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyList<CommissionRecord>>(results);
        }

        public Task AddAsync(CommissionRecord commissionRecord, CancellationToken cancellationToken = default)
        {
            _items.Add(commissionRecord);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
