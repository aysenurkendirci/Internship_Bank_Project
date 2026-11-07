using Bank.Application.Common;
using Microsoft.Extensions.Time.Testing;

namespace Bank.UnitTests.Application;

public sealed class BusinessDateTests
{
    [Fact]
    public void JustAfterMidnightInIstanbul_IsAlreadyTheNextDay()
    {
        // 21:30 UTC on 31 March is 00:30 on 1 April in Istanbul (UTC+3).
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 3, 31, 21, 30, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 4, 1), BusinessDate.Today(time));
    }

    [Fact]
    public void JustBeforeMidnightInIstanbul_IsStillTheSameDay()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 3, 31, 20, 59, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 3, 31), BusinessDate.Today(time));
    }
}
