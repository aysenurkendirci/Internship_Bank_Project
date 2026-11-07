using Bank.Domain.Common;

namespace Bank.Domain.Scheduling;

/// <summary>
/// Describes when a repeating payment happens: "every month on the 31st from January", "every year", ...
/// </summary>
/// <remarks>
/// Each occurrence is computed from <see cref="StartDate"/>, never from the previous occurrence.
/// A rule anchored on 31 January therefore yields 28 February and then 31 March; stepping from
/// the previous date would drift to the 28th for the rest of the year.
/// </remarks>
public sealed record RecurrenceRule
{
    public Frequency Frequency { get; }
    public int Interval { get; }
    public DateOnly StartDate { get; }
    public DateOnly? EndDate { get; }

    public RecurrenceRule(Frequency frequency, DateOnly startDate, int interval = 1, DateOnly? endDate = null)
    {
        if (!Enum.IsDefined(frequency))
            throw new DomainException($"Geçersiz tekrar sıklığı: {frequency}.");
        if (interval < 1)
            throw new DomainException("Tekrar aralığı en az 1 olmalıdır.");
        if (endDate < startDate)
            throw new DomainException("Bitiş tarihi başlangıç tarihinden önce olamaz.");

        Frequency = frequency;
        Interval = interval;
        StartDate = startDate;
        EndDate = endDate;
    }

    public static RecurrenceRule Monthly(DateOnly startDate, DateOnly? endDate = null)
        => new(Frequency.Monthly, startDate, endDate: endDate);

    /// <summary>Occurrences within the inclusive range [<paramref name="from"/>, <paramref name="to"/>].</summary>
    public IEnumerable<DateOnly> OccurrencesBetween(DateOnly from, DateOnly to)
    {
        if (to < from)
            throw new DomainException("Aralığın sonu başından önce olamaz.");

        var last = EndDate is { } end && end < to ? end : to;

        for (var n = 0; ; n++)
        {
            var occurrence = NthOccurrence(n);
            if (occurrence > last)
                yield break;
            if (occurrence >= from)
                yield return occurrence;
        }
    }

    private DateOnly NthOccurrence(int n) => Frequency switch
    {
        Frequency.Weekly => StartDate.AddDays(7 * Interval * n),
        Frequency.Monthly => StartDate.AddMonths(Interval * n),
        Frequency.Yearly => StartDate.AddYears(Interval * n),
        _ => throw new InvalidOperationException($"Unhandled frequency {Frequency}.")
    };
}
