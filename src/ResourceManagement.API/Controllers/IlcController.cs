using Microsoft.AspNetCore.Mvc;
using ResourceManagement.Core.DTOs;
using ResourceManagement.Core.Interfaces;

namespace ResourceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class IlcController : ControllerBase
{
    private readonly IIlcRepository _ilcRepo;
    private readonly IIlcValidationService _validationService;
    private readonly IExcelImportService _importService;

    public IlcController(
        IIlcRepository ilcRepo,
        IIlcValidationService validationService,
        IExcelImportService importService)
    {
        _ilcRepo = ilcRepo;
        _validationService = validationService;
        _importService = importService;
    }

    /// <summary>Get ILC claims for a resource.</summary>
    [HttpGet("resource/{resourceId:int}")]
    public async Task<ActionResult<IEnumerable<IlcClaimDto>>> GetByResource(int resourceId)
    {
        var claims = await _ilcRepo.GetByResourceAsync(resourceId);
        return Ok(claims.Select(c => new IlcClaimDto(
            c.Id, c.ResourceId, c.Resource?.FullName ?? string.Empty,
            c.WeekEndingDate, c.Year, c.WeekNumber, c.ClaimedHours,
            c.ClaimCode, c.ProjectName, c.DemandCode,
            c.ValidationStatus.ToString(), c.ValidationMessages,
            c.ExceedsForecast, c.ExceedsProjectBudget
        )));
    }

    /// <summary>Get claims for a week.</summary>
    [HttpGet("week")]
    public async Task<ActionResult<IEnumerable<IlcClaimDto>>> GetByWeek([FromQuery] DateTime weekEndingDate)
    {
        var claims = await _ilcRepo.GetByWeekAsync(weekEndingDate);
        return Ok(claims.Select(c => new IlcClaimDto(
            c.Id, c.ResourceId, c.Resource?.FullName ?? string.Empty,
            c.WeekEndingDate, c.Year, c.WeekNumber, c.ClaimedHours,
            c.ClaimCode, c.ProjectName, c.DemandCode,
            c.ValidationStatus.ToString(), c.ValidationMessages,
            c.ExceedsForecast, c.ExceedsProjectBudget
        )));
    }

    /// <summary>Preview xlsx before upload - returns headers and sample rows for column mapping.</summary>
    [HttpPost("preview")]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<ExcelPreviewResult>> Preview(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided");

        await using var stream = file.OpenReadStream();
        var preview = await _importService.PreviewXlsxAsync(stream, file.FileName);
        return Ok(preview);
    }

    /// <summary>Upload ILC weekly xlsx with column mapping and validate claims.</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<ImportResult>> Upload(
        IFormFile file,
        [FromForm] string columnMappingJson,
        [FromForm] string uploadedBy = "system")
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided");

        Dictionary<string, string> columnMapping;
        try
        {
            columnMapping = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(columnMappingJson)
                ?? new Dictionary<string, string>();
        }
        catch
        {
            return BadRequest("Invalid column mapping JSON");
        }

        await using var stream = file.OpenReadStream();
        var result = await _importService.ImportIlcClaimsAsync(stream, columnMapping, uploadedBy);
        return Ok(result);
    }

    /// <summary>Validate all claims in a batch.</summary>
    [HttpPost("validate/{batchId:int}")]
    public async Task<ActionResult<IlcValidationResult>> Validate(int batchId)
    {
        var result = await _validationService.ValidateBatchAsync(batchId);
        return Ok(result);
    }

    /// <summary>Get total claimed hours for a resource in a month.</summary>
    [HttpGet("resource/{resourceId:int}/hours/{year:int}/{month:int}")]
    public async Task<ActionResult<decimal>> GetMonthlyHours(int resourceId, int year, int month)
    {
        var hours = await _ilcRepo.GetTotalClaimedHoursAsync(resourceId, year, month);
        return Ok(hours);
    }
}
