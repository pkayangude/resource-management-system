using System.ComponentModel.DataAnnotations;

namespace ResourceManagement.Core.Entities;

/// <summary>ILC (Intranet Labour Claim) weekly entry.</summary>
public class IlcClaim
{
    public int Id { get; set; }
    public int ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;

    /// <summary>Week ending date (Saturday).</summary>
    public DateTime WeekEndingDate { get; set; }

    public int Year { get; set; }
    public int WeekNumber { get; set; }

    public decimal ClaimedHours { get; set; }

    [MaxLength(100)]
    public string? ClaimCode { get; set; }

    [MaxLength(200)]
    public string? ProjectName { get; set; }

    [MaxLength(100)]
    public string? ProjectDbId { get; set; }

    [MaxLength(200)]
    public string? DemandCode { get; set; }

    public IlcValidationStatus ValidationStatus { get; set; } = IlcValidationStatus.Pending;

    [MaxLength(1000)]
    public string? ValidationMessages { get; set; }

    /// <summary>Hours exceed forecast threshold flag.</summary>
    public bool ExceedsForecast { get; set; }

    /// <summary>Hours exceed project/demand budget flag.</summary>
    public bool ExceedsProjectBudget { get; set; }

    public int? UploadBatchId { get; set; }
    public IlcUploadBatch? UploadBatch { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum IlcValidationStatus
{
    Pending = 0,
    Valid = 1,
    Warning = 2,
    Invalid = 3
}
