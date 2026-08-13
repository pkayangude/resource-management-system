using Microsoft.AspNetCore.Mvc;
using ResourceManagement.Core.DTOs;
using ResourceManagement.Core.Entities;
using ResourceManagement.Core.Interfaces;

namespace ResourceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SkillMatrixController : ControllerBase
{
    private readonly ISkillMatrixRepository _skillRepo;
    private readonly IResourceRepository _resourceRepo;

    public SkillMatrixController(ISkillMatrixRepository skillRepo, IResourceRepository resourceRepo)
    {
        _skillRepo = skillRepo;
        _resourceRepo = resourceRepo;
    }

    /// <summary>Get skill matrix for a resource.</summary>
    [HttpGet("resource/{resourceId:int}")]
    public async Task<ActionResult<IEnumerable<SkillMatrixDto>>> GetByResource(int resourceId)
    {
        var skills = await _skillRepo.GetByResourceAsync(resourceId);
        return Ok(skills.Select(s => ToDto(s)));
    }

    /// <summary>Get all resources with a specific skill.</summary>
    [HttpGet("skill/{skillName}")]
    public async Task<ActionResult<IEnumerable<SkillMatrixDto>>> GetBySkill(string skillName)
    {
        var skills = await _skillRepo.GetBySkillAsync(skillName);
        return Ok(skills.Select(s => ToDto(s)));
    }

    /// <summary>Add a skill to a resource.</summary>
    [HttpPost]
    public async Task<ActionResult<SkillMatrixDto>> Create([FromBody] CreateSkillMatrixDto dto)
    {
        var resource = await _resourceRepo.GetByIdAsync(dto.ResourceId);
        if (resource is null) return NotFound("Resource not found");

        if (dto.ProficiencyLevel < 1 || dto.ProficiencyLevel > 4)
            return BadRequest("Proficiency level must be 1-4 (Beginner/Intermediate/Advanced/Expert)");

        var skill = new SkillMatrix
        {
            ResourceId = dto.ResourceId,
            SkillCategory = dto.SkillCategory,
            SkillName = dto.SkillName,
            ProficiencyLevel = dto.ProficiencyLevel,
            YearsOfExperience = dto.YearsOfExperience,
            Certifications = dto.Certifications,
            Notes = dto.Notes
        };

        var created = await _skillRepo.AddAsync(skill);
        return CreatedAtAction(nameof(GetByResource), new { resourceId = dto.ResourceId }, ToDto(created));
    }

    /// <summary>Update a skill entry.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateSkillMatrixDto dto)
    {
        var skills = await _skillRepo.GetByResourceAsync(dto.ResourceId);
        var skill = skills.FirstOrDefault(s => s.Id == id);
        if (skill is null) return NotFound();

        skill.SkillCategory = dto.SkillCategory;
        skill.SkillName = dto.SkillName;
        skill.ProficiencyLevel = dto.ProficiencyLevel;
        skill.YearsOfExperience = dto.YearsOfExperience;
        skill.Certifications = dto.Certifications;
        skill.Notes = dto.Notes;

        await _skillRepo.UpdateAsync(skill);
        return NoContent();
    }

    /// <summary>Delete a skill entry.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _skillRepo.DeleteAsync(id);
        return NoContent();
    }

    private static SkillMatrixDto ToDto(SkillMatrix s) => new(
        s.Id, s.ResourceId, s.Resource?.FullName ?? string.Empty,
        s.SkillCategory, s.SkillName, s.ProficiencyLevel, s.ProficiencyLabel,
        s.YearsOfExperience, s.Certifications, s.Notes
    );
}
