using Microsoft.AspNetCore.Mvc;
using ResourceManagement.Core.DTOs;
using ResourceManagement.Core.Entities;
using ResourceManagement.Core.Interfaces;

namespace ResourceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LeaveController : ControllerBase
{
    private readonly ILeaveRepository _leaveRepo;
    private readonly IForecastCalculationService _calcService;
    private readonly IResourceRepository _resourceRepo;

    public LeaveController(
        ILeaveRepository leaveRepo,
        IForecastCalculationService calcService,
        IResourceRepository resourceRepo)
    {
        _leaveRepo = leaveRepo;
        _calcService = calcService;
        _resourceRepo = resourceRepo;
    }

    /// <summary>Get leave records for a resource.</summary>
    [HttpGet("resource/{resourceId:int}")]
    public async Task<ActionResult<IEnumerable<LeaveRecordDto>>> GetByResource(int resourceId)
    {
        var records = await _leaveRepo.GetByResourceAsync(resourceId);
        return Ok(records.Select(ToDto));
    }

    /// <summary>Get all active leaves as of today.</summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<LeaveRecordDto>>> GetActive()
    {
        var records = await _leaveRepo.GetActiveAsync(DateTime.UtcNow);
        return Ok(records.Select(ToDto));
    }

    /// <summary>Create a long leave record.</summary>
    [HttpPost]
    public async Task<ActionResult<LeaveRecordDto>> Create([FromBody] CreateLeaveDto dto)
    {
        var resource = await _resourceRepo.GetByIdAsync(dto.ResourceId);
        if (resource is null) return NotFound("Resource not found");

        if (!Enum.TryParse<LeaveType>(dto.LeaveType, true, out var leaveType))
            return BadRequest($"Invalid leave type: {dto.LeaveType}");

        if (dto.StartDate >= dto.EndDate)
            return BadRequest("Start date must be before end date");

        if (await _leaveRepo.HasOverlappingLeaveAsync(dto.ResourceId, dto.StartDate, dto.EndDate))
            return Conflict("Overlapping leave already exists for this resource in the given period");

        // Calculate working days (approximate, assuming same location)
        int totalDays = 0;
        for (var d = dto.StartDate; d <= dto.EndDate; d = d.AddDays(1))
        {
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                totalDays++;
        }

        var impactHours = totalDays * 9m;  // 9 hrs/day

        var record = new LeaveRecord
        {
            ResourceId = dto.ResourceId,
            LeaveType = leaveType,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            TotalDays = totalDays,
            Reason = dto.Reason,
            Status = LeaveStatus.Pending,
            ForecastImpactHours = impactHours
        };

        var created = await _leaveRepo.AddAsync(record);
        return CreatedAtAction(nameof(GetByResource), new { resourceId = dto.ResourceId }, ToDto(created));
    }

    /// <summary>Approve a leave request.</summary>
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromQuery] string approvedBy = "manager")
    {
        var records = await _leaveRepo.GetByResourceAsync(0);
        var record = records.FirstOrDefault(); // placeholder
        if (record is null) return NotFound();

        record.Status = LeaveStatus.Approved;
        record.ApprovedBy = approvedBy;
        record.ApprovedAt = DateTime.UtcNow;
        await _leaveRepo.UpdateAsync(record);
        return NoContent();
    }

    /// <summary>Cancel a leave request.</summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var records = await _leaveRepo.GetByResourceAsync(0);
        var record = records.FirstOrDefault();
        if (record is null) return NotFound();

        record.Status = LeaveStatus.Cancelled;
        await _leaveRepo.UpdateAsync(record);
        return NoContent();
    }

    private static LeaveRecordDto ToDto(LeaveRecord l) => new(
        l.Id, l.ResourceId, l.Resource?.FullName ?? string.Empty,
        l.LeaveType.ToString(), l.StartDate, l.EndDate, l.TotalDays,
        l.Reason, l.Status.ToString(), l.ApprovedBy, l.ForecastImpactHours
    );
}
