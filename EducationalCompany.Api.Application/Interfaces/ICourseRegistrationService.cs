using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Domain.Entities;

namespace EducationalCompany.Api.Application.Interfaces
{
    // Interface that defines operations for course registrations
    public interface ICourseRegistrationService
    {
        Task<IEnumerable<CourseRegistrationDto>> GetAllRegistrationsAsync(); // Get all registrations
        Task<CourseRegistrationDto> GetRegistrationByIdAsync(Guid id); // Get registration by ID
        Task<CourseRegistrationDto> CreateRegistrationAsync(CreateCourseRegistrationDto dto); // Create new registration
        Task<CourseRegistrationDto> UpdateRegistrationAsync(UpdateCourseRegistrationDto dto); // Update registration
        Task<bool> DeleteRegistrationAsync(Guid id);  // Delete registration
        Task<IEnumerable<CourseRegistrationDto>> GetRegistrationsByParticipantAsync(Guid participantId);  // Get registrations by participant
        Task<IEnumerable<CourseRegistrationDto>> GetRegistrationsByOccasionAsync(Guid courseOccasionId);// Get registrations by course occasion
        Task<CourseRegistrationDto> GetRegistrationDetailsAsync(Guid id); // Get registration with details
        Task ConfirmRegistrationsAsync (Guid id);// Confirm registration
        Task CancelRegistrationAsync (Guid id);// Cancel registration
    }  
}
