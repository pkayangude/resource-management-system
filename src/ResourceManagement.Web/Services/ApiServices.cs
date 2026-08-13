using System.Net.Http.Json;

namespace ResourceManagement.Web.Services;

// ─── DTOs (mirror from Core) ──────────────────────────────────────────────────

public record ResourceDto(int Id, string EmpId, string TalentId, string FullName,
    string Corporate, string Pcode, string Country, string Location, string Band,
    decimal CostRate, string? Manager, string? Team, string? Category, string? IppfCategory,
    string? JobRoleSkillSet, string? EmployeeType, string? IntranetId,
    string Status, DateTime? OnboardingDate, DateTime? OffboardingDate);

public record ForecastAllocationDto(int Id, int ResourceId, string ResourceName,
    string Band, int Year, int Month, decimal ForecastHours, decimal ForecastCost,
    decimal FteFraction, decimal? ActualHours, decimal? VarianceHours, string? Comments);

public record IlcClaimDto(int Id, int ResourceId, string ResourceName,
    DateTime WeekEndingDate, int Year, int WeekNumber, decimal ClaimedHours,
    string? ClaimCode, string? ProjectName, string? DemandCode,
    string ValidationStatus, string? ValidationMessages,
    bool ExceedsForecast, bool ExceedsProjectBudget);

public record LeaveRecordDto(int Id, int ResourceId, string ResourceName,
    string LeaveType, DateTime StartDate, DateTime EndDate, int TotalDays,
    string? Reason, string Status, string? ApprovedBy, decimal ForecastImpactHours);

public record ProjectDto(int Id, string ProjectCode, string ProjectName,
    string? ProjectDbId, string ProjectType, string? ClaimCode,
    DateTime StartDate, DateTime EndDate, decimal TotalBudgetHours,
    decimal ConsumedHours, decimal RemainingHours, bool IsOverBudget,
    string? Portfolio, string Status);

public record SkillMatrixDto(int Id, int ResourceId, string ResourceName,
    string SkillCategory, string SkillName, int ProficiencyLevel,
    string ProficiencyLabel, int YearsOfExperience, string? Certifications, string? Notes);

public record BandMixDto(string Band, decimal Weightage, int Fte,
    decimal TotalBandValue, decimal BandPercentage, decimal BandMix);

public record DashboardSummaryDto(int TotalActiveResources, int OnboardedThisMonth,
    int OffboardedThisMonth, int ResourcesOnLeave, decimal TotalForecastHoursCurrentMonth,
    decimal TotalActualHoursCurrentMonth, decimal UtilizationPercentage,
    int ProjectsAtRisk, int PendingIlcValidations);

public record ExcelPreviewResult(IEnumerable<string> Headers,
    IEnumerable<Dictionary<string, string?>> SampleRows, int TotalRows, string? ErrorMessage);

public record ImportResult(bool Success, int ImportedCount, int SkippedCount,
    int ErrorCount, IEnumerable<string> Errors, string? Summary);

// ─── Service Interfaces ───────────────────────────────────────────────────────

public interface IResourceApiService
{
    Task<List<ResourceDto>> GetAllAsync(bool includeOffboarded = false);
    Task<ResourceDto?> GetByIdAsync(int id);
    Task<ResourceDto?> CreateAsync(object dto);
    Task UpdateAsync(int id, object dto);
    Task OffboardAsync(int id, DateTime? date = null);
    Task<int> CountActiveAsync();
}

public interface IForecastApiService
{
    Task<List<ForecastAllocationDto>> GetByMonthAsync(int year, int month);
    Task<List<ForecastAllocationDto>> GetByResourceAsync(int resourceId);
    Task GenerateMonthlyForecastAsync(int year, int month);
}

public interface IIlcApiService
{
    Task<List<IlcClaimDto>> GetByResourceAsync(int resourceId);
    Task<List<IlcClaimDto>> GetByWeekAsync(DateTime weekEndingDate);
    Task<ExcelPreviewResult?> PreviewFileAsync(Stream fileStream, string fileName);
    Task<ImportResult?> UploadAsync(Stream fileStream, string fileName, Dictionary<string, string> mapping, string uploadedBy);
    Task ValidateBatchAsync(int batchId);
}

public interface ILeaveApiService
{
    Task<List<LeaveRecordDto>> GetByResourceAsync(int resourceId);
    Task<List<LeaveRecordDto>> GetActiveAsync();
    Task<LeaveRecordDto?> CreateAsync(object dto);
    Task ApproveAsync(int id, string approvedBy);
    Task CancelAsync(int id);
}

public interface IProjectApiService
{
    Task<List<ProjectDto>> GetAllAsync();
    Task<ProjectDto?> GetByIdAsync(int id);
    Task<ProjectDto?> CreateAsync(object dto);
    Task AllocateResourceAsync(int projectId, object dto);
}

public interface ISkillMatrixApiService
{
    Task<List<SkillMatrixDto>> GetByResourceAsync(int resourceId);
    Task<List<SkillMatrixDto>> GetBySkillAsync(string skillName);
    Task<SkillMatrixDto?> CreateAsync(object dto);
    Task DeleteAsync(int id);
}

public interface IBandMixApiService
{
    Task<List<BandMixDto>> GetBandMixAsync(int year, int month);
}

public interface IImportApiService
{
    Task<ExcelPreviewResult?> PreviewAsync(Stream stream, string fileName);
    Task<ImportResult?> ImportResourcesAsync(Stream stream, string fileName, Dictionary<string, string> mapping);
    Task<ImportResult?> ImportMovementsAsync(Stream stream, string fileName, Dictionary<string, string> mapping);
}

public interface IDashboardApiService
{
    Task<DashboardSummaryDto?> GetSummaryAsync();
}

// ─── Service Implementations ──────────────────────────────────────────────────

public class ResourceApiService : IResourceApiService
{
    private readonly HttpClient _http;
    public ResourceApiService(HttpClient http) => _http = http;

    public async Task<List<ResourceDto>> GetAllAsync(bool includeOffboarded = false)
    {
        var result = await _http.GetFromJsonAsync<List<ResourceDto>>(
            $"api/resources?includeOffboarded={includeOffboarded}");
        return result ?? [];
    }

    public Task<ResourceDto?> GetByIdAsync(int id) =>
        _http.GetFromJsonAsync<ResourceDto>($"api/resources/{id}");

    public async Task<ResourceDto?> CreateAsync(object dto)
    {
        var resp = await _http.PostAsJsonAsync("api/resources", dto);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ResourceDto>();
    }

    public async Task UpdateAsync(int id, object dto)
    {
        var resp = await _http.PutAsJsonAsync($"api/resources/{id}", dto);
        resp.EnsureSuccessStatusCode();
    }

    public async Task OffboardAsync(int id, DateTime? date = null)
    {
        var resp = await _http.PostAsJsonAsync($"api/resources/{id}/offboard", new { offboardingDate = date ?? DateTime.UtcNow });
        resp.EnsureSuccessStatusCode();
    }

    public async Task<int> CountActiveAsync()
    {
        var result = await _http.GetFromJsonAsync<int>("api/resources/count");
        return result;
    }
}

public class ForecastApiService : IForecastApiService
{
    private readonly HttpClient _http;
    public ForecastApiService(HttpClient http) => _http = http;

    public async Task<List<ForecastAllocationDto>> GetByMonthAsync(int year, int month)
    {
        var result = await _http.GetFromJsonAsync<List<ForecastAllocationDto>>($"api/forecast/{year}/{month}");
        return result ?? [];
    }

    public async Task<List<ForecastAllocationDto>> GetByResourceAsync(int resourceId)
    {
        var result = await _http.GetFromJsonAsync<List<ForecastAllocationDto>>($"api/forecast/resource/{resourceId}");
        return result ?? [];
    }

    public async Task GenerateMonthlyForecastAsync(int year, int month)
    {
        var resp = await _http.PostAsync($"api/forecast/generate/{year}/{month}", null);
        resp.EnsureSuccessStatusCode();
    }
}

public class IlcApiService : IIlcApiService
{
    private readonly HttpClient _http;
    public IlcApiService(HttpClient http) => _http = http;

    public async Task<List<IlcClaimDto>> GetByResourceAsync(int resourceId)
    {
        var result = await _http.GetFromJsonAsync<List<IlcClaimDto>>($"api/ilc/resource/{resourceId}");
        return result ?? [];
    }

    public async Task<List<IlcClaimDto>> GetByWeekAsync(DateTime weekEndingDate)
    {
        var result = await _http.GetFromJsonAsync<List<IlcClaimDto>>(
            $"api/ilc/week?weekEndingDate={weekEndingDate:yyyy-MM-dd}");
        return result ?? [];
    }

    public async Task<ExcelPreviewResult?> PreviewFileAsync(Stream fileStream, string fileName)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", fileName);
        var resp = await _http.PostAsync("api/ilc/preview", content);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ExcelPreviewResult>();
    }

    public async Task<ImportResult?> UploadAsync(Stream fileStream, string fileName,
        Dictionary<string, string> mapping, string uploadedBy)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", fileName);
        content.Add(new StringContent(System.Text.Json.JsonSerializer.Serialize(mapping)), "columnMappingJson");
        content.Add(new StringContent(uploadedBy), "uploadedBy");
        var resp = await _http.PostAsync("api/ilc/upload", content);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ImportResult>();
    }

    public async Task ValidateBatchAsync(int batchId)
    {
        var resp = await _http.PostAsync($"api/ilc/validate/{batchId}", null);
        resp.EnsureSuccessStatusCode();
    }
}

public class LeaveApiService : ILeaveApiService
{
    private readonly HttpClient _http;
    public LeaveApiService(HttpClient http) => _http = http;

    public async Task<List<LeaveRecordDto>> GetByResourceAsync(int resourceId)
    {
        var result = await _http.GetFromJsonAsync<List<LeaveRecordDto>>($"api/leave/resource/{resourceId}");
        return result ?? [];
    }

    public async Task<List<LeaveRecordDto>> GetActiveAsync()
    {
        var result = await _http.GetFromJsonAsync<List<LeaveRecordDto>>("api/leave/active");
        return result ?? [];
    }

    public async Task<LeaveRecordDto?> CreateAsync(object dto)
    {
        var resp = await _http.PostAsJsonAsync("api/leave", dto);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<LeaveRecordDto>();
    }

    public async Task ApproveAsync(int id, string approvedBy)
    {
        var resp = await _http.PostAsync($"api/leave/{id}/approve?approvedBy={approvedBy}", null);
        resp.EnsureSuccessStatusCode();
    }

    public async Task CancelAsync(int id)
    {
        var resp = await _http.PostAsync($"api/leave/{id}/cancel", null);
        resp.EnsureSuccessStatusCode();
    }
}

public class ProjectApiService : IProjectApiService
{
    private readonly HttpClient _http;
    public ProjectApiService(HttpClient http) => _http = http;

    public async Task<List<ProjectDto>> GetAllAsync()
    {
        var result = await _http.GetFromJsonAsync<List<ProjectDto>>("api/projects");
        return result ?? [];
    }

    public Task<ProjectDto?> GetByIdAsync(int id) =>
        _http.GetFromJsonAsync<ProjectDto>($"api/projects/{id}");

    public async Task<ProjectDto?> CreateAsync(object dto)
    {
        var resp = await _http.PostAsJsonAsync("api/projects", dto);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ProjectDto>();
    }

    public async Task AllocateResourceAsync(int projectId, object dto)
    {
        var resp = await _http.PostAsJsonAsync($"api/projects/{projectId}/allocate", dto);
        resp.EnsureSuccessStatusCode();
    }
}

public class SkillMatrixApiService : ISkillMatrixApiService
{
    private readonly HttpClient _http;
    public SkillMatrixApiService(HttpClient http) => _http = http;

    public async Task<List<SkillMatrixDto>> GetByResourceAsync(int resourceId)
    {
        var result = await _http.GetFromJsonAsync<List<SkillMatrixDto>>($"api/skillmatrix/resource/{resourceId}");
        return result ?? [];
    }

    public async Task<List<SkillMatrixDto>> GetBySkillAsync(string skillName)
    {
        var result = await _http.GetFromJsonAsync<List<SkillMatrixDto>>($"api/skillmatrix/skill/{Uri.EscapeDataString(skillName)}");
        return result ?? [];
    }

    public async Task<SkillMatrixDto?> CreateAsync(object dto)
    {
        var resp = await _http.PostAsJsonAsync("api/skillmatrix", dto);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<SkillMatrixDto>();
    }

    public async Task DeleteAsync(int id)
    {
        var resp = await _http.DeleteAsync($"api/skillmatrix/{id}");
        resp.EnsureSuccessStatusCode();
    }
}

public class BandMixApiService : IBandMixApiService
{
    private readonly HttpClient _http;
    public BandMixApiService(HttpClient http) => _http = http;

    public async Task<List<BandMixDto>> GetBandMixAsync(int year, int month)
    {
        var result = await _http.GetFromJsonAsync<List<BandMixDto>>($"api/bandmix/{year}/{month}");
        return result ?? [];
    }
}

public class ImportApiService : IImportApiService
{
    private readonly HttpClient _http;
    public ImportApiService(HttpClient http) => _http = http;

    public async Task<ExcelPreviewResult?> PreviewAsync(Stream stream, string fileName)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", fileName);
        var resp = await _http.PostAsync("api/import/preview", content);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ExcelPreviewResult>();
    }

    public async Task<ImportResult?> ImportResourcesAsync(Stream stream, string fileName, Dictionary<string, string> mapping)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", fileName);
        content.Add(new StringContent(System.Text.Json.JsonSerializer.Serialize(mapping)), "columnMappingJson");
        var resp = await _http.PostAsync("api/import/resources", content);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ImportResult>();
    }

    public async Task<ImportResult?> ImportMovementsAsync(Stream stream, string fileName, Dictionary<string, string> mapping)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", fileName);
        content.Add(new StringContent(System.Text.Json.JsonSerializer.Serialize(mapping)), "columnMappingJson");
        var resp = await _http.PostAsync("api/import/movements", content);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ImportResult>();
    }
}

public class DashboardApiService : IDashboardApiService
{
    private readonly HttpClient _http;
    public DashboardApiService(HttpClient http) => _http = http;

    public Task<DashboardSummaryDto?> GetSummaryAsync() =>
        _http.GetFromJsonAsync<DashboardSummaryDto>("api/dashboard/summary");
}
