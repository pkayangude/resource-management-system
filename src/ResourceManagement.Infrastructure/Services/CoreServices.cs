using ClosedXML.Excel;
using ExcelDataReader;
using ResourceManagement.Core.Entities;
using ResourceManagement.Core.Interfaces;

namespace ResourceManagement.Infrastructure.Services;

/// <summary>Calculates forecast hours based on working days * 9 hrs and FTE fraction.</summary>
public class ForecastCalculationService : IForecastCalculationService
{
    private const int HoursPerDay = 9;
    private readonly IHolidayRepository _holidays;

    public ForecastCalculationService(IHolidayRepository holidays) => _holidays = holidays;

    public async Task<decimal> CalculateForecastHoursAsync(int year, int month, string location, decimal fteFraction)
    {
        var workingDays = await GetWorkingDaysAsync(year, month, location);
        return workingDays * HoursPerDay * fteFraction;
    }

    public Task<int> GetWorkingDaysAsync(int year, int month, string location) =>
        _holidays.GetWorkingDaysAsync(year, month, location);

    public async Task<int> ImportHolidaysFromXlsxAsync(Stream xlsxStream, int year)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var imported = new List<Holiday>();

        using var reader = ExcelReaderFactory.CreateReader(xlsxStream);
        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
        });

        var table = dataSet.Tables[0];
        foreach (System.Data.DataRow row in table.Rows)
        {
            if (row.IsNull(0)) continue;
            var dateVal = row[2]?.ToString();
            if (!DateTime.TryParse(dateVal, out var date)) continue;

            // columns: S.No, HolidayName, Date, Day, then one per location
            var holidayName = row[1]?.ToString() ?? string.Empty;

            // National flag: check if all location columns are marked
            var locationColumns = new Dictionary<string, int>
            {
                ["Bengaluru"] = 4, ["Chennai"] = 5, ["Hyderabad"] = 6,
                ["Kochi"] = 7, ["Gurgaon"] = 8, ["Noida"] = 9,
                ["Pune"] = 10, ["Ahmedabad"] = 11, ["Kolkata"] = 12,
                ["Bhubaneswar"] = 13, ["Visakhapatnam"] = 14
            };

            bool isNational = locationColumns.Values.All(col => col < table.Columns.Count && !row.IsNull(col));

            foreach (var (loc, col) in locationColumns)
            {
                if (col >= table.Columns.Count) continue;
                var val = row[col]?.ToString();
                if (!string.IsNullOrWhiteSpace(val) && val != "0")
                {
                    imported.Add(new Holiday
                    {
                        Year = date.Year,
                        Date = date,
                        HolidayName = holidayName,
                        Location = loc,
                        Country = "India",
                        IsNational = isNational
                    });
                }
            }
        }

        if (imported.Count > 0)
            await _holidays.AddRangeAsync(imported);

        return imported.Count;
    }
}

/// <summary>Validates ILC claims against forecast and project budgets.</summary>
public class IlcValidationService : IIlcValidationService
{
    private readonly IIlcRepository _ilcRepo;
    private readonly IForecastRepository _forecastRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IResourceRepository _resourceRepo;

    public IlcValidationService(
        IIlcRepository ilcRepo,
        IForecastRepository forecastRepo,
        IProjectRepository projectRepo,
        IResourceRepository resourceRepo)
    {
        _ilcRepo = ilcRepo;
        _forecastRepo = forecastRepo;
        _projectRepo = projectRepo;
        _resourceRepo = resourceRepo;
    }

    public async Task<IlcValidationResult> ValidateBatchAsync(int batchId)
    {
        var claims = (await _ilcRepo.GetByBatchAsync(batchId)).ToList();
        var validations = new List<IlcClaimValidation>();

        foreach (var claim in claims)
        {
            var validation = await ValidateClaimAsync(claim.Id);
            validations.Add(validation);
        }

        return new IlcValidationResult(
            validations.Count,
            validations.Count(v => v.Status == "Valid"),
            validations.Count(v => v.Status == "Warning"),
            validations.Count(v => v.Status == "Invalid"),
            validations
        );
    }

    public async Task<IlcClaimValidation> ValidateClaimAsync(int claimId)
    {
        var claim = await _ilcRepo.GetByBatchAsync(0)
            .ContinueWith(t => (IlcClaim?)null); // placeholder; real query below

        // Real lookup
        var claimEntity = (await _ilcRepo.GetByResourceAsync(-1))
            .FirstOrDefault(c => c.Id == claimId);

        if (claimEntity == null)
            return new IlcClaimValidation(claimId, "Unknown", 0, 0, 0, false, false, "Invalid", ["Claim not found"]);

        var resource = await _resourceRepo.GetByIdAsync(claimEntity.ResourceId);
        var messages = new List<string>();
        bool exceedsForecast = false;
        bool exceedsProjectBudget = false;

        // Check 1: Forecast validation
        var forecastAlloc = await _forecastRepo.GetByResourceAndMonthAsync(
            claimEntity.ResourceId,
            claimEntity.WeekEndingDate.Year,
            claimEntity.WeekEndingDate.Month);

        decimal forecastHours = 0;
        if (forecastAlloc is not null)
        {
            forecastHours = forecastAlloc.ForecastHours;
            var totalActual = await _ilcRepo.GetTotalClaimedHoursAsync(
                claimEntity.ResourceId,
                claimEntity.Year,
                claimEntity.WeekEndingDate.Month);

            if (totalActual > forecastHours * 1.10m)
            {
                exceedsForecast = true;
                messages.Add($"Claimed hours ({totalActual:F1}) exceed monthly forecast ({forecastHours:F1}) by more than 10%");
            }
            else if (totalActual > forecastHours)
            {
                messages.Add($"Claimed hours ({totalActual:F1}) slightly exceed monthly forecast ({forecastHours:F1})");
            }
        }
        else
        {
            messages.Add("No forecast found for this resource in the claim month");
        }

        // Check 2: Weekly hours > 45
        if (claimEntity.ClaimedHours > 45)
        {
            messages.Add($"Weekly claimed hours ({claimEntity.ClaimedHours}) exceed the 45-hour weekly limit");
        }

        // Check 3: Project/Demand budget
        if (!string.IsNullOrWhiteSpace(claimEntity.ClaimCode))
        {
            var project = await _projectRepo.GetByCodeAsync(claimEntity.ClaimCode);
            if (project is not null)
            {
                var consumed = await _ilcRepo.GetProjectConsumedHoursAsync(project.Id);
                if (consumed + claimEntity.ClaimedHours > project.TotalBudgetHours)
                {
                    exceedsProjectBudget = true;
                    messages.Add($"Claim would exceed project budget: {project.ProjectName} ({consumed:F0}/{project.TotalBudgetHours:F0} hrs used)");
                }
            }
        }

        // Determine status
        string status = exceedsForecast || exceedsProjectBudget ? "Invalid"
            : messages.Count > 0 ? "Warning"
            : "Valid";

        // Update claim
        claimEntity.ValidationStatus = status switch
        {
            "Valid" => IlcValidationStatus.Valid,
            "Warning" => IlcValidationStatus.Warning,
            _ => IlcValidationStatus.Invalid
        };
        claimEntity.ExceedsForecast = exceedsForecast;
        claimEntity.ExceedsProjectBudget = exceedsProjectBudget;
        claimEntity.ValidationMessages = string.Join("; ", messages);

        var variance = forecastHours > 0 ? claimEntity.ClaimedHours - forecastHours : 0;

        return new IlcClaimValidation(
            claimId,
            resource?.FullName ?? "Unknown",
            claimEntity.ClaimedHours,
            forecastHours,
            variance,
            exceedsForecast,
            exceedsProjectBudget,
            status,
            messages
        );
    }
}

/// <summary>Band mix calculator: uses band weightages to compute FTE-weighted band mix.</summary>
public class BandMixService : IBandMixService
{
    private static readonly Dictionary<string, decimal> BandWeightages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["4"] = 4.5m, ["Band 4"] = 4.5m,
        ["5"] = 5.0m, ["Band 5"] = 5.0m,
        ["6G"] = 5.5m, ["Band 6G"] = 5.5m,
        ["6A"] = 6.0m, ["Band 6A"] = 6.0m,
        ["6B"] = 6.5m, ["Band 6B"] = 6.5m,
        ["7A"] = 7.0m, ["Band 7A"] = 7.0m,
        ["7B"] = 7.5m, ["Band 7B"] = 7.5m,
        ["8"] = 8.0m, ["Band 8"] = 8.0m,
        ["9"] = 9.0m, ["Band 9"] = 9.0m,
        ["10"] = 10.0m, ["Band 10"] = 10.0m
    };

    private readonly IResourceRepository _resourceRepo;
    private readonly IBandMixRepository _bandMixRepo;

    public BandMixService(IResourceRepository resourceRepo, IBandMixRepository bandMixRepo)
    {
        _resourceRepo = resourceRepo;
        _bandMixRepo = bandMixRepo;
    }

    public async Task<IEnumerable<BandMixDto>> CalculateBandMixAsync(int year, int month)
    {
        var resources = (await _resourceRepo.GetAllAsync()).Where(r => r.IsActive).ToList();

        var bandGroups = resources
            .GroupBy(r => r.Band.Trim())
            .Select(g =>
            {
                var band = g.Key;
                var fte = g.Count();
                var weightage = BandWeightages.TryGetValue(band, out var w) ? w : 0m;
                var totalBandValue = weightage * fte;
                return (Band: band, Fte: fte, Weightage: weightage, TotalBandValue: totalBandValue);
            })
            .OrderBy(b => b.Band)
            .ToList();

        var grandTotal = bandGroups.Sum(b => b.TotalBandValue);
        var totalFte = bandGroups.Sum(b => b.Fte);

        var results = bandGroups.Select(b =>
        {
            var bandPct = totalFte > 0 ? (decimal)b.Fte / totalFte * 100 : 0;
            var bandMix = totalFte > 0 ? grandTotal / totalFte : 0;
            return new BandMixDto(b.Band, b.Weightage, b.Fte, b.TotalBandValue, bandPct, bandMix);
        }).ToList();

        // Persist
        await _bandMixRepo.DeleteByMonthAsync(year, month);
        await _bandMixRepo.AddRangeAsync(results.Select(r => new BandMixRecord
        {
            Year = year, Month = month,
            Band = r.Band, Weightage = r.Weightage,
            Fte = r.Fte, TotalBandValue = r.TotalBandValue,
            BandPercentage = r.BandPercentage, BandMix = r.BandMix
        }));

        return results;
    }

    public async Task<IEnumerable<BandMixDto>> CalculateBandMixForRangeAsync(int year, int startMonth, int endMonth)
    {
        var allResults = new List<BandMixDto>();
        for (int m = startMonth; m <= endMonth; m++)
            allResults.AddRange(await CalculateBandMixAsync(year, m));
        return allResults;
    }
}
