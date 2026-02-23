using EducationalCompany.Api.Infrastructure.Data;
using EducationalCompany.Api.Infrastructure.Repositories;
using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EducationalCompany.Infrastructure.Repositories;

// Repository contract for Teacher entity
public interface ITeacherRepository : IRepository<Teacher>
{
    Task<Teacher> GetTeacherWithOccasionsAsync(Guid id); // Get teacher with assigned course occasions
    Task<IEnumerable<Teacher>> SearchTeachersAsync(string searchTerm); // Search teachers
}

// Repository implementation for Teacher entity
public class TeacherRepository : BaseRepository<Teacher>, ITeacherRepository
{
    public TeacherRepository(ApplicationDbContext context) : base(context)
    {
    }

    // Get teacher including related course occasions and course details
    public async Task<Teacher> GetTeacherWithOccasionsAsync(Guid id)
    {
        return await _context.Teachers
            .Include(t => t.CourseOccasions)
                .ThenInclude(co => co.Course)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    // Search teachers by name, specialization, or email
    public async Task<IEnumerable<Teacher>> SearchTeachersAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllAsync();

        return await _context.Teachers
            .Where(t =>
                t.FirstName.Contains(searchTerm) ||
                t.LastName.Contains(searchTerm) ||
                t.Specialization.Contains(searchTerm) ||
                t.Email.Contains(searchTerm))
            .ToListAsync();
    }
}