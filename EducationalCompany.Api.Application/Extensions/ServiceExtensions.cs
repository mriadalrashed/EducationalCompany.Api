using EducationalCompany.Api.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using EducationalCompany.Api.Application.Interfaces;

namespace EducationalCompany.Api.Application.Extensions
{
    // Extension class used to register application services in DI container
    public static class ServiceExtensions
    {
        // Registers all Application layer services
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<ITeacherService,TeacherService>();
            services.AddScoped<IParticipantService, ParticipantService>();
            services.AddScoped<ICourseRegistrationService, CourseRegistrationService>();
            services.AddScoped<ICourseOccasionService, CourseOccasionService>();
            return services;
        }
    }
}
