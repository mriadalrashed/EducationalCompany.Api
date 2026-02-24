using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Domain.Entities;

namespace EducationalCompany.Api.Application.Interfaces
{
    // Interface that defines operations for participant management
    public interface IParticipantService
    {
        Task<IEnumerable<ParticipantDto>> GetAllParticipantsAsync(); // Get all participants
        Task<ParticipantDto> GetParticipantByIdAsync(Guid id);  // Get participant by ID
        Task<ParticipantDto> CreateParticipantAsync(CreateParticipantDto createTeacherDto); // Create new participant
        Task UpdateParticipantAsync(Guid id, UpdateParticipantDto updateTeacherDto); // Update participant
        Task DeleteParticipantAsync(Guid id);// Delete participant

        Task<IEnumerable<ParticipantDto>> SearchParticipantsAsync(string searchTerm); // Search participants
        Task<ParticipantDto> GetParticipantWithRegistrationsAsync(Guid id); // Get participant with registrations
    }
}
