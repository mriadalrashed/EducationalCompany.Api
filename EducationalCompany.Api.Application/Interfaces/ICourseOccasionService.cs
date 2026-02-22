using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Domain.Entities;

namespace EducationalCompany.Api.Application.Interfaces
{
    // Interface that defines all operations related to course occasions
    public interface ICourseOccasionService
    {
        Task<IEnumerable<CourseOccasionDto>> GetAllOccasionsAsync(); // Get all occasions
        Task<CourseOccasionDto> GetOccasionByIdAsync(Guid id);  // Get occasion by ID
        Task<CourseOccasionDto> CreateOccasionAsync(CreateCourseOccasionDto dto); // Create new occasion
        Task<CourseOccasionDto> UpdateOccasionAsync(UpdateCourseOccasionDto dto); // Update occasion
        Task<bool> DeleteOccasionAsync(Guid id); // Delete occasion
        Task<IEnumerable<CourseOccasionDto>> GetOccasionsByCourseIdAsync(Guid courseId); // Get occasions by course
        Task<IEnumerable<CourseOccasionDto>> GetUpComingOccasionsAsync(); // Get upcoming occasions
        Task AssignTeacherAsync (Guid occasionId, AssignTeacherDto dto); // Assign teacher to occasion
        Task <CourseOccasionDto> GetOccasionWithRegistrationsAsync(Guid id);  // Get occasion with registrations
        Task <bool> IsOccasionFullAsync (Guid id); // Check if occasion is full
        Task <CourseOccasionDto> MapToDto(CourseOccasion occasion); // Map entity to DTO
    }
}
