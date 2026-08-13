using System.ComponentModel.DataAnnotations;

namespace ResourceManagement.Core.Entities;

/// <summary>Tracks batch uploads of ILC weekly xlsx files.</summary>
public class IlcUploadBatch
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string FileName { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string UploadedBy { get; set; } = string.Empty;

    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int WarningRows { get; set; }

    public UploadStatus Status { get; set; } = UploadStatus.Processing;

    [MaxLength(2000)]
    public string? Summary { get; set; }

    public ICollection<IlcClaim> Claims { get; set; } = new List<IlcClaim>();
}

public enum UploadStatus
{
    Processing = 0,
    Completed = 1,
    CompletedWithWarnings = 2,
    Failed = 3
}
