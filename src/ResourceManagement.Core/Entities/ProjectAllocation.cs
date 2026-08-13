using System.ComponentModel.DataAnnotations;

namespace ResourceManagement.Core.Entities;

/// <summary>Allocation of a resource to a project/demand with hour budget.</summary>
public class ProjectAllocation
{
    public int Id { get; set; }
    public int ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public DateTime AllocationStartDate { get; set; }
    public DateTime AllocationEndDate { get; set; }

    /// <summary>Hours per week allocated for this resource on this project.</summary>
    public decimal WeeklyHours { get; set; }

    /// <summary>Total budgeted hours for this resource on this project.</summary>
    public decimal BudgetedHours { get; set; }

    /// <summary>Actual hours consumed by ILC claims.</summary>
    public decimal ConsumedHours { get; set; }

    public decimal RemainingHours => BudgetedHours - ConsumedHours;

    public bool IsOverBudget => ConsumedHours > BudgetedHours;

    public decimal FteFraction { get; set; } = 1;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public AllocationStatus Status { get; set; } = AllocationStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}

public enum AllocationStatus
{
    Active = 1,
    Completed = 2,
    Cancelled = 3
}
