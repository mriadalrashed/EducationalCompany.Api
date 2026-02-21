using EducationalCompany.Api.Infrastructure.Data;
using EducationalCompany.Api.Infrastructure.Repositories;
using EducationalCompany.Domain.Entities;
using EducationalCompany.Domain.Interfaces;
using EducationalCompany.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EducationalCompany.Infrastructure.Repositories;

public interface ICourseRegistrationRepository : IRepository<CourseRegistration>
{
    Task<IEnumerable<CourseRegistration>> GetRegistrationsByParticipantAsync(Guid participantId);
    Task<IEnumerable<CourseRegistration>> GetRegistrationsByOccasionAsync(Guid occasionId);
    Task<CourseRegistration> GetRegistrationDetailsAsync(Guid id);
    Task<bool> HasRegistrationAsync(Guid participantId, Guid occasionId);
}

public class CourseRegistrationRepository : BaseRepository<CourseRegistration>, ICourseRegistrationRepository
{
    public CourseRegistrationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CourseRegistration>> GetRegistrationsByParticipantAsync(Guid participantId)
    {
        return await _context.CourseRegistrations
            .Where(r => r.ParticipantId == participantId)
            .Include(r => r.CourseOccasion)
                .ThenInclude(co => co.Course)
            .ToListAsync();
    }

    public async Task<IEnumerable<CourseRegistration>> GetRegistrationsByOccasionAsync(Guid occasionId)
    {
        return await _context.CourseRegistrations
            .Where(r => r.CourseOccasionId == occasionId)
            .Include(r => r.Participant)
            .ToListAsync();
    }

    public async Task<CourseRegistration> GetRegistrationDetailsAsync(Guid id)
    {
        return await _context.CourseRegistrations
            .Include(r => r.Participant)
            .Include(r => r.CourseOccasion)
                .ThenInclude(co => co.Course)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<bool> HasRegistrationAsync(Guid participantId, Guid occasionId)
    {
        return await _context.CourseRegistrations
            .AnyAsync(r => r.ParticipantId == participantId && r.CourseOccasionId == occasionId);
    }
}