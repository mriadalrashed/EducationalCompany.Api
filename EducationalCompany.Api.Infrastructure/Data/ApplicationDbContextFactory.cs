/*    using EducationalCompany.Api.Infrastructure.Data;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;
    using Microsoft.Extensions.Configuration;
    using System.IO;

    namespace EducationalCompany.Infrastructure.Data
    {
        public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
        {
            public ApplicationDbContext CreateDbContext(string[] args)
            {
                // Build configuration to read appsettings.json
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory()) // make sure this points to the startup project folder
                    .AddJsonFile("appsettings.json")
                    .Build();

                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

                //var connectionString = configuration.GetConnectionString("DefaultConnection");
                //optionsBuilder.UseNpgsql(connectionString);


                return new ApplicationDbContext(optionsBuilder.Options);
            }
        }
    }*/