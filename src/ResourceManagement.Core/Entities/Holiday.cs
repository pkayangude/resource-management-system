using System.ComponentModel.DataAnnotations;

namespace ResourceManagement.Core.Entities;

/// <summary>Public holiday per location/state for working day calculations.</summary>
public class Holiday
{
    public int Id { get; set; }
    public int Year { get; set; }
    public DateTime Date { get; set; }

    [Required, MaxLength(200)]
    public string HolidayName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Location { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Country { get; set; } = "India";

    public bool IsNational { get; set; }
}
