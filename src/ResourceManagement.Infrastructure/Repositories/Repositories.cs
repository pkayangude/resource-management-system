using Microsoft.EntityFrameworkCore;
using ResourceManagement.Core.Entities;
using ResourceManagement.Core.Interfaces;
using ResourceManagement.Infrastructure.Data;

namespace ResourceManagement.Infrastructure.Repositories;

public class ResourceRepository : IResourceRepository
{
    private readonly ResourceManagementDbContext _db;

    public ResourceRepository(ResourceManagementDbContext db) => _db = db;

    public async Task<IEnumerable<Resource>> GetAllAsync(bool includeOffboarded = false)
    {
        var q = _db.Resources.AsQueryable();
        if (!includeOffboarded)
            q = q.Where(r => r.Status != ResourceStatus.Offboarded);
        return await q.OrderBy(r => r.FullName).ToListAsync();
    }

    public async Task<Resource?> GetByIdAsync(int id) =>
        await _db.Resources
            .Include(r => r.ForecastAllocations)
            .Include(r => r.ProjectAllocations).ThenInclude(pa => pa.Project)
            .Include(r => r.SkillMatrices)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<Resource?> GetByEmpIdAsync(string empId) =>
        await _db.Resources.FirstOrDefaultAsync(r => r.EmpId == empId);

    public async Task<Resource?> GetByTalentIdAsync(string talentId) =>
        await _db.Resources.FirstOrDefaultAsync(r => r.TalentId == talentId);

    public async Task<IEnumerable<Resource>> GetByTeamAsync(string team) =>
        await _db.Resources.Where(r => r.Team == team && r.Status == ResourceStatus.Active).ToListAsync();

    public async Task<IEnumerable<Resource>> GetByManagerAsync(string manager) =>
        await _db.Resources.Where(r => r.Manager == manager && r.Status == ResourceStatus.Active).ToListAsync();

    public async Task<Resource> AddAsync(Resource resource)
    {
        _db.Resources.Add(resource);
        await _db.SaveChangesAsync();
        return resource;
    }

    public async Task<IEnumerable<Resource>> AddRangeAsync(IEnumerable<Resource> resources)
    {
        var list = resources.ToList();
        _db.Resources.AddRange(list);
        await _db.SaveChangesAsync();
        return list;
    }

    public async Task UpdateAsync(Resource resource)
    {
        resource.UpdatedAt = DateTime.UtcNow;
        _db.Resources.Update(resource);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string empId) =>
        await _db.Resources.AnyAsync(r => r.EmpId == empId);

    public async Task<int> CountActiveAsync() =>
        await _db.Resources.CountAsync(r => r.Status == ResourceStatus.Active);
}

public class ForecastRepository : IForecastRepository
{
    private readonly ResourceManagementDbContext _db;

    public ForecastRepository(ResourceManagementDbContext db) => _db = db;

    public async Task<IEnumerable<ForecastAllocation>> GetByResourceAsync(int resourceId) =>
        await _db.ForecastAllocations.Where(f => f.ResourceId == resourceId).OrderBy(f => f.Year).ThenBy(f => f.Month).ToListAsync();

    public async Task<IEnumerable<ForecastAllocation>> GetByMonthAsync(int year, int month) =>
        await _db.ForecastAllocations
            .Include(f => f.Resource)
            .Where(f => f.Year == year && f.Month == month)
            .ToListAsync();

    public async Task<ForecastAllocation?> GetByResourceAndMonthAsync(int resourceId, int year, int month) =>
        await _db.ForecastAllocations.FirstOrDefaultAsync(f => f.ResourceId == resourceId && f.Year == year && f.Month == month);

    public async Task<ForecastAllocation> AddAsync(ForecastAllocation allocation)
    {
        _db.ForecastAllocations.Add(allocation);
        await _db.SaveChangesAsync();
        return allocation;
    }

    public async Task<IEnumerable<ForecastAllocation>> AddRangeAsync(IEnumerable<ForecastAllocation> allocations)
    {
        var list = allocations.ToList();
        _db.ForecastAllocations.AddRange(list);
        await _db.SaveChangesAsync();
        return list;
    }

    public async Task UpdateAsync(ForecastAllocation allocation)
    {
        allocation.UpdatedAt = DateTime.UtcNow;
        _db.ForecastAllocations.Update(allocation);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateActualHoursAsync(int resourceId, int year, int month, decimal actualHours)
    {
        var fa = await GetByResourceAndMonthAsync(resourceId, year, month);
        if (fa is not null)
        {
            fa.ActualHours = actualHours;
            await UpdateAsync(fa);
        }
    }
}

public class IlcRepository : IIlcRepository
{
    private readonly ResourceManagementDbContext _db;

    public IlcRepository(ResourceManagementDbContext db) => _db = db;

    public async Task<IEnumerable<IlcClaim>> GetByResourceAsync(int resourceId) =>
        await _db.IlcClaims.Where(c => c.ResourceId == resourceId).OrderByDescending(c => c.WeekEndingDate).ToListAsync();

    public async Task<IEnumerable<IlcClaim>> GetByWeekAsync(DateTime weekEndingDate) =>
        await _db.IlcClaims.Include(c => c.Resource).Where(c => c.WeekEndingDate.Date == weekEndingDate.Date).ToListAsync();

    public async Task<IEnumerable<IlcClaim>> GetByBatchAsync(int batchId) =>
        await _db.IlcClaims.Include(c => c.Resource).Where(c => c.UploadBatchId == batchId).ToListAsync();

    public async Task<decimal> GetTotalClaimedHoursAsync(int resourceId, int year, int month)
    {
        return await _db.IlcClaims
            .Where(c => c.ResourceId == resourceId && c.Year == year && c.WeekEndingDate.Month == month)
            .SumAsync(c => c.ClaimedHours);
    }

    public async Task<decimal> GetProjectConsumedHoursAsync(int projectId)
    {
        var project = await _db.Projects.FindAsync(projectId);
        if (project == null) return 0;
        return await _db.IlcClaims
            .Where(c => c.ClaimCode == project.ClaimCode || c.ProjectDbId == project.ProjectDbId)
            .SumAsync(c => c.ClaimedHours);
    }

    public async Task<IlcClaim> AddAsync(IlcClaim claim)
    {
        _db.IlcClaims.Add(claim);
        await _db.SaveChangesAsync();
        return claim;
    }

    public async Task<IEnumerable<IlcClaim>> AddRangeAsync(IEnumerable<IlcClaim> claims)
    {
        var list = claims.ToList();
        _db.IlcClaims.AddRange(list);
        await _db.SaveChangesAsync();
        return list;
    }

    public async Task<IlcUploadBatch> CreateBatchAsync(IlcUploadBatch batch)
    {
        _db.IlcUploadBatches.Add(batch);
        await _db.SaveChangesAsync();
        return batch;
    }

    public async Task UpdateBatchAsync(IlcUploadBatch batch)
    {
        _db.IlcUploadBatches.Update(batch);
        await _db.SaveChangesAsync();
    }
}

public class ProjectRepository : IProjectRepository
{
    private readonly ResourceManagementDbContext _db;

    public ProjectRepository(ResourceManagementDbContext db) => _db = db;

    public async Task<IEnumerable<Project>> GetAllAsync() =>
        await _db.Projects.OrderBy(p => p.ProjectName).ToListAsync();

    public async Task<Project?> GetByIdAsync(int id) =>
        await _db.Projects.Include(p => p.Allocations).ThenInclude(a => a.Resource).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Project?> GetByCodeAsync(string projectCode) =>
        await _db.Projects.FirstOrDefaultAsync(p => p.ProjectCode == projectCode);

    public async Task<Project> AddAsync(Project project)
    {
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return project;
    }

    public async Task UpdateAsync(Project project)
    {
        project.UpdatedAt = DateTime.UtcNow;
        _db.Projects.Update(project);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<ProjectAllocation>> GetAllocationsByResourceAsync(int resourceId) =>
        await _db.ProjectAllocations
            .Include(pa => pa.Project)
            .Where(pa => pa.ResourceId == resourceId)
            .ToListAsync();

    public async Task<ProjectAllocation> AddAllocationAsync(ProjectAllocation allocation)
    {
        _db.ProjectAllocations.Add(allocation);
        await _db.SaveChangesAsync();
        return allocation;
    }

    public async Task UpdateAllocationAsync(ProjectAllocation allocation)
    {
        allocation.UpdatedAt = DateTime.UtcNow;
        _db.ProjectAllocations.Update(allocation);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateConsumedHoursAsync(int projectId, decimal additionalHours)
    {
        var project = await _db.Projects.FindAsync(projectId);
        if (project is null) return;
        project.ConsumedHours += additionalHours;
        await UpdateAsync(project);
    }
}

public class LeaveRepository : ILeaveRepository
{
    private readonly ResourceManagementDbContext _db;

    public LeaveRepository(ResourceManagementDbContext db) => _db = db;

    public async Task<IEnumerable<LeaveRecord>> GetByResourceAsync(int resourceId) =>
        await _db.LeaveRecords.Where(l => l.ResourceId == resourceId).OrderByDescending(l => l.StartDate).ToListAsync();

    public async Task<IEnumerable<LeaveRecord>> GetActiveAsync(DateTime asOfDate) =>
        await _db.LeaveRecords.Include(l => l.Resource)
            .Where(l => l.Status == LeaveStatus.Approved && l.StartDate <= asOfDate && l.EndDate >= asOfDate)
            .ToListAsync();

    public async Task<LeaveRecord> AddAsync(LeaveRecord leave)
    {
        _db.LeaveRecords.Add(leave);
        await _db.SaveChangesAsync();
        return leave;
    }

    public async Task UpdateAsync(LeaveRecord leave)
    {
        leave.UpdatedAt = DateTime.UtcNow;
        _db.LeaveRecords.Update(leave);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> HasOverlappingLeaveAsync(int resourceId, DateTime start, DateTime end, int? excludeId = null)
    {
        var q = _db.LeaveRecords.Where(l =>
            l.ResourceId == resourceId &&
            l.Status != LeaveStatus.Cancelled &&
            l.Status != LeaveStatus.Rejected &&
            l.StartDate <= end && l.EndDate >= start);
        if (excludeId.HasValue)
            q = q.Where(l => l.Id != excludeId.Value);
        return await q.AnyAsync();
    }
}

public class HolidayRepository : IHolidayRepository
{
    private readonly ResourceManagementDbContext _db;

    public HolidayRepository(ResourceManagementDbContext db) => _db = db;

    public async Task<IEnumerable<Holiday>> GetByYearAsync(int year) =>
        await _db.Holidays.Where(h => h.Year == year).OrderBy(h => h.Date).ToListAsync();

    public async Task<IEnumerable<Holiday>> GetByLocationAsync(int year, string location) =>
        await _db.Holidays
            .Where(h => h.Year == year && (h.IsNational || h.Location.ToLower() == location.ToLower()))
            .OrderBy(h => h.Date)
            .ToListAsync();

    public async Task<int> GetWorkingDaysAsync(int year, int month, string location)
    {
        var holidays = await GetByLocationAsync(year, location);
        var holidayDates = holidays.Select(h => h.Date.Date).ToHashSet();

        int workingDays = 0;
        var daysInMonth = DateTime.DaysInMonth(year, month);
        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);
            if (date.DayOfWeek != DayOfWeek.Saturday &&
                date.DayOfWeek != DayOfWeek.Sunday &&
                !holidayDates.Contains(date.Date))
            {
                workingDays++;
            }
        }
        return workingDays;
    }

    public async Task<Holiday> AddAsync(Holiday holiday)
    {
        _db.Holidays.Add(holiday);
        await _db.SaveChangesAsync();
        return holiday;
    }

    public async Task<IEnumerable<Holiday>> AddRangeAsync(IEnumerable<Holiday> holidays)
    {
        var list = holidays.ToList();
        _db.Holidays.AddRange(list);
        await _db.SaveChangesAsync();
        return list;
    }
}

public class SkillMatrixRepository : ISkillMatrixRepository
{
    private readonly ResourceManagementDbContext _db;

    public SkillMatrixRepository(ResourceManagementDbContext db) => _db = db;

    public async Task<IEnumerable<SkillMatrix>> GetByResourceAsync(int resourceId) =>
        await _db.SkillMatrices.Where(s => s.ResourceId == resourceId).OrderBy(s => s.SkillCategory).ThenBy(s => s.SkillName).ToListAsync();

    public async Task<IEnumerable<SkillMatrix>> GetBySkillAsync(string skillName) =>
        await _db.SkillMatrices.Include(s => s.Resource).Where(s => s.SkillName.Contains(skillName)).ToListAsync();

    public async Task<SkillMatrix> AddAsync(SkillMatrix skill)
    {
        _db.SkillMatrices.Add(skill);
        await _db.SaveChangesAsync();
        return skill;
    }

    public async Task UpdateAsync(SkillMatrix skill)
    {
        skill.LastUpdated = DateTime.UtcNow;
        _db.SkillMatrices.Update(skill);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var skill = await _db.SkillMatrices.FindAsync(id);
        if (skill is not null)
        {
            _db.SkillMatrices.Remove(skill);
            await _db.SaveChangesAsync();
        }
    }
}

public class BandMixRepository : IBandMixRepository
{
    private readonly ResourceManagementDbContext _db;

    public BandMixRepository(ResourceManagementDbContext db) => _db = db;

    public async Task<IEnumerable<BandMixRecord>> GetByMonthAsync(int year, int month) =>
        await _db.BandMixRecords.Where(b => b.Year == year && b.Month == month).OrderBy(b => b.Band).ToListAsync();

    public async Task AddRangeAsync(IEnumerable<BandMixRecord> records)
    {
        _db.BandMixRecords.AddRange(records);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteByMonthAsync(int year, int month)
    {
        var existing = _db.BandMixRecords.Where(b => b.Year == year && b.Month == month);
        _db.BandMixRecords.RemoveRange(existing);
        await _db.SaveChangesAsync();
    }
}
