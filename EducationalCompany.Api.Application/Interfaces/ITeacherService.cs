using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Domain.Entities;

namespace EducationalCompany.Api.Application.Interfaces
{
    public interface ITeacherService
    {
        Task<IEnumerable<TeacherDto>> GetAllTeachersAsync();
        Task<TeacherDto> GetTeacherByIdAsync(Guid id);
        Task<TeacherDto> CreateTeacherAsync(CreateTeacherDto createTeacherDto);
        Task<TeacherDto> UpdateTeacherAsync(UpdateTeacherDto updateTeacherDto);
        Task<bool> DeleteTeacherAsync(Guid id);

        Task<IEnumerable<TeacherDto>> SearchTeachersAsync(string searchTerm);
        Task<TeacherDto> GetTeacherWithOccasionsAsync(Guid id);
    }
}
