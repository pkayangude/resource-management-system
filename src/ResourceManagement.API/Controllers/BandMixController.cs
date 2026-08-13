using Microsoft.AspNetCore.Mvc;
using ResourceManagement.Core.Interfaces;

namespace ResourceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BandMixController : ControllerBase
{
    private readonly IBandMixService _bandMixService;

    public BandMixController(IBandMixService bandMixService) => _bandMixService = bandMixService;

    /// <summary>Calculate band mix for a specific month.</summary>
    [HttpGet("{year:int}/{month:int}")]
    public async Task<ActionResult<IEnumerable<BandMixDto>>> GetBandMix(int year, int month)
    {
        var result = await _bandMixService.CalculateBandMixAsync(year, month);
        return Ok(result);
    }

    /// <summary>Calculate band mix for a quarter or full year range.</summary>
    [HttpGet("{year:int}/range")]
    public async Task<ActionResult<IEnumerable<BandMixDto>>> GetBandMixRange(
        int year,
        [FromQuery] int startMonth = 1,
        [FromQuery] int endMonth = 12)
    {
        if (startMonth < 1 || startMonth > 12 || endMonth < 1 || endMonth > 12 || startMonth > endMonth)
            return BadRequest("Invalid month range");

        var result = await _bandMixService.CalculateBandMixForRangeAsync(year, startMonth, endMonth);
        return Ok(result);
    }
}

/// <summary>Controller for Excel upload preview and mapping.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ImportController : ControllerBase
{
    private readonly IExcelImportService _importService;

    public ImportController(IExcelImportService importService) => _importService = importService;

    /// <summary>Preview any xlsx file to get headers and sample rows for column mapping.</summary>
    [HttpPost("preview")]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<ExcelPreviewResult>> Preview(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided");

        await using var stream = file.OpenReadStream();
        var result = await _importService.PreviewXlsxAsync(stream, file.FileName);
        return Ok(result);
    }

    /// <summary>Bulk import resources from xlsx with column mapping.</summary>
    [HttpPost("resources")]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<ImportResult>> ImportResources(
        IFormFile file,
        [FromForm] string columnMappingJson)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided");

        var columnMapping = System.Text.Json.JsonSerializer
            .Deserialize<Dictionary<string, string>>(columnMappingJson)
            ?? new Dictionary<string, string>();

        await using var stream = file.OpenReadStream();
        var result = await _importService.ImportResourcesAsync(stream, columnMapping);
        return Ok(result);
    }

    /// <summary>Import resource movements xlsx.</summary>
    [HttpPost("movements")]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<ImportResult>> ImportMovements(
        IFormFile file,
        [FromForm] string columnMappingJson)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided");

        var columnMapping = System.Text.Json.JsonSerializer
            .Deserialize<Dictionary<string, string>>(columnMappingJson)
            ?? new Dictionary<string, string>();

        await using var stream = file.OpenReadStream();
        var result = await _importService.ImportResourceMovementsAsync(stream, columnMapping);
        return Ok(result);
    }
}
