using EducationalCompany.Api.Infrastructure.Data;
using EducationalCompany.Api.Infrastructure.Repositories;
using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EducationalCompany.Infrastructure.Repositories;

// Repository contract for Participant entity
public interface IParticipantRepository : IRepository<Participant>
{
    Task<Participant> GetParticipantWithRegistrationsAsync(Guid id); // Get participant with related registrations
    Task<IEnumerable<Participant>> SearchParticipantsAsync(string searchTerm); // Search participants
}

// Repository implementation for Participant entity
public class ParticipantRepository : BaseRepository<Participant>, IParticipantRepository
{
    public ParticipantRepository(ApplicationDbContext context) : base(context)
    {
    }

    // Get participant including registrations and related course data
    public async Task<Participant> GetParticipantWithRegistrationsAsync(Guid id)
    {
        return await _context.Participants
            .Include(p => p.Registrations)
                .ThenInclude(r => r.CourseOccasion)
                    .ThenInclude(co => co.Course)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    // Search participants by name, email, or phone
    public async Task<IEnumerable<Participant>> SearchParticipantsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllAsync();

        return await _context.Participants
            .Where(p =>
                p.FirstName.Contains(searchTerm) ||
                p.LastName.Contains(searchTerm) ||
                p.Email.Contains(searchTerm) ||
                p.Phone.Contains(searchTerm))
            .ToListAsync();
    }
}