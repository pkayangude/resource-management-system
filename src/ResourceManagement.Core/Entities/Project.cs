using System.ComponentModel.DataAnnotations;

namespace ResourceManagement.Core.Entities;

/// <summary>Project or Major Demand entity.</summary>
public class Project
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string ProjectCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string ProjectName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ProjectDbId { get; set; }

    public ProjectType ProjectType { get; set; } = ProjectType.Standard;

    [MaxLength(200)]
    public string? ClaimCode { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>Total budgeted hours for the project.</summary>
    public decimal TotalBudgetHours { get; set; }

    /// <summary>Total consumed hours (from ILC).</summary>
    public decimal ConsumedHours { get; set; }

    /// <summary>Remaining hours = TotalBudgetHours - ConsumedHours.</summary>
    public decimal RemainingHours => TotalBudgetHours - ConsumedHours;

    public bool IsOverBudget => ConsumedHours > TotalBudgetHours;

    [MaxLength(200)]
    public string? Portfolio { get; set; }

    [MaxLength(200)]
    public string? PemName { get; set; }

    [MaxLength(200)]
    public string? RmIntranetId { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ProjectAllocation> Allocations { get; set; } = new List<ProjectAllocation>();
}

public enum ProjectType
{
    Standard = 1,
    MajorDemand = 2,
    Support = 3,
    Internal = 4
}

public enum ProjectStatus
{
    Active = 1,
    Completed = 2,
    OnHold = 3,
    Cancelled = 4
}
