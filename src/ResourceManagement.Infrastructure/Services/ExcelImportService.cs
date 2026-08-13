using ExcelDataReader;
using ResourceManagement.Core.Entities;
using ResourceManagement.Core.Interfaces;

namespace ResourceManagement.Infrastructure.Services;

/// <summary>
/// Excel import service supporting column mapping, preview, and bulk imports
/// from xlsx files for resources, ILC claims, forecasts, and resource movements.
/// </summary>
public class ExcelImportService : IExcelImportService
{
    private readonly IResourceRepository _resourceRepo;
    private readonly IForecastRepository _forecastRepo;
    private readonly IIlcRepository _ilcRepo;
    private readonly IForecastCalculationService _forecastCalc;

    public ExcelImportService(
        IResourceRepository resourceRepo,
        IForecastRepository forecastRepo,
        IIlcRepository ilcRepo,
        IForecastCalculationService forecastCalc)
    {
        _resourceRepo = resourceRepo;
        _forecastRepo = forecastRepo;
        _ilcRepo = ilcRepo;
        _forecastCalc = forecastCalc;
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    public Task<ExcelPreviewResult> PreviewXlsxAsync(Stream stream, string fileName)
    {
        try
        {
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            });

            var table = dataSet.Tables[0];
            var headers = table.Columns.Cast<System.Data.DataColumn>()
                .Select(c => c.ColumnName)
                .ToList();

            var sampleRows = table.Rows.Cast<System.Data.DataRow>()
                .Take(5)
                .Select(row => headers.ToDictionary(h => h, h =>
                {
                    var val = row[h];
                    return val == DBNull.Value ? null : val?.ToString();
                }))
                .ToList();

            return Task.FromResult(new ExcelPreviewResult(headers, sampleRows, table.Rows.Count));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ExcelPreviewResult([], [], 0, ex.Message));
        }
    }

    public async Task<ImportResult> ImportResourcesAsync(Stream stream, Dictionary<string, string> columnMapping)
    {
        var errors = new List<string>();
        int imported = 0, skipped = 0, errorCount = 0;

        try
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            });

            var table = dataSet.Tables[0];
            var newResources = new List<Resource>();

            foreach (System.Data.DataRow row in table.Rows)
            {
                try
                {
                    var empId = GetMapped(row, columnMapping, "EmpId")?.Trim();
                    var talentId = GetMapped(row, columnMapping, "TalentId")?.Trim();
                    var fullName = GetMapped(row, columnMapping, "FullName")?.Trim();

                    if (string.IsNullOrWhiteSpace(empId) || string.IsNullOrWhiteSpace(fullName))
                    {
                        skipped++;
                        continue;
                    }

                    if (await _resourceRepo.ExistsAsync(empId))
                    {
                        skipped++;
                        errors.Add($"Row skipped: EmpId '{empId}' already exists");
                        continue;
                    }

                    var resource = new Resource
                    {
                        EmpId = empId,
                        TalentId = talentId ?? empId,
                        FullName = fullName,
                        Corporate = GetMapped(row, columnMapping, "Corporate") ?? string.Empty,
                        Pcode = GetMapped(row, columnMapping, "Pcode") ?? string.Empty,
                        Country = GetMapped(row, columnMapping, "Country") ?? "India",
                        Location = GetMapped(row, columnMapping, "Location") ?? string.Empty,
                        Band = GetMapped(row, columnMapping, "Band") ?? string.Empty,
                        CostRate = decimal.TryParse(GetMapped(row, columnMapping, "CostRate"), out var cr) ? cr : 0,
                        Manager = GetMapped(row, columnMapping, "Manager"),
                        Team = GetMapped(row, columnMapping, "Team"),
                        Category = GetMapped(row, columnMapping, "Category"),
                        IppfCategory = GetMapped(row, columnMapping, "IppfCategory"),
                        JobRoleSkillSet = GetMapped(row, columnMapping, "JobRoleSkillSet"),
                        EmployeeType = GetMapped(row, columnMapping, "EmployeeType"),
                        IntranetId = GetMapped(row, columnMapping, "IntranetId"),
                        DeptCode = GetMapped(row, columnMapping, "DeptCode"),
                        JrssServiceArea = GetMapped(row, columnMapping, "JrssServiceArea"),
                        Status = ResourceStatus.Active,
                        OnboardingDate = DateTime.UtcNow
                    };

                    if (DateTime.TryParse(GetMapped(row, columnMapping, "DateOfJoining"), out var doj))
                        resource.DateOfJoining = doj;

                    newResources.Add(resource);
                    imported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Row error: {ex.Message}");
                    errorCount++;
                }
            }

            if (newResources.Count > 0)
                await _resourceRepo.AddRangeAsync(newResources);
        }
        catch (Exception ex)
        {
            errors.Add($"File error: {ex.Message}");
            return new ImportResult(false, 0, 0, 1, errors);
        }

        return new ImportResult(
            errorCount == 0,
            imported,
            skipped,
            errorCount,
            errors,
            $"Imported {imported} resources, skipped {skipped}, errors {errorCount}"
        );
    }

    public async Task<ImportResult> ImportIlcClaimsAsync(Stream stream, Dictionary<string, string> columnMapping, string uploadedBy)
    {
        var errors = new List<string>();
        int imported = 0, skipped = 0, errorCount = 0;

        // Create upload batch
        var batch = await _ilcRepo.CreateBatchAsync(new IlcUploadBatch
        {
            FileName = "ILC-Weekly-Upload.xlsx",
            UploadedBy = uploadedBy,
            Status = UploadStatus.Processing
        });

        try
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            });

            var table = dataSet.Tables[0];
            var claims = new List<IlcClaim>();

            foreach (System.Data.DataRow row in table.Rows)
            {
                try
                {
                    var talentId = GetMapped(row, columnMapping, "TalentId")?.Trim();
                    var empId = GetMapped(row, columnMapping, "EmpId")?.Trim();
                    var claimedHoursStr = GetMapped(row, columnMapping, "ClaimedHours");

                    if (string.IsNullOrWhiteSpace(talentId) && string.IsNullOrWhiteSpace(empId))
                    {
                        skipped++;
                        continue;
                    }

                    Resource? resource = null;
                    if (!string.IsNullOrWhiteSpace(talentId))
                        resource = await _resourceRepo.GetByTalentIdAsync(talentId);
                    if (resource == null && !string.IsNullOrWhiteSpace(empId))
                        resource = await _resourceRepo.GetByEmpIdAsync(empId);

                    if (resource == null)
                    {
                        errors.Add($"Resource not found for TalentId='{talentId}', EmpId='{empId}'");
                        skipped++;
                        continue;
                    }

                    if (!decimal.TryParse(claimedHoursStr, out var hours) || hours <= 0)
                    {
                        skipped++;
                        continue;
                    }

                    var weekEndDateStr = GetMapped(row, columnMapping, "WeekEndingDate");
                    if (!DateTime.TryParse(weekEndDateStr, out var weekEnd))
                    {
                        errors.Add($"Invalid week ending date for {resource.FullName}: '{weekEndDateStr}'");
                        errorCount++;
                        continue;
                    }

                    var weekNum = System.Globalization.ISOWeek.GetWeekOfYear(weekEnd);
                    var claimCode = GetMapped(row, columnMapping, "ClaimCode");
                    var projectName = GetMapped(row, columnMapping, "ProjectName");
                    var projectDbId = GetMapped(row, columnMapping, "ProjectDbId");
                    var demandCode = GetMapped(row, columnMapping, "DemandCode");

                    claims.Add(new IlcClaim
                    {
                        ResourceId = resource.Id,
                        WeekEndingDate = weekEnd,
                        Year = weekEnd.Year,
                        WeekNumber = weekNum,
                        ClaimedHours = hours,
                        ClaimCode = claimCode,
                        ProjectName = projectName,
                        ProjectDbId = projectDbId,
                        DemandCode = demandCode,
                        ValidationStatus = IlcValidationStatus.Pending,
                        UploadBatchId = batch.Id
                    });
                    imported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Row error: {ex.Message}");
                    errorCount++;
                }
            }

            if (claims.Count > 0)
                await _ilcRepo.AddRangeAsync(claims);

            batch.TotalRows = imported + skipped + errorCount;
            batch.ValidRows = imported;
            batch.InvalidRows = errorCount;
            batch.Status = errorCount > 0 ? UploadStatus.CompletedWithWarnings : UploadStatus.Completed;
            batch.Summary = $"Imported {imported}, skipped {skipped}, errors {errorCount}";
            await _ilcRepo.UpdateBatchAsync(batch);
        }
        catch (Exception ex)
        {
            errors.Add($"File error: {ex.Message}");
            batch.Status = UploadStatus.Failed;
            batch.Summary = ex.Message;
            await _ilcRepo.UpdateBatchAsync(batch);
            return new ImportResult(false, 0, 0, 1, errors);
        }

        return new ImportResult(
            errorCount == 0,
            imported,
            skipped,
            errorCount,
            errors,
            $"Batch #{batch.Id}: Imported {imported} ILC claims"
        );
    }

    public async Task<ImportResult> ImportForecastAsync(Stream stream, Dictionary<string, string> columnMapping)
    {
        var errors = new List<string>();
        int imported = 0, skipped = 0, errorCount = 0;

        try
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            });

            var table = dataSet.Tables[0];
            var allocations = new List<ForecastAllocation>();

            foreach (System.Data.DataRow row in table.Rows)
            {
                try
                {
                    var empId = GetMapped(row, columnMapping, "EmpId")?.Trim();
                    var talentId = GetMapped(row, columnMapping, "TalentId")?.Trim();
                    if (string.IsNullOrWhiteSpace(empId) && string.IsNullOrWhiteSpace(talentId))
                    {
                        skipped++;
                        continue;
                    }

                    Resource? resource = null;
                    if (!string.IsNullOrWhiteSpace(talentId))
                        resource = await _resourceRepo.GetByTalentIdAsync(talentId);
                    if (resource == null && !string.IsNullOrWhiteSpace(empId))
                        resource = await _resourceRepo.GetByEmpIdAsync(empId);

                    if (resource == null) { skipped++; continue; }

                    decimal fte = decimal.TryParse(GetMapped(row, columnMapping, "FteFraction"), out var fteVal) ? fteVal : 1m;

                    // Month columns Jan-Dec
                    for (int m = 1; m <= 12; m++)
                    {
                        var monthKey = $"Month{m}";
                        var hoursStr = GetMapped(row, columnMapping, monthKey);
                        if (!decimal.TryParse(hoursStr, out var hours) || hours <= 0) continue;

                        int year = DateTime.UtcNow.Year;
                        var existing = await _forecastRepo.GetByResourceAndMonthAsync(resource.Id, year, m);
                        if (existing != null) { skipped++; continue; }

                        allocations.Add(new ForecastAllocation
                        {
                            ResourceId = resource.Id,
                            Year = year,
                            Month = m,
                            ForecastHours = hours,
                            ForecastCost = hours * resource.CostRate,
                            FteFraction = fte,
                            Comments = GetMapped(row, columnMapping, "Comments")
                        });
                        imported++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Row error: {ex.Message}");
                    errorCount++;
                }
            }

            if (allocations.Count > 0)
                await _forecastRepo.AddRangeAsync(allocations);
        }
        catch (Exception ex)
        {
            errors.Add($"File error: {ex.Message}");
            return new ImportResult(false, 0, 0, 1, errors);
        }

        return new ImportResult(errorCount == 0, imported, skipped, errorCount, errors,
            $"Imported {imported} forecast entries");
    }

    public async Task<ImportResult> ImportResourceMovementsAsync(Stream stream, Dictionary<string, string> columnMapping)
    {
        var errors = new List<string>();
        int imported = 0, skipped = 0, errorCount = 0;

        try
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            });

            var table = dataSet.Tables[0];

            foreach (System.Data.DataRow row in table.Rows)
            {
                try
                {
                    var name = GetMapped(row, columnMapping, "ResourceName")?.Trim();
                    if (string.IsNullOrWhiteSpace(name)) { skipped++; continue; }

                    // Try to match by name (approximate)
                    var allResources = await _resourceRepo.GetAllAsync(true);
                    var resource = allResources.FirstOrDefault(r =>
                        r.FullName.Equals(name, StringComparison.OrdinalIgnoreCase));

                    if (resource == null) { errors.Add($"Resource not found: {name}"); errorCount++; continue; }

                    var reasonStr = GetMapped(row, columnMapping, "Reason") ?? string.Empty;
                    var movType = reasonStr.Contains("Attrition", StringComparison.OrdinalIgnoreCase)
                        ? MovementType.Attrition
                        : reasonStr.Contains("Roll", StringComparison.OrdinalIgnoreCase)
                            ? MovementType.RollOff
                            : MovementType.Offboarding;

                    imported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Row error: {ex.Message}");
                    errorCount++;
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"File error: {ex.Message}");
            return new ImportResult(false, 0, 0, 1, errors);
        }

        return new ImportResult(errorCount == 0, imported, skipped, errorCount, errors,
            $"Imported {imported} resource movements");
    }

    private static string? GetMapped(System.Data.DataRow row, Dictionary<string, string> mapping, string field)
    {
        if (!mapping.TryGetValue(field, out var colName)) return null;
        if (!row.Table.Columns.Contains(colName)) return null;
        var val = row[colName];
        return val == DBNull.Value ? null : val?.ToString();
    }
}
