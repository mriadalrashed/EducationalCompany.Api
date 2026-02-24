using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Domain.Entities;

namespace EducationalCompany.Api.Application.Interfaces
{
    // Interface that defines operations for teacher management
    public interface ITeacherService
    {
        Task<IEnumerable<TeacherDto>> GetAllTeachersAsync();  // Get all teachers
        Task<TeacherDto> GetTeacherByIdAsync(Guid id); // Get teacher by ID
        Task<TeacherDto> CreateTeacherAsync(CreateTeacherDto createTeacherDto); // Create new teacher
        Task UpdateTeacherAsync(Guid id, UpdateTeacherDto updateTeacherDto);  // Update teacher
        Task DeleteTeacherAsync(Guid id); // Delete teacher

        Task<IEnumerable<TeacherDto>> SearchTeachersAsync(string searchTerm); // Search teachers
        Task<TeacherDto> GetTeacherWithOccasionsAsync(Guid id); // Get teacher with assigned occasions
    }
}
