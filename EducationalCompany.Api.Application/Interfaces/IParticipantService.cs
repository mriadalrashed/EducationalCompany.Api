using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Domain.Entities;

namespace EducationalCompany.Api.Application.Interfaces
{
    public interface IParticipantService
    {
        Task<IEnumerable<ParticipantDto>> GetAllIParticipantsAsync();
        Task<ParticipantDto> GetParticipantByIdAsync(Guid id);
        Task<ParticipantDto> CreateParticipantAsync(CreateParticipantDto createTeacherDto);
        Task<ParticipantDto> UpdateParticipantAsync(UpdateParticipantDto updateTeacherDto);
        Task<bool> DeleteParticipantAsync(Guid id);

        Task<IEnumerable<ParticipantDto>> SearchParticipantsAsync(string searchTerm);
        Task<ParticipantDto> GetParticipantWithRegistrationsAsync(Guid id);
    }
}
