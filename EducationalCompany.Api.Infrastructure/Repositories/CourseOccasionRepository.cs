using EducationalCompany.Api.Infrastructure.Data;
using EducationalCompany.Api.Infrastructure.Repositories;
using EducationalCompany.Domain.Entities;
using EducationalCompany.Domain.Interfaces;
using EducationalCompany.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EducationalCompany.Infrastructure.Repositories;

// Repository contract for CourseOccasion entity
public interface ICourseOccasionRepository : IRepository<CourseOccasion>
{
    Task<IEnumerable<CourseOccasion>> GetByCourseIdAsync(Guid courseId); // Get occasions by course
    Task<IEnumerable<CourseOccasion>> GetUpcomingOccasionsAsync(); // Get upcoming occasions
    Task<CourseOccasion> GetWithRegistrationsAsync(Guid id); // Get occasion with registrations
    Task<bool> IsOccasionFullAsync(Guid id); // Check if occasion is full
}

// Repository implementation with caching
public class CourseOccasionRepository : BaseRepository<CourseOccasion>, ICourseOccasionRepository
{
    private readonly IMemoryCache _cache;

    public CourseOccasionRepository(ApplicationDbContext context, IMemoryCache cache)
        : base(context, cache)
    {
        _cache = cache;
    }

    // Cache key generators
    private string GetByCourseCacheKey(Guid courseId) => $"course_occasions_{courseId}";
    private const string UPCOMING_CACHE_KEY = "occasions_upcoming";
    private string GetWithRegsCacheKey(Guid id) => $"occasion_with_regs_{id}";
    private string GetIsFullCacheKey(Guid id) => $"occasion_full_{id}";

    // Get all occasions for a specific course (with caching)
    public async Task<IEnumerable<CourseOccasion>> GetByCourseIdAsync(Guid courseId)
    {
        var cacheKey = GetByCourseCacheKey(courseId);

        if (!_cache.TryGetValue(cacheKey, out IEnumerable<CourseOccasion> occasions))
        {
            occasions = await _context.CourseOccasions
                .Where(co => co.CourseId == courseId)
                .Include(co => co.Teacher)
                .ToListAsync();

            _cache.Set(cacheKey, occasions, TimeSpan.FromMinutes(10)); // Cache for 10 minutes
        }

        return occasions;
    }

    // Get upcoming occasions ordered by start date (with caching)
    public async Task<IEnumerable<CourseOccasion>> GetUpcomingOccasionsAsync()
    {
        if (!_cache.TryGetValue(UPCOMING_CACHE_KEY, out IEnumerable<CourseOccasion> occasions))
        {
            occasions = await _context.CourseOccasions
                .Where(co => co.StartDate > DateTime.UtcNow)
                .Include(co => co.Course)
                .Include(co => co.Teacher)
                .OrderBy(co => co.StartDate)
                .ToListAsync();

            _cache.Set(UPCOMING_CACHE_KEY, occasions, TimeSpan.FromMinutes(5)); // Cache for 5 minutes
        }

        return occasions;
    }

    // Get occasion including registrations and related data (with caching)
    public async Task<CourseOccasion> GetWithRegistrationsAsync(Guid id)
    {
        var cacheKey = GetWithRegsCacheKey(id);

        if (!_cache.TryGetValue(cacheKey, out CourseOccasion occasion))
        {
            occasion = await _context.CourseOccasions
                .Include(co => co.Registrations)
                    .ThenInclude(r => r.Participant)
                .Include(co => co.Course)
                .Include(co => co.Teacher)
                .FirstOrDefaultAsync(co => co.Id == id);

            if (occasion != null)
            {
                _cache.Set(cacheKey, occasion, TimeSpan.FromMinutes(5)); // Cache for 5 minutes
            }
        }

        return occasion;
    }

    // Check if an occasion is full (with short-term caching)
    public async Task<bool> IsOccasionFullAsync(Guid id)
    {
        var cacheKey = GetIsFullCacheKey(id);

        if (!_cache.TryGetValue(cacheKey, out bool isFull))
        {
            var occasion = await _context.CourseOccasions
                .FirstOrDefaultAsync(co => co.Id == id);

            isFull = occasion?.IsFull ?? false;

            _cache.Set(cacheKey, isFull, TimeSpan.FromMinutes(1)); // Cache for 1 minute
        }

        return isFull;
    }
}