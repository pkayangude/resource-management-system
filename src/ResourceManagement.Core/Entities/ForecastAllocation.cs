using System.ComponentModel.DataAnnotations;

namespace ResourceManagement.Core.Entities;

/// <summary>Monthly forecast allocation for a resource.</summary>
public class ForecastAllocation
{
    public int Id { get; set; }
    public int ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;

    public int Year { get; set; }
    public int Month { get; set; }  // 1-12

    /// <summary>Forecasted hours for the month (derived from working days * 9h/day).</summary>
    public decimal ForecastHours { get; set; }

    /// <summary>Forecasted cost = ForecastHours * CostRate.</summary>
    public decimal ForecastCost { get; set; }

    /// <summary>FTE fraction (1 = full, 0.5 = half, etc.).</summary>
    public decimal FteFraction { get; set; } = 1;

    /// <summary>Actual claimed hours from ILC for the month.</summary>
    public decimal? ActualHours { get; set; }

    /// <summary>Variance between forecast and actual.</summary>
    public decimal? VarianceHours => ActualHours.HasValue ? ActualHours - ForecastHours : null;

    [MaxLength(500)]
    public string? Comments { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
