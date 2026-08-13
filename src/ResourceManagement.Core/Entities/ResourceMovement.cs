using System.ComponentModel.DataAnnotations;

namespace ResourceManagement.Core.Entities;

/// <summary>Resource movement (onboarding / offboarding / transfer).</summary>
public class ResourceMovement
{
    public int Id { get; set; }
    public int ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;

    public MovementType MovementType { get; set; }

    [MaxLength(200)]
    public string? Portfolio { get; set; }

    [MaxLength(200)]
    public string? TeamPod { get; set; }

    [MaxLength(100)]
    public string? MvtFlex { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }

    public DateTime? ReleaseDate { get; set; }
    public DateTime? StartDate { get; set; }

    [MaxLength(200)]
    public string? ReasonsForRelease { get; set; }

    public bool IsBackfilled { get; set; }

    [MaxLength(200)]
    public string? BackfillResourceName { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}

public enum MovementType
{
    Onboarding = 1,
    Offboarding = 2,
    Transfer = 3,
    RollOff = 4,
    Attrition = 5
}
