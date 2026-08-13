namespace ResourceManagement.Core.DTOs;

// ─── Resource DTOs ────────────────────────────────────────────────────────────

public record ResourceDto(
    int Id,
    string EmpId,
    string TalentId,
    string FullName,
    string Corporate,
    string Pcode,
    string Country,
    string Location,
    string Band,
    decimal CostRate,
    string? Manager,
    string? Team,
    string? Category,
    string? IppfCategory,
    string? JobRoleSkillSet,
    string? EmployeeType,
    string? IntranetId,
    string Status,
    DateTime? OnboardingDate,
    DateTime? OffboardingDate
);

public record CreateResourceDto(
    string EmpId,
    string TalentId,
    string FullName,
    string Corporate,
    string Pcode,
    string Country,
    string Location,
    string Band,
    decimal CostRate,
    string? Manager,
    string? Team,
    string? Category,
    string? IppfCategory,
    string? JobRoleSkillSet,
    string? EmployeeType,
    string? IntranetId,
    DateTime? DateOfJoining,
    DateTime? OnboardingDate
);

public record UpdateResourceDto(
    string FullName,
    string Country,
    string Location,
    string Band,
    decimal CostRate,
    string? Manager,
    string? Team,
    string? Category,
    string? JobRoleSkillSet,
    string? IntranetId
);

// ─── Forecast DTOs ────────────────────────────────────────────────────────────

public record ForecastAllocationDto(
    int Id,
    int ResourceId,
    string ResourceName,
    string Band,
    int Year,
    int Month,
    decimal ForecastHours,
    decimal ForecastCost,
    decimal FteFraction,
    decimal? ActualHours,
    decimal? VarianceHours,
    string? Comments
);

public record CreateForecastDto(
    int ResourceId,
    int Year,
    int Month,
    decimal FteFraction,
    string? Comments
);

// ─── ILC DTOs ─────────────────────────────────────────────────────────────────

public record IlcClaimDto(
    int Id,
    int ResourceId,
    string ResourceName,
    DateTime WeekEndingDate,
    int Year,
    int WeekNumber,
    decimal ClaimedHours,
    string? ClaimCode,
    string? ProjectName,
    string? DemandCode,
    string ValidationStatus,
    string? ValidationMessages,
    bool ExceedsForecast,
    bool ExceedsProjectBudget
);

// ─── Leave DTOs ───────────────────────────────────────────────────────────────

public record LeaveRecordDto(
    int Id,
    int ResourceId,
    string ResourceName,
    string LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    int TotalDays,
    string? Reason,
    string Status,
    string? ApprovedBy,
    decimal ForecastImpactHours
);

public record CreateLeaveDto(
    int ResourceId,
    string LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    string? Reason
);

// ─── Project DTOs ─────────────────────────────────────────────────────────────

public record ProjectDto(
    int Id,
    string ProjectCode,
    string ProjectName,
    string? ProjectDbId,
    string ProjectType,
    string? ClaimCode,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalBudgetHours,
    decimal ConsumedHours,
    decimal RemainingHours,
    bool IsOverBudget,
    string? Portfolio,
    string Status
);

public record CreateProjectDto(
    string ProjectCode,
    string ProjectName,
    string? ProjectDbId,
    string ProjectType,
    string? ClaimCode,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalBudgetHours,
    string? Portfolio,
    string? PemName
);

public record ProjectAllocationDto(
    int Id,
    int ResourceId,
    string ResourceName,
    int ProjectId,
    string ProjectName,
    DateTime AllocationStartDate,
    DateTime AllocationEndDate,
    decimal WeeklyHours,
    decimal BudgetedHours,
    decimal ConsumedHours,
    decimal RemainingHours,
    bool IsOverBudget,
    decimal FteFraction,
    string Status
);

// ─── Skill Matrix DTOs ────────────────────────────────────────────────────────

public record SkillMatrixDto(
    int Id,
    int ResourceId,
    string ResourceName,
    string SkillCategory,
    string SkillName,
    int ProficiencyLevel,
    string ProficiencyLabel,
    int YearsOfExperience,
    string? Certifications,
    string? Notes
);

public record CreateSkillMatrixDto(
    int ResourceId,
    string SkillCategory,
    string SkillName,
    int ProficiencyLevel,
    int YearsOfExperience,
    string? Certifications,
    string? Notes
);

// ─── Dashboard DTOs ───────────────────────────────────────────────────────────

public record DashboardSummaryDto(
    int TotalActiveResources,
    int OnboardedThisMonth,
    int OffboardedThisMonth,
    int ResourcesOnLeave,
    decimal TotalForecastHoursCurrentMonth,
    decimal TotalActualHoursCurrentMonth,
    decimal UtilizationPercentage,
    int ProjectsAtRisk,
    int PendingIlcValidations
);
