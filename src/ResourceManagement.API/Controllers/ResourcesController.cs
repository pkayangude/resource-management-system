using Microsoft.AspNetCore.Mvc;
using ResourceManagement.Core.DTOs;
using ResourceManagement.Core.Entities;
using ResourceManagement.Core.Interfaces;

namespace ResourceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ResourcesController : ControllerBase
{
    private readonly IResourceRepository _repo;

    public ResourcesController(IResourceRepository repo) => _repo = repo;

    /// <summary>Get all active resources.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ResourceDto>>> GetAll([FromQuery] bool includeOffboarded = false)
    {
        var resources = await _repo.GetAllAsync(includeOffboarded);
        return Ok(resources.Select(ToDto));
    }

    /// <summary>Get a resource by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ResourceDto>> GetById(int id)
    {
        var resource = await _repo.GetByIdAsync(id);
        if (resource is null) return NotFound();
        return Ok(ToDto(resource));
    }

    /// <summary>Create a new resource (single onboarding).</summary>
    [HttpPost]
    public async Task<ActionResult<ResourceDto>> Create([FromBody] CreateResourceDto dto)
    {
        if (await _repo.ExistsAsync(dto.EmpId))
            return Conflict(new { message = $"Resource with EmpId '{dto.EmpId}' already exists." });

        var resource = new Resource
        {
            EmpId = dto.EmpId,
            TalentId = dto.TalentId,
            FullName = dto.FullName,
            Corporate = dto.Corporate,
            Pcode = dto.Pcode,
            Country = dto.Country,
            Location = dto.Location,
            Band = dto.Band,
            CostRate = dto.CostRate,
            Manager = dto.Manager,
            Team = dto.Team,
            Category = dto.Category,
            IppfCategory = dto.IppfCategory,
            JobRoleSkillSet = dto.JobRoleSkillSet,
            EmployeeType = dto.EmployeeType,
            IntranetId = dto.IntranetId,
            DateOfJoining = dto.DateOfJoining,
            OnboardingDate = dto.OnboardingDate ?? DateTime.UtcNow,
            Status = ResourceStatus.Active
        };

        var created = await _repo.AddAsync(resource);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToDto(created));
    }

    /// <summary>Update an existing resource.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateResourceDto dto)
    {
        var resource = await _repo.GetByIdAsync(id);
        if (resource is null) return NotFound();

        resource.FullName = dto.FullName;
        resource.Country = dto.Country;
        resource.Location = dto.Location;
        resource.Band = dto.Band;
        resource.CostRate = dto.CostRate;
        resource.Manager = dto.Manager;
        resource.Team = dto.Team;
        resource.Category = dto.Category;
        resource.JobRoleSkillSet = dto.JobRoleSkillSet;
        resource.IntranetId = dto.IntranetId;

        await _repo.UpdateAsync(resource);
        return NoContent();
    }

    /// <summary>Offboard a resource (set status and date).</summary>
    [HttpPost("{id:int}/offboard")]
    public async Task<IActionResult> Offboard(int id, [FromBody] OffboardRequest request)
    {
        var resource = await _repo.GetByIdAsync(id);
        if (resource is null) return NotFound();

        resource.Status = ResourceStatus.Offboarded;
        resource.OffboardingDate = request.OffboardingDate ?? DateTime.UtcNow;
        await _repo.UpdateAsync(resource);
        return NoContent();
    }

    /// <summary>Get count of active resources.</summary>
    [HttpGet("count")]
    public async Task<ActionResult<int>> Count() => Ok(await _repo.CountActiveAsync());

    private static ResourceDto ToDto(Resource r) => new(
        r.Id, r.EmpId, r.TalentId, r.FullName, r.Corporate, r.Pcode,
        r.Country, r.Location, r.Band, r.CostRate, r.Manager, r.Team,
        r.Category, r.IppfCategory, r.JobRoleSkillSet, r.EmployeeType,
        r.IntranetId, r.Status.ToString(), r.OnboardingDate, r.OffboardingDate
    );
}

public record OffboardRequest(DateTime? OffboardingDate, string? Reason);
