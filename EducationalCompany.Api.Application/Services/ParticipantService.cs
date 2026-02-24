using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Infrastructure;
using EducationalCompany.Api.Application.Interfaces;

namespace EducationalCompany.Api.Application.Services;

public class ParticipantService : IParticipantService
{
    private readonly IUnitOfWork _unitOfWork;

    public ParticipantService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ParticipantDto>> GetAllParticipantsAsync()
    {
        var participants = await _unitOfWork.Participants.GetAllAsync();
        return participants.Select(MapToDto);
    }

    public async Task<ParticipantDto> GetParticipantByIdAsync(Guid id)
    {
        var participant = await _unitOfWork.Participants.GetByIdAsync(id);
        if (participant == null)
            throw new KeyNotFoundException($"Participant with id {id} not found.");
        return MapToDto(participant);
    }

    public async Task<ParticipantDto> CreateParticipantAsync(CreateParticipantDto dto)
    {

        var existingParticipant = (await _unitOfWork.Participants.SearchParticipantsAsync(dto.Email)).FirstOrDefault();
        if (existingParticipant == null)
            throw new InvalidOperationException($"Participant with email {dto.Email} already exists.");

        var participant = new Participant(
            dto.FirstName,
            dto.LastName,
            dto.Email,
            dto.Phone,
            dto.Address);

        await _unitOfWork.Participants.AddAsync(participant);
        return MapToDto(participant);
    }

    public async Task UpdateParticipantAsync(Guid id, UpdateParticipantDto dto)
    {
        var participant = await _unitOfWork.Participants.GetByIdAsync(id);

        if (participant == null)
            throw new KeyNotFoundException($"participant with id {id} not found.");

        if (dto.Email != participant.Email)
        {
            var existingParticipant = (await _unitOfWork.Participants.SearchParticipantsAsync(dto.Email)).FirstOrDefault(p => p.Id != id);
            if (existingParticipant != null)
                throw new InvalidOperationException($"Participant with email {dto.Email} already exists.");
        }
        participant.Update(dto.FirstName, dto.LastName, dto.Email, dto.Phone, dto.Address);

        await _unitOfWork.Participants.UpdateAsync(participant);
    }

    public async Task DeleteParticipantAsync(Guid id)
    {
        var participant = await _unitOfWork.Participants.GetByIdAsync(id);
        if (participant == null)
            throw new KeyNotFoundException($"participant with id {id} not found.");
        var registrations = await _unitOfWork.CourseRegistrations.GetRegistrationsByParticipantAsync(id);
        if (registrations.Any())
            throw new InvalidOperationException($"Can't Delete Participant Who Has a Course Registration");

        await _unitOfWork.Participants.DeleteAsync(id);
    }

    public async Task<IEnumerable<ParticipantDto>> SearchParticipantsAsync(string searchTerm)
    {
        var participants = await _unitOfWork.Participants.SearchParticipantsAsync(searchTerm);
        return participants.Select(MapToDto);
    }

    public async Task<ParticipantDto> GetParticipantWithRegistrationsAsync(Guid id)
    {
        var participant = await _unitOfWork.Participants.GetParticipantWithRegistrationsAsync(id);
        if (participant == null)
            throw new KeyNotFoundException($"Participant with ID {id} not found");

        var dto = MapToDto(participant);

        // You can add registration details to the DTO if needed

        return dto;
    }

    private ParticipantDto MapToDto(Participant participant)
    {
        return new ParticipantDto
        {
            Id = participant.Id,
            FirstName = participant.FirstName,
            LastName = participant.LastName,
            Email = participant.Email,
            Phone = participant.Phone,
            Address = participant.Address,
            UpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
    }

}

