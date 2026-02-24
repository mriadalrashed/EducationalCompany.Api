using EducationalCompany.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;


namespace EducationalCompany.Api.Infrastructure.Extensions
{
    // Extension class used to register infrastructure services
    public static class ServiceExtensions
    {
        // Registers database and infrastructure dependencies
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register ApplicationDbContext with PostgreSQL provider
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            // Register UnitOfWork for handling repositories and transactions
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}

/*
using EducationalCompany.Api.Infrastructure;
using EducationalCompany.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCompany.Api.Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    IConfiguration configuration)
    {
        // Add SQL Server Database
        services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
        configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Add Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}*/