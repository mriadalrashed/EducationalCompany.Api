using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Application.Interfaces;
using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Infrastructure;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace EducationalCompany.Api.Application.Services;

public class CourseRegistrationService : ICourseRegistrationService
{

    private readonly IUnitOfWork _unitOfWork;

    private readonly ICourseOccasisonService _courseoccasisonservice;

    public CourseRegistrationService(IUnitOfWork unitOfWork, ICourseOccasisonService courseoccasisonservice)
    {
        _unitOfWork = unitOfWork;
        _courseoccasisonservice = courseoccasisonservice;
    }

    public async Task<IEnumerable<CourseRegistrationDto>> GetAllRegistrationsAsync()
    {
        var registrations = await _unitOfWork.CourseRegistrations.GetAllAsync();
        return await MapToDtoList(registrations);
    }

    public async Task<CourseRegistrationDto> GetRegistrationByIdAsync(Guid id)
    {
        var registrations = await _unitOfWork.CourseRegistrations.GetByIdAsync(id);
        if (registrations == null)
            throw new KeyNotFoundException($"Registration with id {id} not found.");

        return await MapToDto(registrations);
    }

    public async Task<CourseRegistrationDto> CreateRegistrationAsync(CreateCourseRegistrationDto dto)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var participant = await _unitOfWork.Participants.GetByIdAsync(dto.ParticipantId);
            if (participant == null)
                throw new KeyNotFoundException($"Participant with id {dto.ParticipantId} not found.");
            var occasion = await _unitOfWork.CourseOccasions.GetByIdAsync(dto.CourseOccasionId);
            if (occasion == null)
                throw new KeyNotFoundException($"Occasion with id {dto.CourseOccasionId} not found.");

            if (occasion.IsFull)
                throw new InvalidOperationException("course occasion is full");

            var exsistingRegistration = await _unitOfWork.CourseRegistrations.HasRegistrationAsync(dto.ParticipantId, dto.CourseOccasionId);

            if (exsistingRegistration)
                throw new InvalidOperationException("Participant already registred for this course.");

            var registration = new CourseRegistration(
                dto.ParticipantId,
                dto.CourseOccasionId
             );

            if (!occasion.TryRegisterParticipant())
                throw new InvalidOperationException("Faild Registration , Occasion is full.");

            await _unitOfWork.CourseRegistrations.AddAsync(registration);
            await _unitOfWork.CourseOccasions.UpdateAsync(occasion);
            await _unitOfWork.CommitTransactionAsync();

            var registrationWithDetails = await _unitOfWork.CourseRegistrations.GetRegistrationsDetailsAsync(registration.id);

            return await MapToDto(registrationWithDetails ?? registration);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionsAsync();
            throw;
        }
    }

    public async Task UpdateRegistrationsStatusAsync(Guid id, UpdateCourseRegistrationDto dto)
    {
        var registration = await _unitOfWork.CourseRegistrations.GetByIdAsync(id); 
        if (registration == null)
            throw new KeyNotFoundException($"registration with id {id} not found.");
        switch (dto.Status.ToLower())
        {
            case "confirmed" :
                registration.Confirm();
                break;

            case "Cancelled":
                registration.CancelRegistrationAsync();
                break;
            default :
                throw new ArgumentException($"invalid status: {dto.Status}. must be confirmed or cancelled");
        }
        await _unitOfWork.CourseRegistrations.UpdateAsync(registration); 
    }

    public async Task DeleteRegistrationsAsync(Guid id)
    {
        var registration = await _unitOfWork.CourseRegistrations.GetByIdAsync(id);
        if (registration == null)
            throw new KeyNotFoundException($"registration with id {id} not found.");

        var occasion = await _unitOfWork.CourseOccasions.GetByIdAsync(registration.CourseOccasionId);
        if (occasion != null)
        {
            occasion.CancelRegistration();
            await _unitOfWork.CourseOccasions.UpdateAsync(occasion);
        }

        await _unitOfWork.CourseRegistrations.DeleteAsync(id);
    }

    public async Task<IEnumerable<CourseRegistrationDto>> GetRegistrationByParticipantAsync(Guid participantId)
    {
        var registrations = await _unitOfWork.CourseRegistrations.GetRegistrationByParticipantAsync(participantId);
        return await MapToDtoList(registrations);
    }

    public async Task<IEnumerable<CourseRegistrationDto>> GetRegistrationByOccasionAsync(Guid occasionId)
    {
        var registrations = await _unitOfWork.CourseRegistrations.GetRegistrationByOccasionAsync(occasionId);
        return await MapToDtoList(registrations);
    }

    public async Task<CourseRegistrationDto> GetRegistrationsDetailsAsync(Guid id)
    {
        var registrations = await _unitOfWork.CourseRegistrations.GetRegistrationsDetailsAsync(id);
        if (registrations == null)
            throw new KeyNotFoundException($"registration with id {id} not found.");
        return await MapToDto(registrations);
    }
    public async Task CancelRegistrationAsync(Guid id)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var registration = await _unitOfWork.CourseRegistrations.GetByIdAsync(id);
            if (registration == null)
                throw new KeyNotFoundException($"registration with id {id} not found.");

            var occasion = await _unitOfWork.CourseOccasions.GetByIdAsync(registration.CourseOccasionId);
            if (occasion == null)
                throw new KeyNotFoundException($" course occasion with id {registration.CourseOccasionId} not found.");

            registration.cancel();
            occasion.cancelRegistration();

            await _unitOfWork.CourseRegstrations.UpdateAsync(registration);
            await _unitOfWork.CourseOccasions.UpdateAsync(occasion);
            await _unitOfWork.CommitTransactionAsync();
        }
        catch 
        { 
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    private async Task<CourseRegistrationDto> MapToDto(CourseRegistration r)
    {
       var dto = new CourseRegistrationDto(
            Id = r.Id,
            ParticipantId = r.ParticipantId,
            CourseOccasionId = r.CourseOccasionId,
            Status = r.Status,
            RegistrationDate = r.RegistrationDate,
            ConfirmedAt = r.ConfirmedAt,
            CancelledAt = r.CancelledAt,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
           );
        if ( r.ParticipantId != Guid.Empty )
        {
            var p = await _unitOfWork.Participants.GetByIdAsync( r.ParticipantId );
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
        if (r.CourseOccasionId != Guid.Empty)
        {
            var O = await _unitOfWork.CourseOccasions.GetByIdAsync(r.CourseOccasionsId);
            if (O != null)
            {
                dto.CourseOccasion = await _courseoccasisonservice.MapToDto( O );
                
            }
        }
        return dto;

    }

    private async Task<IEnumerable<CourseRegistrationDto>> MapToDtoList(IEnumerable<CourseRegistration> r)
    {
        var dtos = new List<CourseRegistrationDto>();
        foreach ( var c in r )
        {
            dtos.Add (await MapToDto(c)); 
        }
        return dtos;
    }
}

