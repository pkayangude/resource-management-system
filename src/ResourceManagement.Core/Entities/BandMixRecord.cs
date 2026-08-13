using System.ComponentModel.DataAnnotations;

namespace ResourceManagement.Core.Entities;

/// <summary>Band mix calculation record for a given month.</summary>
public class BandMixRecord
{
    public int Id { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }

    [Required, MaxLength(20)]
    public string Band { get; set; } = string.Empty;

    public decimal Weightage { get; set; }

    public int Fte { get; set; }

    public decimal TotalBandValue { get; set; }

    public decimal BandPercentage { get; set; }

    public decimal BandMix { get; set; }

    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}
