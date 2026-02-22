using EducationalCompany.Api.Domain.Interfaces;
using EducationalCompany.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EducationalCompany.Api.Infrastructure.Repositories
{
    // Generic base repository for common CRUD operations
    public abstract class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;
        protected readonly IMemoryCache _cache;

        // Constructor receives DbContext and optional cache
        protected BaseRepository(ApplicationDbContext context, IMemoryCache? cache = null)
        {
            _context = context;
            _dbSet = _context.Set<T>();
            _cache = cache;
        }

        // Get entity by Id (with optional caching)
        public virtual async Task<T> GetByIdAsync(Guid id)
        {
            if (_cache != null)
            {
                var cachekey = $"{typeof(T).Name}_{id}";

                if (!_cache.TryGetValue(cachekey, out T entity))
                {
                    entity = await _dbSet.FindAsync(id)!;

                    if (entity != null)
                    {
                        _cache.Set(cachekey, entity, TimeSpan.FromMinutes(30)); // Cache for 30 minutes
                    }
                }

                return await Task.FromResult(entity);
            }

            return await _dbSet.FindAsync(id)!;
        }

        // Get all entities (with optional caching)
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            if (_cache != null)
            {
                var cachekey = $"{typeof(T).Name}_all";

                if (!_cache.TryGetValue(cachekey, out IEnumerable<T> entities))
                {
                    entities = await _dbSet.ToListAsync();
                    _cache.Set(cachekey, entities, TimeSpan.FromMinutes(30)); // Cache for 30 minutes
                }

                return entities;
            }

            return await _dbSet.ToListAsync();
        }

        // Add new entity
        public virtual async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();

            // Clear cache after insert
            if (_cache != null)
                _cache.Remove($"{typeof(T).Name}_all");
        }

        // Update existing entity
        public virtual async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();

            // Clear related cache after update
            if (_cache != null)
            {
                var id = (Guid)entity.GetType().GetProperty("Id")!.GetValue(entity);
                _cache.Remove($"{typeof(T).Name}_all");
                _cache.Remove($"{typeof(T).Name}_{id}");
            }
        }

        // Delete entity by Id
        public virtual async Task DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id);

            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();

                // Clear related cache after delete
                if (_cache != null)
                {
                    _cache.Remove($"{typeof(T).Name}_all");
                    _cache.Remove($"{typeof(T).Name}_{id}");
                }
            }
        }

        // Check if entity exists
        public virtual async Task<bool> ExistsAsync(Guid id)
        {
            return await _dbSet.AnyAsync(e => EF.Property<Guid>(e, "Id") == id);
        }
    }
}