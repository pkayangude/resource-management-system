using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ResourceManagement.Core.Interfaces;
using ResourceManagement.Infrastructure.Data;
using ResourceManagement.Infrastructure.Repositories;
using ResourceManagement.Infrastructure.Services;

namespace ResourceManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<ResourceManagementDbContext>(options =>
            options.UseSqlServer(connectionString,
                sql => sql.MigrationsAssembly(typeof(ResourceManagementDbContext).Assembly.FullName)));

        // Repositories
        services.AddScoped<IResourceRepository, ResourceRepository>();
        services.AddScoped<IForecastRepository, ForecastRepository>();
        services.AddScoped<IIlcRepository, IlcRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ILeaveRepository, LeaveRepository>();
        services.AddScoped<IHolidayRepository, HolidayRepository>();
        services.AddScoped<ISkillMatrixRepository, SkillMatrixRepository>();
        services.AddScoped<IBandMixRepository, BandMixRepository>();

        // Services
        services.AddScoped<IForecastCalculationService, ForecastCalculationService>();
        services.AddScoped<IIlcValidationService, IlcValidationService>();
        services.AddScoped<IBandMixService, BandMixService>();
        services.AddScoped<IExcelImportService, ExcelImportService>();

        return services;
    }
}
