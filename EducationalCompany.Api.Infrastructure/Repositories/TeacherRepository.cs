using EducationalCompany.Api.Infrastructure.Data;
using EducationalCompany.Api.Infrastructure.Repositories;
using EducationalCompany.Domain.Entities;
using EducationalCompany.Domain.Interfaces;
using EducationalCompany.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EducationalCompany.Infrastructure.Repositories;

public interface ITeacherRepository : IRepository<Teacher>
{
    Task<Teacher> GetTeacherWithOccasionsAsync(Guid id);
    Task<IEnumerable<Teacher>> SearchTeachersAsync(string searchTerm);
}

public class TeacherRepository : BaseRepository<Teacher>, ITeacherRepository
{
    public TeacherRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Teacher> GetTeacherWithOccasionsAsync(Guid id)
    {
        return await _context.Teachers
            .Include(t => t.CourseOccasions)
                .ThenInclude(co => co.Course)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

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