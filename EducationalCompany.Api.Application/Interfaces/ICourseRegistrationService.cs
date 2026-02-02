using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Domain.Entities;

namespace EducationalCompany.Api.Application.Interfaces
{
    public interface ICourseRegistrationService
    {
        Task<IEnumerable<CourseRegistrationDto>> GetAllRegistrationsAsync();
        Task<CourseRegistrationDto> GetRegistrationByIdAsync(Guid id);
        Task<CourseRegistrationDto> CreateRegistrationAsync(CreateCourseRegistrationDto dto);
        Task<CourseRegistrationDto> UpdateRegistrationAsync(UpdateCourseRegistrationDto dto);
        Task<bool> DeleteRegistrationAsync(Guid id);
        Task<IEnumerable<CourseRegistrationDto>> GetRegistrationsByParticipantAsync(Guid participantId);
        Task<IEnumerable<CourseRegistrationDto>> GetRegistrationsByOccasionAsync(Guid courseOccasionId);
        Task<CourseRegistrationDto> GetRegistrationsDetailsAsync(Guid id);
        Task ConfirmRegistrationssAsync (Guid id);
        Task CancelRegistrationAsync (Guid id);
    }  
}
