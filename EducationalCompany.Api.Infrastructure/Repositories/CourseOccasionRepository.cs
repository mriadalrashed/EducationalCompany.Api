using EducationalCompany.Api.Infrastructure.Data;
using EducationalCompany.Api.Infrastructure.Repositories;
using EducationalCompany.Domain.Entities;
using EducationalCompany.Domain.Interfaces;
using EducationalCompany.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EducationalCompany.Infrastructure.Repositories;

public interface ICourseOccasionRepository : IRepository<CourseOccasion>
{
    Task<IEnumerable<CourseOccasion>> GetByCourseIdAsync(Guid courseId);
    Task<IEnumerable<CourseOccasion>> GetUpcomingOccasionsAsync();
    Task<CourseOccasion> GetWithRegistrationsAsync(Guid id);
    Task<bool> IsOccasionFullAsync(Guid id);
}

public class CourseOccasionRepository : BaseRepository<CourseOccasion>, ICourseOccasionRepository
{
    private readonly IMemoryCache _cache;

    public CourseOccasionRepository(ApplicationDbContext context, IMemoryCache cache)
    : base(context, cache)
    {
        _cache = cache;
    }

    private string GetByCourseCacheKey(Guid courseId) => $"course_occasions_{courseId}";
    private const string UPCOMING_CACHE_KEY = "occasions_upcoming";
    private string GetWithRegsCacheKey(Guid id) => $"occasion_with_regs_{id}";
    private string GetIsFullCacheKey(Guid id) => $"occasion_full_{id}";

    public async Task<IEnumerable<CourseOccasion>> GetByCourseIdAsync(Guid courseId)
    {
        var cacheKey = GetByCourseCacheKey(courseId);

        if (!_cache.TryGetValue(cacheKey, out IEnumerable<CourseOccasion> occasions))
        {
            occasions = await _context.CourseOccasions
                .Where(co => co.CourseId == courseId)
                .Include(co => co.Teacher)
                .ToListAsync();

            _cache.Set(cacheKey, occasions, TimeSpan.FromMinutes(10));
        }

        return occasions;
    }

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

            _cache.Set(UPCOMING_CACHE_KEY, occasions, TimeSpan.FromMinutes(5));
        }

        return occasions;
    }

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
                _cache.Set(cacheKey, occasion, TimeSpan.FromMinutes(5));
            }
        }

        return occasion;
    }

    public async Task<bool> IsOccasionFullAsync(Guid id)
    {
        var cacheKey = GetIsFullCacheKey(id);

        if (!_cache.TryGetValue(cacheKey, out bool isFull))
        {
            var occasion = await _context.CourseOccasions
                .FirstOrDefaultAsync(co => co.Id == id);

            isFull = occasion?.IsFull ?? false;

            _cache.Set(cacheKey, isFull, TimeSpan.FromMinutes(1));
        }

        return isFull;
    }
}