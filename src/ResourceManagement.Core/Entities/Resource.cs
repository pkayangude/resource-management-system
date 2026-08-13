using System.ComponentModel.DataAnnotations;

namespace ResourceManagement.Core.Entities;

/// <summary>Resource entity representing an employee or contractor.</summary>
public class Resource
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string EmpId { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string TalentId { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Corporate { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Pcode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Location { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Band { get; set; } = string.Empty;

    public decimal CostRate { get; set; }

    [MaxLength(200)]
    public string? Manager { get; set; }

    [MaxLength(200)]
    public string? Team { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? IppfCategory { get; set; }

    [MaxLength(200)]
    public string? JobRoleSkillSet { get; set; }

    [MaxLength(50)]
    public string? EmployeeType { get; set; }  // RF = Regular, CT = Contractor

    [MaxLength(200)]
    public string? IntranetId { get; set; }

    [MaxLength(200)]
    public string? NotesId { get; set; }

    [MaxLength(200)]
    public string? PemTalentId { get; set; }

    [MaxLength(100)]
    public string? DeptCode { get; set; }

    [MaxLength(200)]
    public string? JrssServiceArea { get; set; }

    public DateTime? DateOfJoining { get; set; }
    public DateTime? OnboardingDate { get; set; }
    public DateTime? OffboardingDate { get; set; }

    public ResourceStatus Status { get; set; } = ResourceStatus.Active;

    public bool IsActive => Status == ResourceStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation
    public ICollection<ForecastAllocation> ForecastAllocations { get; set; } = new List<ForecastAllocation>();
    public ICollection<IlcClaim> IlcClaims { get; set; } = new List<IlcClaim>();
    public ICollection<LeaveRecord> LeaveRecords { get; set; } = new List<LeaveRecord>();
    public ICollection<ProjectAllocation> ProjectAllocations { get; set; } = new List<ProjectAllocation>();
    public ICollection<SkillMatrix> SkillMatrices { get; set; } = new List<SkillMatrix>();
    public ICollection<ResourceMovement> ResourceMovements { get; set; } = new List<ResourceMovement>();
}

public enum ResourceStatus
{
    Active = 1,
    OnLeave = 2,
    Offboarded = 3,
    Pending = 4
}
