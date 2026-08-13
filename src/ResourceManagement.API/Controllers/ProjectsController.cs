using Microsoft.AspNetCore.Mvc;
using ResourceManagement.Core.DTOs;
using ResourceManagement.Core.Entities;
using ResourceManagement.Core.Interfaces;

namespace ResourceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectRepository _projectRepo;

    public ProjectsController(IProjectRepository projectRepo) => _projectRepo = projectRepo;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAll()
    {
        var projects = await _projectRepo.GetAllAsync();
        return Ok(projects.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDto>> GetById(int id)
    {
        var project = await _projectRepo.GetByIdAsync(id);
        if (project is null) return NotFound();
        return Ok(ToDto(project));
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectDto dto)
    {
        var existing = await _projectRepo.GetByCodeAsync(dto.ProjectCode);
        if (existing is not null)
            return Conflict($"Project with code '{dto.ProjectCode}' already exists");

        if (!Enum.TryParse<ProjectType>(dto.ProjectType, true, out var projectType))
            return BadRequest($"Invalid project type: {dto.ProjectType}");

        var project = new Project
        {
            ProjectCode = dto.ProjectCode,
            ProjectName = dto.ProjectName,
            ProjectDbId = dto.ProjectDbId,
            ProjectType = projectType,
            ClaimCode = dto.ClaimCode,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            TotalBudgetHours = dto.TotalBudgetHours,
            Portfolio = dto.Portfolio,
            PemName = dto.PemName,
            Status = ProjectStatus.Active
        };

        var created = await _projectRepo.AddAsync(project);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToDto(created));
    }

    /// <summary>Allocate a resource to a project with budget hours.</summary>
    [HttpPost("{projectId:int}/allocate")]
    public async Task<ActionResult<ProjectAllocationDto>> AllocateResource(
        int projectId,
        [FromBody] AllocateResourceRequest req)
    {
        var project = await _projectRepo.GetByIdAsync(projectId);
        if (project is null) return NotFound("Project not found");

        var allocation = new ProjectAllocation
        {
            ResourceId = req.ResourceId,
            ProjectId = projectId,
            AllocationStartDate = req.StartDate,
            AllocationEndDate = req.EndDate,
            WeeklyHours = req.WeeklyHours,
            BudgetedHours = req.BudgetedHours,
            FteFraction = req.FteFraction,
            Notes = req.Notes,
            Status = AllocationStatus.Active
        };

        var created = await _projectRepo.AddAllocationAsync(allocation);
        return Ok(new ProjectAllocationDto(
            created.Id, created.ResourceId,
            created.Resource?.FullName ?? string.Empty,
            projectId, project.ProjectName,
            created.AllocationStartDate, created.AllocationEndDate,
            created.WeeklyHours, created.BudgetedHours, created.ConsumedHours,
            created.RemainingHours, created.IsOverBudget, created.FteFraction,
            created.Status.ToString()
        ));
    }

    /// <summary>Get allocations for a resource.</summary>
    [HttpGet("allocations/resource/{resourceId:int}")]
    public async Task<ActionResult<IEnumerable<ProjectAllocationDto>>> GetAllocationsByResource(int resourceId)
    {
        var allocations = await _projectRepo.GetAllocationsByResourceAsync(resourceId);
        return Ok(allocations.Select(a => new ProjectAllocationDto(
            a.Id, a.ResourceId, a.Resource?.FullName ?? string.Empty,
            a.ProjectId, a.Project?.ProjectName ?? string.Empty,
            a.AllocationStartDate, a.AllocationEndDate,
            a.WeeklyHours, a.BudgetedHours, a.ConsumedHours,
            a.RemainingHours, a.IsOverBudget, a.FteFraction,
            a.Status.ToString()
        )));
    }

    private static ProjectDto ToDto(Project p) => new(
        p.Id, p.ProjectCode, p.ProjectName, p.ProjectDbId,
        p.ProjectType.ToString(), p.ClaimCode,
        p.StartDate, p.EndDate, p.TotalBudgetHours,
        p.ConsumedHours, p.RemainingHours, p.IsOverBudget,
        p.Portfolio, p.Status.ToString()
    );
}

public record AllocateResourceRequest(
    int ResourceId,
    DateTime StartDate,
    DateTime EndDate,
    decimal WeeklyHours,
    decimal BudgetedHours,
    decimal FteFraction,
    string? Notes
);
