using EducationalCompany.Api.Infrastructure.Data;
using EducationalCompany.Api.Infrastructure.Repositories;
using EducationalCompany.Domain.Entities;
using EducationalCompany.Domain.Interfaces;
using EducationalCompany.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EducationalCompany.Infrastructure.Repositories;

public interface IParticipantRepository : IRepository<Participant>
{
    Task<Participant> GetParticipantWithRegistrationsAsync(Guid id);
    Task<IEnumerable<Participant>> SearchParticipantsAsync(string searchTerm);
}

public class ParticipantRepository : BaseRepository<Participant>, IParticipantRepository
{
    public ParticipantRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Participant> GetParticipantWithRegistrationsAsync(Guid id)
    {
        return await _context.Participants
            .Include(p => p.Registrations)
                .ThenInclude(r => r.CourseOccasion)
                    .ThenInclude(co => co.Course)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

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