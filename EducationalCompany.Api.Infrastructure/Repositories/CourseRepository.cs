using EducationalCompany.Api.Infrastructure.Data;
using EducationalCompany.Api.Infrastructure.Repositories;
using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EducationalCompany.Infrastructure.Repositories;

// Repository contract for Course entity
public interface ICourseRepository : IRepository<Course>
{
    Task<Course> GetCourseWithOccasionsAsync(Guid id); // Get course with related occasions
    Task<IEnumerable<Course>> SearchCoursesAsync(string searchTerm); // Search courses by name or description
    Task<bool> CourseNameExistsAsync(string name); // Check if course name already exists
}

// Repository implementation for Course entity
public class CourseRepository : BaseRepository<Course>, ICourseRepository
{
    private readonly IMemoryCache _cache;

    public CourseRepository(ApplicationDbContext context, IMemoryCache cache)
        : base(context, cache)
    {
        _cache = cache;
    }

    // Generate cache key for course with occasions
    private string GetWithOccasionsCacheKey(Guid id) => $"course_with_occasions_{id}";

    // Generate cache key for search results
    private string GetSearchCacheKey(string term) => $"courses_search_{term}";

    // Cache key for all courses
    private const string ALL_COURSES_KEY = "courses_all";

    // Get course including related occasions (with caching)
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
                _cache.Set(cacheKey, course, TimeSpan.FromMinutes(60)); // Cache for 60 minutes
            }
        }

        return course;
    }

    // Search courses with short-term caching
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

            _cache.Set(cacheKey, courses, TimeSpan.FromMinutes(2)); // Cache search results for 2 minutes
        }

        return courses;
    }

    // Check if a course name already exists in database
    public async Task<bool> CourseNameExistsAsync(string name)
    {
        return await _context.Courses.AnyAsync(c => c.Name == name);
    }
}