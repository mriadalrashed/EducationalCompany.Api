using EducationalCompany.Api.Infrastructure.Data;
using EducationalCompany.Api.Infrastructure.Repositories;
using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EducationalCompany.Infrastructure.Repositories;

// Repository contract for CourseRegistration entity
public interface ICourseRegistrationRepository : IRepository<CourseRegistration>
{
    Task<IEnumerable<CourseRegistration>> GetRegistrationsByParticipantAsync(Guid participantId); // Get registrations by participant
    Task<IEnumerable<CourseRegistration>> GetRegistrationsByOccasionAsync(Guid occasionId); // Get registrations by course occasion
    Task<CourseRegistration> GetRegistrationDetailsAsync(Guid id); // Get registration with full details
    Task<bool> HasRegistrationAsync(Guid participantId, Guid occasionId); // Check if registration exists
}

// Repository implementation for CourseRegistration entity
public class CourseRegistrationRepository : BaseRepository<CourseRegistration>, ICourseRegistrationRepository
{
    public CourseRegistrationRepository(ApplicationDbContext context) : base(context)
    {
    }

    // Get all registrations for a specific participant (including course details)
    public async Task<IEnumerable<CourseRegistration>> GetRegistrationsByParticipantAsync(Guid participantId)
    {
        return await _context.CourseRegistrations
            .Where(r => r.ParticipantId == participantId)
            .Include(r => r.CourseOccasion)
                .ThenInclude(co => co.Course)
            .ToListAsync();
    }

    // Get all registrations for a specific course occasion (including participant details)
    public async Task<IEnumerable<CourseRegistration>> GetRegistrationsByOccasionAsync(Guid occasionId)
    {
        return await _context.CourseRegistrations
            .Where(r => r.CourseOccasionId == occasionId)
            .Include(r => r.Participant)
            .ToListAsync();
    }

    // Get registration with related participant and course data
    public async Task<CourseRegistration> GetRegistrationDetailsAsync(Guid id)
    {
        return await _context.CourseRegistrations
            .Include(r => r.Participant)
            .Include(r => r.CourseOccasion)
                .ThenInclude(co => co.Course)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    // Check if a participant is already registered in a course occasion
    public async Task<bool> HasRegistrationAsync(Guid participantId, Guid occasionId)
    {
        return await _context.CourseRegistrations
            .AnyAsync(r => r.ParticipantId == participantId && r.CourseOccasionId == occasionId);
    }
}