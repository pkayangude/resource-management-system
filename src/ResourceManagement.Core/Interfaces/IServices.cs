namespace ResourceManagement.Core.Interfaces;

/// <summary>Service for forecast hours calculation based on working days, holidays, and FTE.</summary>
public interface IForecastCalculationService
{
    /// <summary>Computes forecast hours for a given month. Working days * 9 hrs/day * FTE fraction.</summary>
    Task<decimal> CalculateForecastHoursAsync(int year, int month, string location, decimal fteFraction);

    /// <summary>Gets the number of working days for a month in a location.</summary>
    Task<int> GetWorkingDaysAsync(int year, int month, string location);

    /// <summary>Imports the annual holiday xlsx and stores in DB for working day calculations.</summary>
    Task<int> ImportHolidaysFromXlsxAsync(Stream xlsxStream, int year);
}

/// <summary>ILC validation service.</summary>
public interface IIlcValidationService
{
    /// <summary>Validates a batch of weekly ILC claims against forecast and project budgets.</summary>
    Task<IlcValidationResult> ValidateBatchAsync(int batchId);

    /// <summary>Validates a single claim.</summary>
    Task<IlcClaimValidation> ValidateClaimAsync(int claimId);
}

/// <summary>Result of ILC batch validation.</summary>
public record IlcValidationResult(
    int TotalClaims,
    int ValidCount,
    int WarningCount,
    int InvalidCount,
    IEnumerable<IlcClaimValidation> ClaimValidations
);

/// <summary>Validation result for a single ILC claim.</summary>
public record IlcClaimValidation(
    int ClaimId,
    string ResourceName,
    decimal ClaimedHours,
    decimal ForecastHours,
    decimal Variance,
    bool ExceedsForecast,
    bool ExceedsProjectBudget,
    string Status,
    IEnumerable<string> Messages
);

/// <summary>Excel import service.</summary>
public interface IExcelImportService
{
    /// <summary>Parses xlsx column headers for mapping preview.</summary>
    Task<ExcelPreviewResult> PreviewXlsxAsync(Stream stream, string fileName);

    /// <summary>Imports resources from xlsx with column mapping.</summary>
    Task<ImportResult> ImportResourcesAsync(Stream stream, Dictionary<string, string> columnMapping);

    /// <summary>Imports ILC weekly claims from xlsx.</summary>
    Task<ImportResult> ImportIlcClaimsAsync(Stream stream, Dictionary<string, string> columnMapping, string uploadedBy);

    /// <summary>Imports forecast allocations from xlsx.</summary>
    Task<ImportResult> ImportForecastAsync(Stream stream, Dictionary<string, string> columnMapping);

    /// <summary>Imports resource movements from xlsx.</summary>
    Task<ImportResult> ImportResourceMovementsAsync(Stream stream, Dictionary<string, string> columnMapping);
}

/// <summary>Preview of an xlsx file showing column headers and sample rows.</summary>
public record ExcelPreviewResult(
    IEnumerable<string> Headers,
    IEnumerable<Dictionary<string, string?>> SampleRows,
    int TotalRows,
    string? ErrorMessage = null
);

/// <summary>Result of a bulk import operation.</summary>
public record ImportResult(
    bool Success,
    int ImportedCount,
    int SkippedCount,
    int ErrorCount,
    IEnumerable<string> Errors,
    string? Summary = null
);

/// <summary>Band mix calculator.</summary>
public interface IBandMixService
{
    Task<IEnumerable<BandMixDto>> CalculateBandMixAsync(int year, int month);
    Task<IEnumerable<BandMixDto>> CalculateBandMixForRangeAsync(int year, int startMonth, int endMonth);
}

public record BandMixDto(
    string Band,
    decimal Weightage,
    int Fte,
    decimal TotalBandValue,
    decimal BandPercentage,
    decimal BandMix
);
