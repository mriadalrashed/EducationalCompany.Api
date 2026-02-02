using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Domain.Entities;

namespace EducationalCompany.Api.Application.Interfaces
{
    public interface ICourseOccasionService
    {
        Task<IEnumerable<CourseOccasionDto>> GetAllOccasionsAsync();
        Task<CourseOccasionDto> GetOccasionByIdAsync(Guid id);
        Task<CourseOccasionDto> CreateOccasionAsync(CreateCourseOccasionDto dto);
        Task<CourseOccasionDto> UpdateOccasionAsync(UpdateCourseOccasionDto dto);
        Task<bool> DeleteOccasionAsync(Guid id);
        Task<IEnumerable<CourseOccasionDto>> GetOccasionsByIdAsync(Guid courseId);
        Task AssignTeacherAsync (Guid occasionId, AssignTeacherDto dto);
        Task <CourseOccasionDto> GetOccasionWithRegstrationsAsync (Guid id);
        Task <bool> IsOccasionFullAsync (Guid id);
        Task <CourseOccasionDto> MaptoDto (CourseOccasion occasion);
    }
}
