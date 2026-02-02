using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Domain.Entities;

namespace EducationalCompany.Api.Application.Interfaces
{
    public interface ICourseService
    {
        Task<IEnumerable<CourseDto>> GetAllCoursesAsync();
        Task<CourseDto> GetCourseByIdAsync(Guid id);
        Task<CourseDto> CreateCourseAsync(CreateCourseDto dto);
        Task<CourseDto> UpdateCourseAsync(UpdateCourseDto dto);
        Task<bool> DeleteCourseAsync(Guid id);
        Task<IEnumerable<CourseDto>> SearchCoursesAsync(string searchTerm);
    }
}
