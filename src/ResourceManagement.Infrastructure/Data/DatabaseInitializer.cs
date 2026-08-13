using Microsoft.EntityFrameworkCore;
using ResourceManagement.Core.Entities;
using ResourceManagement.Infrastructure.Data;

namespace ResourceManagement.Infrastructure.Data;

/// <summary>Seeds reference/sample data on first run. Idempotent — re-runs are safe.</summary>
public static class DatabaseInitializer
{
    public static async Task SeedAsync(ResourceManagementDbContext db)
    {
        await db.Database.MigrateAsync();

        await SeedProjectsAsync(db);
        await SeedHolidaysAsync(db);
        await SeedResourcesAsync(db);
        await SeedSkillMatricesAsync(db);
        await SeedForecastAllocationsAsync(db);
    }

    // ─── Projects ────────────────────────────────────────────────────────────────
    private static async Task SeedProjectsAsync(ResourceManagementDbContext db)
    {
        if (await db.Projects.AnyAsync()) return;

        db.Projects.AddRange(
            new Project
            {
                ProjectCode      = "P6TYQ",
                ProjectName      = "Project Picasso AMS",
                ProjectDbId      = "IN-07-01422",
                ProjectType      = ProjectType.Standard,
                ClaimCode        = "P6TYQ",
                StartDate        = new DateTime(2019, 1, 1),
                EndDate          = new DateTime(2028, 6, 30),
                TotalBudgetHours = 500000,
                Portfolio        = "AMS",
                Status           = ProjectStatus.Active
            },
            new Project
            {
                ProjectCode      = "RARV6",
                ProjectName      = "Project Picasso Persistent BUD",
                ProjectDbId      = "IN-07-01423",
                ProjectType      = ProjectType.Standard,
                ClaimCode        = "RARV6",
                StartDate        = new DateTime(2026, 1, 1),
                EndDate          = new DateTime(2026, 12, 31),
                TotalBudgetHours = 80000,
                Portfolio        = "BUD Corporate",
                Status           = ProjectStatus.Active
            },
            new Project
            {
                ProjectCode      = "RARV5",
                ProjectName      = "Project Picasso Persistent Corporate",
                ProjectDbId      = "IN-07-01424",
                ProjectType      = ProjectType.Standard,
                ClaimCode        = "RARV5",
                StartDate        = new DateTime(2026, 1, 1),
                EndDate          = new DateTime(2026, 12, 31),
                TotalBudgetHours = 60000,
                Portfolio        = "BUD Persistent",
                Status           = ProjectStatus.Active
            },
            new Project
            {
                ProjectCode      = "DEMAND-2026-01",
                ProjectName      = "Cloud Migration Demand 2026",
                ProjectDbId      = "DEMAND-001",
                ProjectType      = ProjectType.MajorDemand,
                ClaimCode        = "DMND01",
                StartDate        = new DateTime(2026, 3, 1),
                EndDate          = new DateTime(2026, 9, 30),
                TotalBudgetHours = 12000,
                Portfolio        = "Cloud",
                Status           = ProjectStatus.Active
            }
        );
        await db.SaveChangesAsync();
    }

    // ─── Holidays (IBM India 2026 — from Holiday List 2026.xlsx) ─────────────────
    private static async Task SeedHolidaysAsync(ResourceManagementDbContext db)
    {
        if (await db.Holidays.AnyAsync()) return;

        var holidays = new List<(string Name, DateTime Date, string[] Locations)>
        {
            ("Pongal / Sankranti / Uttarayan",         new DateTime(2026, 1, 15),  ["BANGALORE","CHENNAI","HYDERABAD","VISAKHAPATNAM"]),
            ("Republic Day",                            new DateTime(2026, 1, 26),  ["BANGALORE","CHENNAI","HYDERABAD","KOLKATA","PUNE","NOIDA","GURGAON","AHMEDABAD","BHUBANESWAR","VISAKHAPATNAM","MYSORE"]),
            ("Holi",                                    new DateTime(2026, 3, 4),   ["GURGAON","NOIDA","KOLKATA","AHMEDABAD","BHUBANESWAR"]),
            ("Ugadi / Gudi Padava",                     new DateTime(2026, 3, 19),  ["BANGALORE","HYDERABAD","VISAKHAPATNAM","PUNE","MYSORE"]),
            ("Good Friday",                             new DateTime(2026, 4, 3),   ["BANGALORE","CHENNAI","KOLKATA","GURGAON","NOIDA","VISAKHAPATNAM","MYSORE"]),
            ("Tamil New Year",                          new DateTime(2026, 4, 14),  ["CHENNAI"]),
            ("Vishu",                                   new DateTime(2026, 4, 15),  ["KOCHI"]),
            ("May Day / Maharashtra Day",               new DateTime(2026, 5, 1),   ["BANGALORE","CHENNAI","HYDERABAD","KOLKATA","PUNE","BHUBANESWAR","VISAKHAPATNAM","MYSORE"]),
            ("Id-ul-Zuha (Bakrid)",                     new DateTime(2026, 5, 28),  ["BANGALORE","CHENNAI","HYDERABAD","KOLKATA","PUNE","NOIDA","GURGAON","AHMEDABAD","BHUBANESWAR","VISAKHAPATNAM","KOCHI","MYSORE"]),
            ("Telangana Formation Day",                 new DateTime(2026, 6, 2),   ["HYDERABAD"]),
            ("Rath Yathra",                             new DateTime(2026, 7, 16),  ["BHUBANESWAR"]),
            ("Onam Day 1",                              new DateTime(2026, 8, 25),  ["KOCHI"]),
            ("Onam Day 2",                              new DateTime(2026, 8, 26),  ["KOCHI"]),
            ("Raksha Bandhan",                          new DateTime(2026, 8, 28),  ["GURGAON","NOIDA","BHUBANESWAR"]),
            ("Janmashtami",                             new DateTime(2026, 9, 4),   ["GURGAON","NOIDA","AHMEDABAD","KOLKATA"]),
            ("Ganesh Chaturthi / Vinayaka Chaturthi",  new DateTime(2026, 9, 14),  ["BANGALORE","CHENNAI","HYDERABAD","GURGAON","NOIDA","PUNE","AHMEDABAD","BHUBANESWAR","VISAKHAPATNAM","MYSORE"]),
            ("Gandhi Jayanti",                          new DateTime(2026, 10, 2),  ["BANGALORE","CHENNAI","HYDERABAD","KOLKATA","PUNE","NOIDA","GURGAON","AHMEDABAD","BHUBANESWAR","VISAKHAPATNAM","KOCHI","MYSORE"]),
            ("Dussehra",                                new DateTime(2026, 10, 20), ["BANGALORE","CHENNAI","HYDERABAD","KOLKATA","PUNE","NOIDA","GURGAON","AHMEDABAD","BHUBANESWAR","VISAKHAPATNAM","KOCHI","MYSORE"]),
            ("Vikram Samvant New Year / Balipratipada", new DateTime(2026, 11, 10), ["PUNE","AHMEDABAD"]),
            ("Christmas",                               new DateTime(2026, 12, 25), ["BANGALORE","CHENNAI","HYDERABAD","KOLKATA","PUNE","NOIDA","GURGAON","AHMEDABAD","BHUBANESWAR","VISAKHAPATNAM","KOCHI","MYSORE"]),
        };

        foreach (var (name, date, locations) in holidays)
        {
            foreach (var loc in locations)
            {
                db.Holidays.Add(new Holiday
                {
                    HolidayName = name,
                    Date        = date,
                    Location    = loc,
                    Year        = 2026,
                    Country     = "India",
                    IsNational  = locations.Length >= 8
                });
            }
        }
        await db.SaveChangesAsync();
    }

    // ─── Resources (from PIR Report template — first 20 rows) ────────────────────
    private static async Task SeedResourcesAsync(ResourceManagementDbContext db)
    {
        if (await db.Resources.AnyAsync()) return;

        var rows = new[]
        {
            ("005KMO744","AJAY ROHIDEKAR","RF","BANGALORE","7B","RJE","SAP Forecast to Produce & Logistics","ajayrohidekar@ibm.com"),
            ("04699D744","ANOOJA GIRIJAKUMARI","RF","BANGALORE","7B","JP3","SAP Application Operations","anooja.mg@in.ibm.com"),
            ("SVRDMC744","Aachal Rajendra Kadam","CT","PUNE","04","JGF","Platform Engineering Services","aachal.rajendra.kadam@ibm.com"),
            ("08993B744","Abhay Mishal","RF","PUNE","09","JP3","SAP Application Operations","abmishal@in.ibm.com"),
            ("002XBU744","Abhishek Deulgaonkar","RF","PUNE","7B","JP3","SAP Application Operations","abhishek.deulgaonkar@ibm.com"),
            ("0034VP744","Abhishek Kumar","RF","KOLKATA","7A","JP3","Quality Engineering","abhishek.kumar66@ibm.com"),
            ("0685C9744","Abhishek Neogi","RF","GURGAON","7A","JOG","Digital Product Engineering","abhishekneogi@in.ibm.com"),
            ("004QN3744","Abilesh Kumar","RF","CHENNAI","08","RJE","SAP Business Networks","abilesh.kumar@ibm.com"),
            ("002N3W744","Alekhya Kanakala","RF","HYDERABAD","7A","RJE","SAP Technical Infrastructure","kanakala.alekhya1@ibm.com"),
            ("000XEE744","Anish Nayak","RF","KOLKATA","6B","JOG","IBM & Red Hat","anish.nayak@ibm.com"),
            ("002WBK744","Ankit Upadhyay","RF","PUNE","7B","RJE","SAP Record to Report & Controls","ankit.upadhyay1@ibm.com"),
            ("000OTZ744","Anusha Math","RF","HYDERABAD","6B","JOG","Microsoft","mathanusha@in.ibm.com"),
            ("002DC3744","Apurva Yadav","RF","PUNE","6B","JOG","Microsoft","apurva.yadav@ibm.com"),
            ("004B3N744","Aravind Venugopal","RF","CHENNAI","6B","JI4","ServiceNow Core","aravind.venugopal@ibm.com"),
            ("005HAQ744","Arijit Biswas","RF","KOLKATA","7B","JM3","Migration & Modernization","arijit.biswas3@ibm.com"),
            ("003KIV744","Arvind Shrivastava","RF","NOIDA","7B","J3M","AMS Automation","arvind.shrivastava@ibm.com"),
            ("0029IW744","Asha Jadhav","RF","HYDERABAD","6B","JGF","Hybrid Cloud Application Operations","asha.jadhav@ibm.com"),
            ("004K5O744","Ashi Gupta","RF","NOIDA","6B","JI4","ServiceNow Core","ashi.gupta@ibm.com"),
            ("005MZK744","Asutosh Kaushik","RF","BHUBANESWAR","7A","J7B","Microsoft","asutosh.kaushik@ibm.com"),
            ("AVS8SV744","Avinash Gautam","CT","BANGALORE","6B","RHB","Microsoft","avinash.gautam@ibm.com"),
        };

        foreach (var (tid, name, empType, loc, band, dept, jrss, email) in rows)
        {
            db.Resources.Add(new Resource
            {
                TalentId        = tid,
                EmpId           = tid,
                FullName        = name,
                EmployeeType    = empType,
                Location        = loc,
                Country         = "India",
                Band            = band,
                DeptCode        = dept,
                JrssServiceArea = jrss,
                IntranetId      = email,
                Corporate       = "IBM India",
                Team            = jrss,
                DateOfJoining   = DateTime.UtcNow.AddYears(-3),
                OnboardingDate  = DateTime.UtcNow.AddDays(-30),
                Status          = ResourceStatus.Active,
                CreatedBy       = "seed"
            });
        }
        await db.SaveChangesAsync();
    }

    // ─── Skill Matrices ───────────────────────────────────────────────────────────
    private static async Task SeedSkillMatricesAsync(ResourceManagementDbContext db)
    {
        if (await db.SkillMatrices.AnyAsync()) return;

        var resources = await db.Resources.Take(10).ToListAsync();
        var skills    = new[] { "SAP ABAP", ".NET", "Azure", "ServiceNow", "MuleSoft", "Python", "Java", "SQL Server" };
        var rng       = new Random(42);

        foreach (var res in resources)
        {
            foreach (var skill in skills.OrderBy(_ => rng.Next()).Take(rng.Next(2, 4)))
            {
                db.SkillMatrices.Add(new SkillMatrix
                {
                    ResourceId        = res.Id,
                    SkillName         = skill,
                    SkillCategory     = "Technical",
                    ProficiencyLevel  = rng.Next(1, 5),
                    YearsOfExperience = rng.Next(1, 8),
                    LastUpdated       = DateTime.UtcNow.AddMonths(-rng.Next(1, 12)),
                    UpdatedBy         = "seed"
                });
            }
        }
        await db.SaveChangesAsync();
    }

    // ─── Forecast Allocations (Jun–Aug 2026 sample) ───────────────────────────────
    private static async Task SeedForecastAllocationsAsync(ResourceManagementDbContext db)
    {
        if (await db.ForecastAllocations.AnyAsync()) return;

        var resources = await db.Resources.Take(10).ToListAsync();

        var months = new[] { (2026, 6), (2026, 7), (2026, 8) };
        foreach (var res in resources)
        {
            foreach (var (yr, mo) in months)
            {
                int workingDays = Enumerable.Range(1, DateTime.DaysInMonth(yr, mo))
                    .Select(d => new DateTime(yr, mo, d).DayOfWeek)
                    .Count(dow => dow != DayOfWeek.Saturday && dow != DayOfWeek.Sunday);

                decimal forecastHours = workingDays * 9m;   // 9 hrs/day × FTE 1.0

                db.ForecastAllocations.Add(new ForecastAllocation
                {
                    ResourceId   = res.Id,
                    Year         = yr,
                    Month        = mo,
                    ForecastHours = forecastHours,
                    FteFraction  = 1m,
                    ForecastCost = forecastHours * 50m    // placeholder rate
                });
            }
        }
        await db.SaveChangesAsync();
    }
}
