using Microsoft.AspNetCore.Mvc;
using ResourceManagement.Core.DTOs;
using ResourceManagement.Core.Entities;
using ResourceManagement.Core.Interfaces;

namespace ResourceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ForecastController : ControllerBase
{
    private readonly IForecastRepository _forecastRepo;
    private readonly IForecastCalculationService _calcService;
    private readonly IResourceRepository _resourceRepo;
    private readonly IExcelImportService _importService;

    public ForecastController(
        IForecastRepository forecastRepo,
        IForecastCalculationService calcService,
        IResourceRepository resourceRepo,
        IExcelImportService importService)
    {
        _forecastRepo = forecastRepo;
        _calcService = calcService;
        _resourceRepo = resourceRepo;
        _importService = importService;
    }

    /// <summary>Get forecast for a specific year/month.</summary>
    [HttpGet("{year:int}/{month:int}")]
    public async Task<ActionResult<IEnumerable<ForecastAllocationDto>>> GetByMonth(int year, int month)
    {
        var allocations = await _forecastRepo.GetByMonthAsync(year, month);
        return Ok(allocations.Select(ToDto));
    }

    /// <summary>Get forecast for a specific resource.</summary>
    [HttpGet("resource/{resourceId:int}")]
    public async Task<ActionResult<IEnumerable<ForecastAllocationDto>>> GetByResource(int resourceId)
    {
        var allocations = await _forecastRepo.GetByResourceAsync(resourceId);
        return Ok(allocations.Select(ToDto));
    }

    /// <summary>Create a forecast allocation with auto-calculated hours.</summary>
    [HttpPost]
    public async Task<ActionResult<ForecastAllocationDto>> Create([FromBody] CreateForecastDto dto)
    {
        var resource = await _resourceRepo.GetByIdAsync(dto.ResourceId);
        if (resource is null) return NotFound("Resource not found");

        var existing = await _forecastRepo.GetByResourceAndMonthAsync(dto.ResourceId, dto.Year, dto.Month);
        if (existing is not null)
            return Conflict("Forecast already exists for this resource and month");

        var hours = await _calcService.CalculateForecastHoursAsync(
            dto.Year, dto.Month, resource.Location, dto.FteFraction);

        var allocation = new ForecastAllocation
        {
            ResourceId = dto.ResourceId,
            Year = dto.Year,
            Month = dto.Month,
            ForecastHours = hours,
            ForecastCost = hours * resource.CostRate,
            FteFraction = dto.FteFraction,
            Comments = dto.Comments
        };

        var created = await _forecastRepo.AddAsync(allocation);
        return CreatedAtAction(nameof(GetByResource), new { resourceId = dto.ResourceId }, ToDto(created));
    }

    /// <summary>Generate forecasts for all active resources for a month.</summary>
    [HttpPost("generate/{year:int}/{month:int}")]
    public async Task<ActionResult<object>> GenerateForecast(int year, int month)
    {
        var resources = await _resourceRepo.GetAllAsync();
        var created = 0;
        var skipped = 0;

        foreach (var resource in resources.Where(r => r.IsActive))
        {
            var existing = await _forecastRepo.GetByResourceAndMonthAsync(resource.Id, year, month);
            if (existing is not null) { skipped++; continue; }

            var hours = await _calcService.CalculateForecastHoursAsync(year, month, resource.Location, 1m);
            await _forecastRepo.AddAsync(new ForecastAllocation
            {
                ResourceId = resource.Id,
                Year = year,
                Month = month,
                ForecastHours = hours,
                ForecastCost = hours * resource.CostRate,
                FteFraction = 1m
            });
            created++;
        }

        return Ok(new { message = $"Generated {created} forecasts, skipped {skipped} existing." });
    }

    /// <summary>Import annual holiday xlsx (used for working day calculation).</summary>
    [HttpPost("import-holidays")]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<object>> ImportHolidays(IFormFile file, [FromQuery] int year = 0)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided");

        if (year == 0) year = DateTime.UtcNow.Year;

        await using var stream = file.OpenReadStream();
        var count = await _calcService.ImportHolidaysFromXlsxAsync(stream, year);
        return Ok(new { imported = count, year });
    }

    /// <summary>Get working days for a month/location.</summary>
    [HttpGet("working-days/{year:int}/{month:int}")]
    public async Task<ActionResult<int>> GetWorkingDays(int year, int month, [FromQuery] string location = "India")
    {
        var days = await _calcService.GetWorkingDaysAsync(year, month, location);
        return Ok(days);
    }

    /// <summary>Upload forecast xlsx with column mapping.</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<ImportResult>> UploadForecast(
        IFormFile file,
        [FromBody] Dictionary<string, string> columnMapping)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided");

        await using var stream = file.OpenReadStream();
        var result = await _importService.ImportForecastAsync(stream, columnMapping);
        return Ok(result);
    }

    private static ForecastAllocationDto ToDto(ForecastAllocation f) => new(
        f.Id, f.ResourceId, f.Resource?.FullName ?? string.Empty,
        f.Resource?.Band ?? string.Empty, f.Year, f.Month,
        f.ForecastHours, f.ForecastCost, f.FteFraction,
        f.ActualHours, f.VarianceHours, f.Comments
    );
}
