using EducationalCompany.Api.Infrastructure.Data;
using EducationalCompany.Api.Infrastructure.Repositories;
using EducationalCompany.Domain.Entities;
using EducationalCompany.Domain.Interfaces;
using EducationalCompany.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EducationalCompany.Infrastructure.Repositories;

public interface ICourseRepository : IRepository<Course>
{
    Task<Course> GetCourseWithOccasionsAsync(Guid id);
    Task<IEnumerable<Course>> SearchCoursesAsync(string searchTerm);
    Task<bool> CourseNameExistsAsync(string name);
}

public class CourseRepository : BaseRepository<Course>, ICourseRepository
{
    private readonly IMemoryCache _cache;

    public CourseRepository(ApplicationDbContext context, IMemoryCache cache)
        : base(context, cache)
    {
        _cache = cache;
    }

    private string GetWithOccasionsCacheKey(Guid id) => $"course_with_occasions_{id}";
    private string GetSearchCacheKey(string term) => $"courses_search_{term}";
    private const string ALL_COURSES_KEY = "courses_all";

    public async Task<Course> GetCourseWithOccasionsAsync(Guid id)
    {
        var cacheKey = GetWithOccasionsCacheKey(id);

        if (!_cache.TryGetValue(cacheKey, out Course course))
        {
            course = await _context.Courses
                .Include(c => c.Occasions)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course != null)
            {
                _cache.Set(cacheKey, course, TimeSpan.FromMinutes(60));
            }
        }

        return course;
    }

    public async Task<IEnumerable<Course>> SearchCoursesAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllAsync();

        var cacheKey = GetSearchCacheKey(searchTerm);

        if (!_cache.TryGetValue(cacheKey, out IEnumerable<Course> courses))
        {
            courses = await _context.Courses
                .Where(c => c.Name.Contains(searchTerm) || c.Description.Contains(searchTerm))
                .ToListAsync();

            _cache.Set(cacheKey, courses, TimeSpan.FromMinutes(2));
        }

        return courses;
    }

    public async Task<bool> CourseNameExistsAsync(string name)
    {
        return await _context.Courses.AnyAsync(c => c.Name == name);
    }
}