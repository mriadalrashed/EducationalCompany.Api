using EducationalCompany.Api.Infrastructure.Data;
using EducationalCompany.Api.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;

namespace EducationalCompany.Api.Infrastructure
{
    // UnitOfWork contract for managing repositories and transactions
    public interface IUnitOfWork : IDisposable
    {
        ICourseRepository Courses { get; } // Course repository
        ICourseOccasionRepository CourseOccasions { get; } // Course occasion repository
        ICourseRegistrationRepository CourseRegistrations { get; } // Registration repository
        IParticipantRepository Participants { get; } // Participant repository
        ITeacherRepository Teachers { get; } // Teacher repository

        Task<int> CompleteAsync(); // Save changes
        Task BeginTransactionAsync(); // Start transaction
        Task CommitTransactionAsync(); // Commit transaction
        Task RollbackTransactionAsync(); // Rollback transaction
    }

    // UnitOfWork implementation
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private IDbContextTransaction _currentTransaction;

        // Constructor initializes DbContext and repositories
        public UnitOfWork(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;

            // Initialize repositories
            Courses = new CourseRepository(_context, _cache);
            CourseOccasions = new CourseOccasionRepository(_context, _cache);
            CourseRegistrations = new CourseRegistrationRepository(_context);
            Participants = new ParticipantRepository(_context);
            Teachers = new TeacherRepository(_context);
        }

        public ICourseRepository Courses { get; }
        public ICourseOccasionRepository CourseOccasions { get; }
        public ICourseRegistrationRepository CourseRegistrations { get; }
        public IParticipantRepository Participants { get; }
        public ITeacherRepository Teachers { get; }

        // Save all changes to database
        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        // Begin database transaction
        public async Task BeginTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                return;
            }

            _currentTransaction = await _context.Database.BeginTransactionAsync();
        }

        // Commit transaction
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

        // Rollback transaction
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

        // Dispose resources
        public void Dispose()
        {
            _context.Dispose();
            _currentTransaction?.Dispose();
        }
    }
}