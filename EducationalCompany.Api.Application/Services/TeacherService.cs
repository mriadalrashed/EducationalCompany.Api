using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Infrastructure;
using EducationalCompany.Api.Application.Interfaces;

namespace EducationalCompany.Api.Application.Services;

// Service responsible for teacher business logic
public class TeacherService : ITeacherService
{
    private readonly IUnitOfWork _unitOfWork;

    // Constructor with dependency injection
    public TeacherService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // Get all teachers
    public async Task<IEnumerable<TeacherDto>> GetAllTeachersAsync()
    {
        var teachers = await _unitOfWork.Teachers.GetAllAsync();
        return teachers.Select(MapToDto);
    }

    // Get teacher by ID
    public async Task<TeacherDto> GetTeacherByIdAsync(Guid id)
    {
        var teacher = await _unitOfWork.Teachers.GetByIdAsync(id);

        if (teacher == null)
            throw new KeyNotFoundException($"Teacher with id {id} not found.");

        return MapToDto(teacher);
    }

    // Create new teacher
    public async Task<TeacherDto> CreateTeacherAsync(CreateTeacherDto dto)
    {
        // Check for null DTO first
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        // Check if teacher email already exists
        var existingTeacher =
            (await _unitOfWork.Teachers.SearchTeachersAsync(dto.Email))
            .FirstOrDefault();

        if (existingTeacher != null)
            throw new InvalidOperationException(
                $"Teaher With Email '{dto.Email}' Already Exists");

        // Create teacher entity
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

    // Update teacher
    public async Task UpdateTeacherAsync(Guid id, UpdateTeacherDto dto)
    {
        // Check for null DTO first
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var teacher = await _unitOfWork.Teachers.GetByIdAsync(id);

        if (teacher == null)
            throw new KeyNotFoundException(
                $"Teacher with id {id} not found.");

        // Check if email changed and already exists
        if (dto.Email != teacher.Email)
        {
            var existingTeacher =
                (await _unitOfWork.Teachers.SearchTeachersAsync(dto.Email))
                .FirstOrDefault(t => t.Id != id);

            if (existingTeacher != null)
                throw new InvalidOperationException(
                    $"Teaher With Email '{dto.Email}' Already Exists");
        }

        // Update entity
        teacher.Update(
            dto.FirstName,
            dto.LastName,
            dto.Email,
            dto.Phone,
            dto.Specialization);

        await _unitOfWork.Teachers.UpdateAsync(teacher);
    }

    // Delete teacher
    public async Task DeleteTeacherAsync(Guid id)
    {

        var teacher = await _unitOfWork.Teachers.GetByIdAsync(id);

        if (teacher == null)
            throw new KeyNotFoundException(
                $"Teacher with id {id} not found.");

        // Check if teacher is assigned to course occasions
        var occasions =
            await _unitOfWork.CourseOccasions.GetByTeacherIdAsync(id);

        if (occasions.Any())
            throw new InvalidOperationException(
                $"Can Not Delete Teacher Who Is Assgin To Course Occasion");

        await _unitOfWork.Teachers.DeleteAsync(id);
    }

    // Search teachers
    public async Task<IEnumerable<TeacherDto>> SearchTeachersAsync(string searchTerm)
    {
        var teachers =
            await _unitOfWork.Teachers.SearchTeachersAsync(searchTerm);

        return teachers.Select(MapToDto);
    }

    public async Task<TeacherDto> GetTeacherWithOccasionsAsync(Guid id)
    {
        var teacher = await _unitOfWork.Teachers.GetTeacherWithOccasionsAsync(id);
        if (teacher == null)
            throw new KeyNotFoundException($"Teacher with ID {id} not found");

        var dto = MapToDto(teacher);

        // Add occasions to DTO if needed
        // You can create a more detailed DTO if required

        return dto;
    }

    // Map Teacher entity to DTO
    private TeacherDto MapToDto(Teacher teacher)
    {
        return new TeacherDto
        {
            Id = teacher.Id,
            FirstName = teacher.FirstName,
            LastName = teacher.LastName,
            Email = teacher.Email,
            Phone = teacher.Phone,
            Specialization = teacher.Specialization,
            CreatedAt = teacher.CreatedAt,
            UpdatedAt = teacher.UpdatedAt
        };
    }
}