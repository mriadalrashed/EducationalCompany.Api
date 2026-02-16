using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Infrastructure;
using EducationalCompany.Api.Application.Interfaces;

namespace EducationalCompany.Api.Application.Services;
    public class TeacherService : ITeacherService
    {
    private readonly IUnitOfWork _unitOfWork;

    public TeacherService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ParticipantDto>> GetAllTeacherAsync()
    {
        var teachers = await _unitOfWork.Participants.GetAllAsync();
        return teachers.Select(MapToDto);
    }
    
    public async Task <TeacherDto> GetTeacherByIdAsync (Guid id)
    {
        var teacher = await _unitOfWork.Participants.GetByIdAsync(id);
        if (teacher == null) 
            throw new KeyNotFoundException($"Teacher with id {id} not found.");

        return MapToDto(teacher);
    }
    public async Task<TeacherDto> CreateTeacherAsync(CreateTeacherDto dto)
    {
        var existingTeacher = (await _unitOfWork.Teachers.SearchTeachersAsync(dto.Email)).FirstOrDefault();
        if (existingTeacher != null)
            throw new InvalidOperationException($"Teaher With Email '{dto.Email}' Already Exists");

        var teacher = new Teacher(
            dto.FirstName,
            dto.LastName,
            dto.Email,
            dto.Phone,
            dto.Specialization
            );

        await _unitOfWork.Teachers.AddAsync(teacher);
        return MapToDto(teacher);
    }
    public async Task<TeacherDto> UpdateTeacherAsync(Guid id,UpdateTeacherDto dto)
    {
       
       var teacher = await _unitOfWork.Teachers.GetByIdAsync(id);

       if (teacher == null)
            throw new KeyNotFoundException($"Teacher with id {id} not found.");
        if (dto.Email != teacher.Email) 
        { 
            var existingTeacher = (await _unitOfWork.Teachers.SearchTeachersAsync(dto.Email)).FirstOrDefault(t => t.Id != id);
        if (existingTeacher != null)
            throw new InvalidOperationException($"Teaher With Email '{dto.Email}' Already Exists");
        }

        teacher.Update(dto.FirstName, dto.LastName, dto.Email, dto.Phone, dto.Specialization);
        await _unitOfWork.Teachers.UpdateAsync(teacher);
    }

    public async Task DeleteTeacherAsync (Guid id)
    {
        var teacher = await _unitOfWork.Teachers.GetByIdAsync(id);
        if(teacher == null)
            throw new KeyNotFoundException($"Teacher with id {id} not found.");
        var occasion = _unitOfWork.CourseOccasions.GetByCourseIdAsync(id);
        if (occasion.Any())
            throw new InvalidOperationException($"Can Not Delete Teacher Who Is Assgin To Course Occasion");
        await _unitOfWork.Teachers.DeleteAsync(teacher);
    }

    public async Task<IEnumerable<TeacherDto>> SearchTeachersAsync(string searchTerm)
    {
        var teachers = await _unitOfWork.Teachers.SearchTeachersAsync(searchTerm);
        return teachers.Select(MapToDto);
    }

    private TeacherDto MapToDto(Teacher teacher)
    {
        return new TeacherDto
        {
          Id = teacher.id,
          FirstName = teacher.firstName,
          LastName = teacher.lastName,
          Email = teacher.email,
          Phone = teacher.phone,
          Specialization = teacher.specialization,
          CreatedAt = teacher.createdAt,
          UpdatedAt = teacher.updatedAt
        };
    }
}
