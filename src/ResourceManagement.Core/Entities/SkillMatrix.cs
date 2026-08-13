using System.ComponentModel.DataAnnotations;

namespace ResourceManagement.Core.Entities;

/// <summary>Skill matrix entry for a resource.</summary>
public class SkillMatrix
{
    public int Id { get; set; }
    public int ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;

    [Required, MaxLength(100)]
    public string SkillCategory { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string SkillName { get; set; } = string.Empty;

    /// <summary>Proficiency: 1=Beginner, 2=Intermediate, 3=Advanced, 4=Expert.</summary>
    public int ProficiencyLevel { get; set; }

    public string ProficiencyLabel => ProficiencyLevel switch
    {
        1 => "Beginner",
        2 => "Intermediate",
        3 => "Advanced",
        4 => "Expert",
        _ => "Unknown"
    };

    public int YearsOfExperience { get; set; }

    [MaxLength(500)]
    public string? Certifications { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}
