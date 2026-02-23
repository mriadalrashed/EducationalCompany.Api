using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Application.Interfaces;
using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Infrastructure;

namespace EducationalCompany.Api.Application.Services;

public class CourseOccasionService : ICourseOccasionService
{
    private readonly IUnitOfWork _unitOfWork;

    public CourseOccasionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CourseOccasionDto>> GetAllOccasionsAsync()
    {
        var occasions = await _unitOfWork.CourseOccasions.GetAllAsync();
        return await MapToDto(occasions);
    }

    public async Task<CourseOccasionDto> GetOccasionByIdAsync(Guid id)
    {
        var occasion = await _unitOfWork.CourseOccasions.GetByIdAsync(id);

        if (occasion == null)
            throw new KeyNotFoundException();

        return await MapToDto(occasion);
    }

    public async Task<CourseOccasionDto> CreateOccasionAsync(CreateCourseOccasionDto dto)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(dto.CourseId);

        if (course == null)
            throw new KeyNotFoundException("Course not found");

        var occasion = new CourseOccasion(
            dto.CourseId,
            dto.StartDate.ToUniversalTime(),
            dto.EndDate.ToUniversalTime(),
            dto.MaxParticipants
        );

        await _unitOfWork.CourseOccasions.AddAsync(occasion);

        return await MapToDto(occasion);
    }

    public async Task UpdateOccasionAsync(Guid id, CreateCourseOccasionDto dto)
    {
        var occasion = await _unitOfWork.CourseOccasions.GetByIdAsync(id);
        if (occasion == null)
            throw new KeyNotFoundException($"Course occasion with ID {id} not found");

        // Check if course exists
        var course = await _unitOfWork.Courses.GetByIdAsync(dto.CourseId);
        if (course == null)
            throw new KeyNotFoundException($"Course with ID {dto.CourseId} not found");

        // Cannot update if there are registrations
        var registrations = await _unitOfWork.CourseRegistrations.GetRegistrationsByOccasionAsync(id);
        if (registrations.Any())
            throw new InvalidOperationException("Cannot update occasion that has registrations");

        // Update occasion details using the new UpdateDetails method
        occasion.UpdateDetails(
            dto.StartDate.ToUniversalTime(),
            dto.EndDate.ToUniversalTime(),
            dto.MaxParticipants);

        // If course is changing, we need to handle it differently
        // For now, we'll create a new occasion with updated values
        // This is a simplified approach - in production you'd need a more robust solution

        await _unitOfWork.CourseOccasions.UpdateAsync(occasion);
    }

    public async Task DeleteOccasionAsync(Guid id)
    {
        var occasion = await _unitOfWork.CourseOccasions.GetByIdAsync(id);
        if (occasion == null)
            throw new KeyNotFoundException($"Course occasion with ID {id} not found");

        // Cannot delete if there are registrations
        var registrations = await _unitOfWork.CourseRegistrations.GetRegistrationsByOccasionAsync(id);
        if (registrations.Any())
            throw new InvalidOperationException("Cannot delete occasion that has registrations");

        await _unitOfWork.CourseOccasions.DeleteAsync(id);
    }

    public async Task<IEnumerable<CourseOccasionDto>> GetOccasionsByCourseIdAsync(Guid courseId)
    {
        var occasions = await _unitOfWork.CourseOccasions.GetByCourseIdAsync(courseId);
        return await MapToDto(occasions);
    }

    public async Task<IEnumerable<CourseOccasionDto>> GetUpComingOccasionsAsync()
    {
        var occasions = await _unitOfWork.CourseOccasions.GetUpcomingOccasionsAsync();
        return await MapToDto(occasions);
    }

    public async Task AssignTeacherAsync(Guid occasionId, AssignTeacherDto dto)
    {
        var occasion = await _unitOfWork.CourseOccasions.GetByIdAsync(occasionId);

        if (occasion == null)
            throw new KeyNotFoundException();

        var teacher = await _unitOfWork.Teachers.GetByIdAsync(dto.TeacherId);

        if (teacher == null)
            throw new KeyNotFoundException();

        occasion.AssignTeacher(dto.TeacherId);

        await _unitOfWork.CourseOccasions.UpdateAsync(occasion);
    }

    public async Task<CourseOccasionDto> GetOccasionWithRegistrationsAsync(Guid id)
    {
        var occasion = await _unitOfWork.CourseOccasions.GetWithRegistrationsAsync(id);

        if (occasion == null)
            throw new KeyNotFoundException();

        return await MapToDto(occasion);
    }

    public async Task<bool> IsOccasionFullAsync(Guid id)
    {
        return await _unitOfWork.CourseOccasions.IsOccasionFullAsync(id);
    }

    public async Task<CourseOccasionDto> MapToDto(CourseOccasion occasion)
    {
        return new CourseOccasionDto
        {
            Id = occasion.Id,
            CourseId = occasion.CourseId,
            TeacherId = occasion.TeacherId,
            StartDate = occasion.StartDate,
            EndDate = occasion.EndDate,
            MaxParticipants = occasion.MaxParticipants,
            CurrentParticipants = occasion.CurrentParticipants,
            IsFull = occasion.IsFull,
        };
    }

    private async Task<IEnumerable<CourseOccasionDto>> MapToDto(IEnumerable<CourseOccasion> occasions)
    {
        var list = new List<CourseOccasionDto>();

        foreach (var occasion in occasions)
        {
            list.Add(await MapToDto(occasion));
        }

        return list;
    }
}