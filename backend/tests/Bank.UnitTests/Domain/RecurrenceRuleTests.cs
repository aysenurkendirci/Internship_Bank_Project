using Bank.Domain.Common;
using Bank.Domain.Scheduling;

namespace Bank.UnitTests.Domain;

public sealed class RecurrenceRuleTests
{
    private static DateOnly D(int year, int month, int day) => new(year, month, day);

    [Fact]
    public void Monthly_OnThe31st_ClampsShortMonthsWithoutDrifting()
    {
        var rule = RecurrenceRule.Monthly(D(2026, 1, 31));

        var dates = rule.OccurrencesBetween(D(2026, 1, 1), D(2026, 5, 31));

        Assert.Equal([D(2026, 1, 31), D(2026, 2, 28), D(2026, 3, 31), D(2026, 4, 30), D(2026, 5, 31)], dates);
    }

    [Fact]
    public void Yearly_OnLeapDay_FallsBackToFeb28AndReturnsInLeapYears()
    {
        var rule = new RecurrenceRule(Frequency.Yearly, D(2024, 2, 29));

        var dates = rule.OccurrencesBetween(D(2024, 1, 1), D(2028, 12, 31));

        Assert.Equal([D(2024, 2, 29), D(2025, 2, 28), D(2026, 2, 28), D(2027, 2, 28), D(2028, 2, 29)], dates);
    }

    [Fact]
    public void Weekly_WithInterval_SkipsWeeks()
    {
        var rule = new RecurrenceRule(Frequency.Weekly, D(2026, 3, 2), interval: 2);

        var dates = rule.OccurrencesBetween(D(2026, 3, 1), D(2026, 3, 31));

        Assert.Equal([D(2026, 3, 2), D(2026, 3, 16), D(2026, 3, 30)], dates);
    }

    [Fact]
    public void Occurrences_BeforeRangeStart_AreExcluded()
    {
        var rule = RecurrenceRule.Monthly(D(2025, 6, 15));

        var dates = rule.OccurrencesBetween(D(2026, 1, 1), D(2026, 3, 31));

        Assert.Equal([D(2026, 1, 15), D(2026, 2, 15), D(2026, 3, 15)], dates);
    }

    [Fact]
    public void Occurrences_StopAtEndDate()
    {
        var rule = RecurrenceRule.Monthly(D(2026, 1, 10), endDate: D(2026, 3, 10));

        var dates = rule.OccurrencesBetween(D(2026, 1, 1), D(2026, 12, 31));

        Assert.Equal([D(2026, 1, 10), D(2026, 2, 10), D(2026, 3, 10)], dates);
    }

    [Fact]
    public void Occurrences_RangeBoundariesAreInclusive()
    {
        var rule = RecurrenceRule.Monthly(D(2026, 1, 10));

        var dates = rule.OccurrencesBetween(D(2026, 2, 10), D(2026, 3, 10));

        Assert.Equal([D(2026, 2, 10), D(2026, 3, 10)], dates);
    }

    [Fact]
    public void Occurrences_RuleStartingAfterRange_IsEmpty()
    {
        var rule = RecurrenceRule.Monthly(D(2027, 1, 1));

        Assert.Empty(rule.OccurrencesBetween(D(2026, 1, 1), D(2026, 12, 31)));
    }

    [Fact]
    public void Constructor_EndBeforeStart_IsRejected()
    {
        Assert.Throws<DomainException>(() => RecurrenceRule.Monthly(D(2026, 5, 1), endDate: D(2026, 4, 30)));
    }

    [Fact]
    public void Constructor_ZeroInterval_IsRejected()
    {
        Assert.Throws<DomainException>(() => new RecurrenceRule(Frequency.Monthly, D(2026, 1, 1), interval: 0));
    }
}
