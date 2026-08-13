using ResourceManagement.Core.Entities;

namespace ResourceManagement.Core.Interfaces;

public interface IResourceRepository
{
    Task<IEnumerable<Resource>> GetAllAsync(bool includeOffboarded = false);
    Task<Resource?> GetByIdAsync(int id);
    Task<Resource?> GetByEmpIdAsync(string empId);
    Task<Resource?> GetByTalentIdAsync(string talentId);
    Task<IEnumerable<Resource>> GetByTeamAsync(string team);
    Task<IEnumerable<Resource>> GetByManagerAsync(string manager);
    Task<Resource> AddAsync(Resource resource);
    Task<IEnumerable<Resource>> AddRangeAsync(IEnumerable<Resource> resources);
    Task UpdateAsync(Resource resource);
    Task<bool> ExistsAsync(string empId);
    Task<int> CountActiveAsync();
}

public interface IForecastRepository
{
    Task<IEnumerable<ForecastAllocation>> GetByResourceAsync(int resourceId);
    Task<IEnumerable<ForecastAllocation>> GetByMonthAsync(int year, int month);
    Task<ForecastAllocation?> GetByResourceAndMonthAsync(int resourceId, int year, int month);
    Task<ForecastAllocation> AddAsync(ForecastAllocation allocation);
    Task<IEnumerable<ForecastAllocation>> AddRangeAsync(IEnumerable<ForecastAllocation> allocations);
    Task UpdateAsync(ForecastAllocation allocation);
    Task UpdateActualHoursAsync(int resourceId, int year, int month, decimal actualHours);
}

public interface IIlcRepository
{
    Task<IEnumerable<IlcClaim>> GetByResourceAsync(int resourceId);
    Task<IEnumerable<IlcClaim>> GetByWeekAsync(DateTime weekEndingDate);
    Task<IEnumerable<IlcClaim>> GetByBatchAsync(int batchId);
    Task<decimal> GetTotalClaimedHoursAsync(int resourceId, int year, int month);
    Task<decimal> GetProjectConsumedHoursAsync(int projectId);
    Task<IlcClaim> AddAsync(IlcClaim claim);
    Task<IEnumerable<IlcClaim>> AddRangeAsync(IEnumerable<IlcClaim> claims);
    Task<IlcUploadBatch> CreateBatchAsync(IlcUploadBatch batch);
    Task UpdateBatchAsync(IlcUploadBatch batch);
}

public interface IProjectRepository
{
    Task<IEnumerable<Project>> GetAllAsync();
    Task<Project?> GetByIdAsync(int id);
    Task<Project?> GetByCodeAsync(string projectCode);
    Task<Project> AddAsync(Project project);
    Task UpdateAsync(Project project);
    Task<IEnumerable<ProjectAllocation>> GetAllocationsByResourceAsync(int resourceId);
    Task<ProjectAllocation> AddAllocationAsync(ProjectAllocation allocation);
    Task UpdateAllocationAsync(ProjectAllocation allocation);
    Task UpdateConsumedHoursAsync(int projectId, decimal additionalHours);
}

public interface ILeaveRepository
{
    Task<IEnumerable<LeaveRecord>> GetByResourceAsync(int resourceId);
    Task<IEnumerable<LeaveRecord>> GetActiveAsync(DateTime asOfDate);
    Task<LeaveRecord> AddAsync(LeaveRecord leave);
    Task UpdateAsync(LeaveRecord leave);
    Task<bool> HasOverlappingLeaveAsync(int resourceId, DateTime start, DateTime end, int? excludeId = null);
}

public interface IHolidayRepository
{
    Task<IEnumerable<Holiday>> GetByYearAsync(int year);
    Task<IEnumerable<Holiday>> GetByLocationAsync(int year, string location);
    Task<int> GetWorkingDaysAsync(int year, int month, string location);
    Task<Holiday> AddAsync(Holiday holiday);
    Task<IEnumerable<Holiday>> AddRangeAsync(IEnumerable<Holiday> holidays);
}

public interface ISkillMatrixRepository
{
    Task<IEnumerable<SkillMatrix>> GetByResourceAsync(int resourceId);
    Task<IEnumerable<SkillMatrix>> GetBySkillAsync(string skillName);
    Task<SkillMatrix> AddAsync(SkillMatrix skill);
    Task UpdateAsync(SkillMatrix skill);
    Task DeleteAsync(int id);
}

public interface IBandMixRepository
{
    Task<IEnumerable<BandMixRecord>> GetByMonthAsync(int year, int month);
    Task AddRangeAsync(IEnumerable<BandMixRecord> records);
    Task DeleteByMonthAsync(int year, int month);
}
