using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Application.Interfaces;
using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Infrastructure;

namespace EducationalCompany.Api.Application.Services;

// Service responsible for handling course occasion business logic
public class CourseOccasionService : ICourseOccasionService
{
    private readonly IUnitOfWork _unitOfWork;

    public CourseOccasionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // Get all course occasions
    public async Task<IEnumerable<CourseOccasionDto>> GetAllOccasionsAsync()
    {
        var occasions = await _unitOfWork.CourseOccasions.GetAllAsync();
        return await MapToDto(occasions);
    }

    // Get single occasion by ID
    public async Task<CourseOccasionDto> GetOccasionByIdAsync(Guid id)
    {
        var occasion = await _unitOfWork.CourseOccasions.GetByIdAsync(id);

        if (occasion == null)
            throw new KeyNotFoundException($"occasion with id {id} not found.");

        return await MapToDto(occasion);
    }

    // Create new course occasion
    public async Task<CourseOccasionDto> CreateOccasionAsync(CreateCourseOccasionDto dto)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(dto.CourseId);

        if (course == null)
            throw new InvalidOperationException($"course with id {dto.CourseId} already exists.");

        var Occasion = new CourseOccasion
        {
            dto.CourseId,
            dto.StartDate.ToUniversalTime(),
            dto.EndDate.ToUniversalTime(),
            dto.MaxParticipants
        };

        await _unitOfWork.Occasions.AddAsync(Occasion);

        var OccasionsWithDetails =
            await _unitOfWork.CourseOccasions.GetWithRegistrationsAsync(Occasion.Id);

        return await MapToDto(OccasionsWithDetails ?? Occasion);
    }

    // Update existing occasion
    public async Task UpdateOccasionAsync(Guid id, CreateCourseOccasionDto dto)
    {
        var occasion = await _unitOfWork.CourseOccasions.GetByIdAsync(id);

        if (occasion == null)
            throw new KeyNotFoundException($"courseoccasion with id {id} not found.");

        var course = await _unitOfWork.Courses.GetByIdAsync(dto.CourseId);

        if (course == null)
            throw new KeyNotFoundException($"courseoccasion with id {dto.CourseId} not found.");

        var registrations =
            await _unitOfWork.CourseRegistrations.GetRegistrationsByOccasionAsync(id);

        if (registrations.Any())
            throw new InvalidOperationException($"can not update occasion that has registration.");

        occasion.updateDetails(
            dto.StartDate.ToUniversalTime(),
            dto.EndDate.ToUniversalTime(),
            dto.MaxParticipants);

        await _unitOfWork.CourseOccasions.UpdateAsync(occasion);
    }

    // Delete occasion
    public async Task DeleteOccasionAsync(Guid id)
    {
        var occasion = await _unitOfWork.CourseOccasions.GetByIdAsync(id);

        if (occasion == null)
            throw new KeyNotFoundException($"courseoccasion with id {id} not found.");

        var registrations =
            await _unitOfWork.CourseRegistrations.GetRegistrationByOccasionAsync(id);

        if (registrations.Any())
            throw new InvalidOperationException($"can not delete occasion that has registration.");

        await _unitOfWork.CourseOccasions.DeleteAsync(occasion);
    }

    // Get occasions by course
    public async Task<IEnumerable<CourseOccasionDto>> GetOccasionsByCourseIdAsync(Guid courseId)
    {
        var occasion = await _unitOfWork.CourseOccasions.GetByCourseIdAsync(courseId);
        return await MapToDto(occasion);
    }

    // Get upcoming occasions
    public async Task<IEnumerable<CourseOccasionDto>> GetUpComingOccasionsAsync()
    {
        var occasion = await _unitOfWork.CourseOccasions.GetUpComingOccasionsAsync();
        return await MapToDto(occasion);
    }

    // Assign teacher to occasion
    public async Task AssignTeacherAsync(Guid occasionId, AssignTeacherDto dto)
    {
        var occasion = await _unitOfWork.CourseOccasions.GetByIdAsync(occasionId);

        if (occasion == null)
            throw new KeyNotFoundException($"courseoccasion with id {occasionId} not found.");

        var teacher = await _unitOfWork.Teachers.GetByIdAsync(dto.TeacherId);

        if (teacher == null)
            throw new InvalidOperationException($"teacher with id {dto.TeacherId} not found.");

        occasion.AssignTeacher(dto.TeacherId);

        await _unitOfWork.CourseOccasions.UpdateAsync(occasion);
    }

    // Get occasion with registrations
    public async Task<CourseOccasionDto> GetOccasionWithRegstrationsAsync(Guid id)
    {
        var occasion = await _unitOfWork.CourseOccasions.GetWithRegistrationsAsync(id);

        if (occasion == null)
            throw new KeyNotFoundException($"course occasion with id {id} not found.");

        return await MapToDto(occasion);
    }

    // Check if occasion is full
    public async Task<bool> IsOccasionFullAsync(Guid id)
    {
        return await _unitOfWork.CourseOccasions.IsOccasionFullAsync(id);
    }

    // Map list of entities to DTO list
    private async Task<IEnumerable<CourseOccasionDto>> MapToDto(IEnumerable<CourseOccasion> occasions)
    {
        var dtos = new List<CourseOccasionDto>();

        foreach (var occasion in occasions)
        {
            dtos.Add(await MapToDto(occasion));
        }

        return dtos;
    }
}