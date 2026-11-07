using Bank.Domain.Analytics;

namespace Bank.UnitTests.Analytics;

public sealed class StatisticsTests
{
    [Theory]
    [InlineData(new[] { 3.0, 1, 2 }, 2.0)]
    [InlineData(new[] { 4.0, 1, 3, 2 }, 2.5)]
    public void Median_OfOddAndEvenSets(double[] values, double expected)
    {
        Assert.Equal(expected, Statistics.Median(values));
    }

    [Fact]
    public void MedianAbsoluteDeviation_IgnoresAnExtremeValue()
    {
        // The mean-based spread would explode because of 1000; the MAD barely moves.
        Assert.Equal(1.0, Statistics.MedianAbsoluteDeviation([1, 2, 3, 4, 1000]));
    }

    [Fact]
    public void StandardDeviation_IsTheSampleStandardDeviation()
    {
        Assert.Equal(Math.Sqrt(2.5), Statistics.StandardDeviation([1, 2, 3, 4, 5]), precision: 10);
    }

    [Fact]
    public void Slope_OfAPerfectLine()
    {
        Assert.Equal(2.0, Statistics.Slope([10, 12, 14, 16]), precision: 10);
    }

    [Fact]
    public void Slope_OfASinglePoint_IsZero()
    {
        Assert.Equal(0, Statistics.Slope([5]));
    }
}
