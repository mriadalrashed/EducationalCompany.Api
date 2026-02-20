using EducationalCompany.Api.Infrastructure.Repositories;
using EducationalCompany.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;


namespace EducationalCompany.Api.Infrastructure
{
    public interface IUnitofWorkIUnitofWork : IDisposable
    {
        ICourseRepository Courses{ get; }

        ICourseOccasionRepository CourseOccasions { get; }

        ICourseRegistrationRepository CourseRegistrations { get; }

        IParticipantRepository Participants { get; }

        ITeacherRepository Teachers { get; }

        Task<int> CompleteAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }

    public class UnitofWork : IUnitofWork
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private IDbContextTransaction _currentTransaction;

        public UnitofWork(ApplicationDbContext context , IMemoryCache cache)
        {
            _context = context;
            _cache = Cache;
            Courses = new CourseRepository(_context);
            CourseOccasions = new CourseOccasionRepository(_context);
            CourseRegistrations = new CourseRegistrationRepository(_context);
            Participants = new ParticipantRepository(_context);
            Teachers = new TeacherRepository(_context);
        }

        public ICourseRepository Courses { get; }

        public ICourseOccasionRepository CourseOccasions { get; }

        public ICourseRegistrationRepository CourseRegistrations { get; }

        public IParticipantRepository Participants { get; }

        public ITeacherRepository Teachers { get; }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                return;
            }
            _currentTransaction = await _context.Database.BeginTransactionAsync();
        }
        public async Task CommitTransactionAsync()
        {
            try
            {
                await CompleteAsync();
                await (_currentTransaction?.CommitAsync() ?? Task.CompletedTask);
            }
            catch
            {
               await RollbackTransactionAsync();
               throw;
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            try
            {
                await (_currentTransaction?.RollbackAsync() ?? Task.CompletedTask);
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        public void Dispose()
        {
            _context.Dispose();
            _currentTransaction?.Dispose();
        }



    }




    
}
