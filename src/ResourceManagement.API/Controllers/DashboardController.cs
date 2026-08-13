using Microsoft.AspNetCore.Mvc;
using ResourceManagement.Core.DTOs;
using ResourceManagement.Core.Interfaces;

namespace ResourceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IResourceRepository _resourceRepo;
    private readonly IForecastRepository _forecastRepo;
    private readonly IIlcRepository _ilcRepo;
    private readonly ILeaveRepository _leaveRepo;
    private readonly IProjectRepository _projectRepo;

    public DashboardController(
        IResourceRepository resourceRepo,
        IForecastRepository forecastRepo,
        IIlcRepository ilcRepo,
        ILeaveRepository leaveRepo,
        IProjectRepository projectRepo)
    {
        _resourceRepo = resourceRepo;
        _forecastRepo = forecastRepo;
        _ilcRepo = ilcRepo;
        _leaveRepo = leaveRepo;
        _projectRepo = projectRepo;
    }

    /// <summary>Get dashboard summary KPIs.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        var now = DateTime.UtcNow;
        var year = now.Year;
        var month = now.Month;

        var totalActive = await _resourceRepo.CountActiveAsync();
        var activeLeaves = await _leaveRepo.GetActiveAsync(now);

        var forecastAllocations = (await _forecastRepo.GetByMonthAsync(year, month)).ToList();
        var totalForecast = forecastAllocations.Sum(f => f.ForecastHours);
        var totalActual = forecastAllocations.Sum(f => f.ActualHours ?? 0);
        var utilization = totalForecast > 0 ? totalActual / totalForecast * 100 : 0;

        var projects = (await _projectRepo.GetAllAsync()).ToList();
        var projectsAtRisk = projects.Count(p => p.IsOverBudget);

        return Ok(new DashboardSummaryDto(
            TotalActiveResources: totalActive,
            OnboardedThisMonth: 0,    // can be enhanced with movement tracking
            OffboardedThisMonth: 0,
            ResourcesOnLeave: activeLeaves.Count(),
            TotalForecastHoursCurrentMonth: totalForecast,
            TotalActualHoursCurrentMonth: totalActual,
            UtilizationPercentage: Math.Round(utilization, 2),
            ProjectsAtRisk: projectsAtRisk,
            PendingIlcValidations: 0
        ));
    }
}
