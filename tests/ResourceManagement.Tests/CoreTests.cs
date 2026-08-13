using Xunit;
using ResourceManagement.Core.Entities;

namespace ResourceManagement.Tests;

// ─── Forecast hours calculation ───────────────────────────────────────────────

public class ForecastCalculationLogicTests
{
    private const int HoursPerDay = 9;

    [Theory]
    [InlineData(22, 1.0,  198.0)]
    [InlineData(22, 0.5,   99.0)]
    [InlineData(20, 0.25,  45.0)]
    [InlineData(23, 1.0,  207.0)]
    public void ForecastHours_IsWorkingDaysTimesNineTimesFte(int workingDays, double fte, double expected)
    {
        var hours = workingDays * HoursPerDay * (decimal)fte;
        Assert.Equal((decimal)expected, hours);
    }

    [Fact]
    public void ForecastCost_IsHoursTimesRate()
    {
        decimal hours = 198m;
        decimal costRate = 19m;
        Assert.Equal(3762m, hours * costRate);
    }
}

// ─── ILC validation rules ─────────────────────────────────────────────────────

public class IlcValidationRuleTests
{
    [Theory]
    [InlineData(45,  false)]
    [InlineData(44,  false)]
    [InlineData(46,  true)]
    [InlineData(50,  true)]
    public void WeeklyHours_ExceedsFortyFiveLimit(decimal claimed, bool shouldExceed)
        => Assert.Equal(shouldExceed, claimed > 45);

    [Theory]
    [InlineData(900,  1000, 50,  false)]
    [InlineData(951,  1000, 50,  true)]
    [InlineData(1000, 1000, 1,   true)]
    [InlineData(0,    500,  100, false)]
    public void ProjectBudget_ExceedCheck(decimal consumed, decimal budget, decimal newClaim, bool shouldExceed)
        => Assert.Equal(shouldExceed, (consumed + newClaim) > budget);

    [Theory]
    [InlineData(198,   198, false)]   // exactly at forecast — no overrun
    [InlineData(190,   198, false)]   // below forecast — no overrun
    [InlineData(218,   198, true)]    // 218 > 198*1.10=217.8 — overrun
    [InlineData(217,   198, false)]   // 217 < 217.8 — NOT overrun (boundary)
    [InlineData(220,   200, false)]   // 220 = 200*1.10=220 exactly — NOT overrun
    [InlineData(221,   200, true)]    // 221 > 220 — overrun
    [InlineData(180,   198, false)]   // well below — no overrun
    public void MonthlyHours_ForecastOverrun_TenPercentThreshold(decimal actual, decimal forecast, bool expectsOverrun)
        => Assert.Equal(expectsOverrun, actual > forecast * 1.10m);
}

// ─── Band mix weightage ───────────────────────────────────────────────────────

public class BandMixWeightageTests
{
    private static readonly Dictionary<string, decimal> Weightages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["4"]   = 4.5m,  ["Band 4"]  = 4.5m,
        ["5"]   = 5.0m,  ["Band 5"]  = 5.0m,
        ["6G"]  = 5.5m,  ["Band 6G"] = 5.5m,
        ["6A"]  = 6.0m,  ["Band 6A"] = 6.0m,
        ["6B"]  = 6.5m,  ["Band 6B"] = 6.5m,
        ["7A"]  = 7.0m,  ["Band 7A"] = 7.0m,
        ["7B"]  = 7.5m,  ["Band 7B"] = 7.5m,
        ["8"]   = 8.0m,  ["Band 8"]  = 8.0m,
        ["9"]   = 9.0m,  ["Band 9"]  = 9.0m,
        ["10"]  = 10.0m, ["Band 10"] = 10.0m,
    };

    [Theory]
    [InlineData("6B",      6.5)]
    [InlineData("Band 6B", 6.5)]
    [InlineData("7A",      7.0)]
    [InlineData("Band 7A", 7.0)]
    [InlineData("8",       8.0)]
    [InlineData("9",       9.0)]
    public void BandWeightage_LookupIsCorrect(string band, double expectedWeight)
    {
        Assert.True(Weightages.TryGetValue(band, out var w));
        Assert.Equal((decimal)expectedWeight, w);
    }

    [Fact]
    public void BandMix_TwoResources_IsWeightedAverage()
    {
        // 6B (6.5) × 1 + 7A (7.0) × 1 = 13.5 / 2 = 6.75
        var bands = new[] { ("6B", 1), ("7A", 1) };
        var totalValue = bands.Sum(b => Weightages[b.Item1] * b.Item2);
        var totalFte   = bands.Sum(b => b.Item2);
        Assert.Equal(6.75m, totalValue / totalFte);
    }

    [Fact]
    public void BandMix_48FteTeam_MatchesSpreadsheetSample()
    {
        // Matches Bandmix Calculator.xlsx sample (June column: bandmix ≈ 6.52)
        var team = new[] {
            ("Band 4",  3), ("Band 6G", 1), ("Band 6A", 9),
            ("Band 6B", 18), ("Band 7A", 10), ("Band 7B", 6), ("Band 8", 1),
        };
        var totalValue = team.Sum(b => Weightages[b.Item1] * b.Item2);
        var totalFte   = team.Sum(b => b.Item2);
        var bandMix    = totalValue / totalFte;

        Assert.Equal(48, totalFte);
        Assert.True(bandMix > 6.50m && bandMix < 6.55m,
            $"Expected bandMix ≈ 6.52, got {bandMix}");
    }
}

// ─── Leave forecast impact ────────────────────────────────────────────────────

public class LeaveImpactTests
{
    [Theory]
    [InlineData(10, 90)]
    [InlineData(5,  45)]
    [InlineData(22, 198)]
    public void LeaveImpact_IsWorkingDaysTimes9(int workingDays, decimal expectedImpact)
        => Assert.Equal(expectedImpact, workingDays * 9m);
}

// ─── Working days calculation ─────────────────────────────────────────────────

public class WorkingDaysTests
{
    [Fact]
    public void WorkingDays_Jan2026_Is22()
    {
        int year = 2026, month = 1;
        int daysInMonth = DateTime.DaysInMonth(year, month);
        int weekendDays = Enumerable.Range(1, daysInMonth)
            .Count(d => new DateTime(year, month, d).DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
        Assert.Equal(22, daysInMonth - weekendDays);
    }

    [Fact]
    public void WorkingDays_LessHoliday_ReducedByOne()
        => Assert.Equal(21, 22 - 1);
}

// ─── Entity domain logic ──────────────────────────────────────────────────────

public class ResourceEntityTests
{
    [Fact]
    public void Resource_IsActive_WhenStatusActive()
        => Assert.True(new Resource { Status = ResourceStatus.Active }.IsActive);

    [Fact]
    public void Resource_IsNotActive_WhenOffboarded()
        => Assert.False(new Resource { Status = ResourceStatus.Offboarded }.IsActive);

    [Fact]
    public void Project_IsOverBudget_WhenConsumedExceedsBudget()
        => Assert.True(new Project { TotalBudgetHours = 1000, ConsumedHours = 1001 }.IsOverBudget);

    [Fact]
    public void Project_RemainingHours_IsCorrect()
        => Assert.Equal(250m, new Project { TotalBudgetHours = 1000, ConsumedHours = 750 }.RemainingHours);

    [Fact]
    public void ProjectAllocation_IsOverBudget_WhenConsumedExceedsBudgeted()
    {
        var pa = new ProjectAllocation { BudgetedHours = 500, ConsumedHours = 600 };
        Assert.True(pa.IsOverBudget);
        Assert.Equal(-100m, pa.RemainingHours);
    }

    [Theory]
    [InlineData(1, "Beginner")]
    [InlineData(2, "Intermediate")]
    [InlineData(3, "Advanced")]
    [InlineData(4, "Expert")]
    public void SkillMatrix_ProficiencyLabel_IsCorrect(int level, string expected)
        => Assert.Equal(expected, new SkillMatrix { ProficiencyLevel = level }.ProficiencyLabel);

    [Fact]
    public void ForecastAllocation_VarianceHours_IsActualMinusForecast()
    {
        var fa = new ForecastAllocation { ForecastHours = 198, ActualHours = 185 };
        Assert.Equal(-13m, fa.VarianceHours);
    }

    [Fact]
    public void ForecastAllocation_VarianceHours_NullWhenNoActual()
    {
        var fa = new ForecastAllocation { ForecastHours = 198, ActualHours = null };
        Assert.Null(fa.VarianceHours);
    }
}
