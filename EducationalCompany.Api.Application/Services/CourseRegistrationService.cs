using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Application.Interfaces;
using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Infrastructure;
using System.Net.NetworkInformation;

namespace EducationalCompany.Api.Application.Services;

// Service responsible for handling course registration business logic
public class CourseRegistrationService : ICourseRegistrationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICourseOccasionService _courseoccasisonservice;

    // Constructor with dependency injection
    public CourseRegistrationService(
        IUnitOfWork unitOfWork,
        ICourseOccasionService courseoccasisonservice)
    {
        _unitOfWork = unitOfWork;
        _courseoccasisonservice = courseoccasisonservice;
    }

    // Get all registrations
    public async Task<IEnumerable<CourseRegistrationDto>> GetAllRegistrationsAsync()
    {
        var registrations =
            await _unitOfWork.CourseRegistrations.GetAllAsync();

        return await MapToDtoList(registrations);
    }

    // Get single registration by ID
    public async Task<CourseRegistrationDto> GetRegistrationByIdAsync(Guid id)
    {
        var registrations =
            await _unitOfWork.CourseRegistrations.GetByIdAsync(id);

        if (registrations == null)
            throw new KeyNotFoundException(
                $"Registration with id {id} not found.");

        return await MapToDto(registrations);
    }

    // Create new registration with transaction
    public async Task<CourseRegistrationDto> CreateRegistrationAsync(
        CreateCourseRegistrationDto dto)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            // Validate participant
            var participant =
                await _unitOfWork.Participants.GetByIdAsync(dto.ParticipantId);

            if (participant == null)
                throw new KeyNotFoundException(
                    $"Participant with id {dto.ParticipantId} not found.");

            // Validate occasion
            var occasion =
                await _unitOfWork.CourseOccasions.GetByIdAsync(dto.CourseOccasionId);

            if (occasion == null)
                throw new KeyNotFoundException(
                    $"Occasion with id {dto.CourseOccasionId} not found.");

            // Check if occasion is full
            if (occasion.IsFull)
                throw new InvalidOperationException(
                    "course occasion is full");

            // Check duplicate registration
            var exsistingRegistration =
                await _unitOfWork.CourseRegistrations
                    .HasRegistrationAsync(dto.ParticipantId, dto.CourseOccasionId);

            if (exsistingRegistration)
                throw new InvalidOperationException(
                    "Participant already registred for this course.");

            // Create registration entity
            var registration = new CourseRegistration(
                dto.ParticipantId,
                dto.CourseOccasionId
            );

            // Try increment participant count
            if (!occasion.TryRegisterParticipant())
                throw new InvalidOperationException(
                    "Faild Registration , Occasion is full.");

            await _unitOfWork.CourseRegistrations.AddAsync(registration);
            await _unitOfWork.CourseOccasions.UpdateAsync(occasion);

            await _unitOfWork.CommitTransactionAsync();

            // Get full details
            var registrationWithDetails =
                await _unitOfWork.CourseRegistrations
                    .GetRegistrationDetailsAsync(registration.Id);

            return await MapToDto(
                registrationWithDetails ?? registration);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    // Update registration status
    public async Task UpdateRegistrationsStatusAsync(
        Guid id,
        UpdateRegistrationStatusDto dto)
    {
        var registration =
            await _unitOfWork.CourseRegistrations.GetByIdAsync(id);

        if (registration == null)
            throw new KeyNotFoundException(
                $"registration with id {id} not found.");

        // Handle status change
        switch (dto.Status.ToLower())
        {
            case "confirmed":
                registration.Confirm();
                break;

            case "Cancelled":
                registration.Cancel();
                break;

            default:
                throw new ArgumentException(
                    $"invalid status: {dto.Status}. must be confirmed or cancelled");
        }

        await _unitOfWork.CourseRegistrations.UpdateAsync(registration);
    }

    // Delete registration
    public async Task DeleteRegistrationAsync(Guid id)
    {
        var registration =
            await _unitOfWork.CourseRegistrations.GetByIdAsync(id);

        if (registration == null)
            throw new KeyNotFoundException(
                $"registration with id {id} not found.");

        var occasion =
            await _unitOfWork.CourseOccasions
                .GetByIdAsync(registration.CourseOccasionId);

        if (occasion != null)
        {
            occasion.CancelRegistration();
            await _unitOfWork.CourseOccasions.UpdateAsync(occasion);
        }

        await _unitOfWork.CourseRegistrations.DeleteAsync(id);
    }

    // Get registrations by participant
    public async Task<IEnumerable<CourseRegistrationDto>>
        GetRegistrationsByParticipantAsync(Guid participantId)
    {
        var registrations =
            await _unitOfWork.CourseRegistrations
                .GetRegistrationsByParticipantAsync(participantId);

        return await MapToDtoList(registrations);
    }

    // Get registrations by occasion
    public async Task<IEnumerable<CourseRegistrationDto>>
        GetRegistrationsByOccasionAsync(Guid occasionId)
    {
        var registrations =
            await _unitOfWork.CourseRegistrations
                .GetRegistrationsByOccasionAsync(occasionId);

        return await MapToDtoList(registrations);
    }

    // Get registration with full details
    public async Task<CourseRegistrationDto>
        GetRegistrationDetailsAsync(Guid id)
    {
        var registrations =
            await _unitOfWork.CourseRegistrations
                .GetRegistrationDetailsAsync(id);

        if (registrations == null)
            throw new KeyNotFoundException(
                $"registration with id {id} not found.");

        return await MapToDto(registrations);
    }

    public async Task ConfirmRegistrationAsync(Guid id)
    {
        var registration = await _unitOfWork.CourseRegistrations.GetByIdAsync(id);
        if (registration == null)
            throw new KeyNotFoundException($"Registration with ID {id} not found");

        registration.Confirm();
        await _unitOfWork.CourseRegistrations.UpdateAsync(registration);
    }

    // Cancel registration with transaction
    public async Task CancelRegistrationAsync(Guid id)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var registration =
                await _unitOfWork.CourseRegistrations.GetByIdAsync(id);

            if (registration == null)
                throw new KeyNotFoundException(
                    $"registration with id {id} not found.");

            var occasion =
                await _unitOfWork.CourseOccasions
                    .GetByIdAsync(registration.CourseOccasionId);

            if (occasion == null)
                throw new KeyNotFoundException(
                    $" course occasion with id {registration.CourseOccasionId} not found.");

            registration.Cancel();
            occasion.CancelRegistration();

            await _unitOfWork.CourseRegistrations.UpdateAsync(registration);
            await _unitOfWork.CourseOccasions.UpdateAsync(occasion);

            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    // Map entity to DTO (including related data)
    private async Task<CourseRegistrationDto>
        MapToDto(CourseRegistration r)
    {
        var dto = new CourseRegistrationDto {
            Id = r.Id,
            ParticipantId = r.ParticipantId,
            CourseOccasionId = r.CourseOccasionId,
            Status = r.Status,
            RegistrationDate = r.RegistrationDate,
            ConfirmedAt = r.ConfirmedAt,
            CancelledAt = r.CancelledAt,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        // Load participant details
        if (r.ParticipantId != Guid.Empty)
        {
            var p =
                await _unitOfWork.Participants
                    .GetByIdAsync(r.ParticipantId);

            if (p != null)
            {
                dto.Participant = new ParticipantDto
                {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = p.Email,
                    Phone = p.Phone,
                    Address = p.Address,
                    UpdatedAt = p.UpdatedAt,
                    CreatedAt = p.CreatedAt
                };
            }
        }

        // Load occasion details
        if (r.CourseOccasionId != Guid.Empty)
        {
            var O =
                await _unitOfWork.CourseOccasions
                    .GetByIdAsync(r.CourseOccasionId);

            if (O != null)
            {
                dto.CourseOccasion =
                    await _courseoccasisonservice.MapToDto(O);
            }
        }

        return dto;
    }

    // Map list of registrations
    private async Task<IEnumerable<CourseRegistrationDto>>
        MapToDtoList(IEnumerable<CourseRegistration> r)
    {
        var dtos = new List<CourseRegistrationDto>();

        foreach (var c in r)
        {
            dtos.Add(await MapToDto(c));
        }

        return dtos;
    }
}