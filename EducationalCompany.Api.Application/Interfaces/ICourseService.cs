using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Domain.Entities;

namespace EducationalCompany.Api.Application.Interfaces
{
    // Interface that defines operations for course management
    public interface ICourseService
    {
        Task<IEnumerable<CourseDto>> GetAllCoursesAsync(); // Get all courses
        Task<CourseDto> GetCourseByIdAsync(Guid id); // Get course by ID
        Task<CourseDto> CreateCourseAsync(CreateCourseDto dto); // Create new course
        Task UpdateCourseAsync(Guid id, UpdateCourseDto dto); // Update course
        Task DeleteCourseAsync(Guid id); // Delete course
        Task<IEnumerable<CourseDto>> SearchCoursesAsync(string searchTerm); // Search courses by keyword
    }
}
