using EducationalCompany.Api.Domain.Interfaces;
using EducationalCompany.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;


    namespace EducationalCompany.Api.Infrastructure.Repositories
    {
        public abstract class BaseRepository<T> : IRepository<T> where T : class
        {
            protected readonly ApplicationDbContext _context;
            protected readonly DbSet<T> _dbSet;
            protected readonly IMemoryCache _cache;


            protected BaseRepository (ApplicationDbContext context,IMemoryCache? cache=null)
            {
                _context = context;
                _dbSet = _context.Set<T>();
                _cache = cache;
            }

            public virtual async Task<T> GetByIdAsync(Guid id)
            {
                if( _cache != null)
                 { var cachekey = $"{typeof(T).Name}_{id}";
                if (!_cache.TryGetValue(cachekey, out T entity))
                {
                        entity = await _dbSet.FindAsync(id)!;
                        if (entity != null)
                        {
                            _cache.Set(cachekey, entity, TimeSpan.FromMinutes(30));
                        }
                    }
                    return await Task.FromResult(entity);
                }

                return await _dbSet.FindAsync(id)!;
            }

            public virtual async Task<IEnumerable<T>> GetAllAsync()
            {
                if ( _cache != null )
                {
                    var cachekey = $"{typeof(T).Name}_all";
                    if(!_cache.TryGetValue(cachekey,out IEnumerable<T> entities))
                    {
                        entities = await _dbSet.ToListAsync();
                        _cache.Set(cachekey, entities ,TimeSpan.FromMinutes(30));
                    }
                    return entities;
                }
                return await _dbSet.ToListAsync() ;
            }

            public virtual async Task AddAsync(T entity)
            { 
                await _dbSet.AddAsync(entity);
                await _context.SaveChangesAsync();
                if ( _cache != null )
                    _cache.Remove($"{typeof(T).Name}_all"); 
            }

            public virtual async Task UpdateAsync(T entity)
            {
                _dbSet.Update(entity);
                await _context.SaveChangesAsync();
                if (_cache != null)
                {
                    var id = (Guid)entity.GetType().GetProperty("Id")!.GetValue(entity);
                    _cache.Remove($"{typeof(T).Name}_all");
                    _cache.Remove($"{typeof(T).Name}_{id}");
                }
            }

            public virtual async Task DeleteAsync(Guid id)
            {
                var entity = await GetByIdAsync(id);
                if ( entity != null )
                {
                    _dbSet.Remove(entity);
                    await _context.SaveChangesAsync();
                    if (_cache != null)
                    {
                        _cache.Remove($"{typeof(T).Name}_all");
                        _cache.Remove($"{typeof(T).Name}_{id}");
                    }
                }
            }
            public virtual async Task<bool> ExistsAsync(Guid id)
            {
                return await _dbSet.AnyAsync(e => EF.Property<Guid>(e, "Id") == id);
            }

        }

    }

