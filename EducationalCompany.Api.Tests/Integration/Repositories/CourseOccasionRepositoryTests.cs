// Note:
// AI-assisted tools were used to help generate and structure parts of these unit tests.
// All tests have been reviewed, validated, and verified manually to ensure correctness
// and proper coverage of the intended functionality.

using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Infrastructure.Data;
using EducationalCompany.Api.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EducationalCompany.Tests.Integration.Repositories
{
    public class CourseOccasionRepositoryTests : IAsyncLifetime
    {
        private ApplicationDbContext _context;
        private CourseOccasionRepository _repository;
        private IMemoryCache _memoryCache;

        public async Task InitializeAsync()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"TestDatabase_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _repository = new CourseOccasionRepository(_context, _memoryCache);

            await _context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
            _memoryCache.Dispose();
        }

        private async Task<Course> CreateTestCourseAsync(string name = "Test Course")
        {
            var course = new Course(name, "Test Description", 40, 1000m);
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            return course;
        }

        private async Task<Teacher> CreateTestTeacherAsync()
        {
            var teacher = new Teacher("John", "Doe", "john.teacher@test.com", "1234567890", "Mathematics");
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();
            return teacher;
        }

        private async Task<Participant> CreateTestParticipantAsync(string email = null)
        {
            var participant = new Participant(
                "Test",
                "User",
                email ?? $"test.{Guid.NewGuid()}@example.com",
                "1234567890",
                "Test Address");
            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();
            return participant;
        }

        [Fact]
        public async Task AddAsync_ValidCourseOccasion_ShouldAddToDatabase()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = new CourseOccasion(
                course.Id,
                DateTime.UtcNow.AddDays(10),
                DateTime.UtcNow.AddDays(20),
                30);

            // Act
            await _repository.AddAsync(occasion);
            await _context.SaveChangesAsync();

            // Assert
            var savedOccasion = await _context.CourseOccasions.FindAsync(occasion.Id);
            savedOccasion.ShouldNotBeNull();
            savedOccasion.CourseId.ShouldBe(course.Id);
            savedOccasion.MaxParticipants.ShouldBe(30);
            savedOccasion.CurrentParticipants.ShouldBe(0);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingOccasion_ShouldReturnOccasion()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = new CourseOccasion(
                course.Id,
                DateTime.UtcNow.AddDays(10),
                DateTime.UtcNow.AddDays(20),
                30);

            _context.CourseOccasions.Add(occasion);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(occasion.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(occasion.Id);
            result.CourseId.ShouldBe(course.Id);
        }

        [Fact]
        public async Task GetByCourseIdAsync_WithMultipleOccasions_ShouldReturnCourseOccasions()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasions = new[]
            {
                new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 30),
                new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(40), 25),
                new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(50), DateTime.UtcNow.AddDays(60), 20)
            };

            _context.CourseOccasions.AddRange(occasions);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _repository.GetByCourseIdAsync(course.Id)).ToList();

            // Assert
            results.Count.ShouldBe(3);
            results.All(o => o.CourseId == course.Id).ShouldBeTrue();
        }

        [Fact]
        public async Task GetUpcomingOccasionsAsync_ShouldReturnOnlyFutureOccasions()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasions = new[]
            {
                new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-5), 30), // Past
                new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 25),  // Future
                new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(40), 20)   // Future
            };

            _context.CourseOccasions.AddRange(occasions);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _repository.GetUpcomingOccasionsAsync()).ToList();

            // Assert
            results.Count.ShouldBe(2);
            results.All(o => o.StartDate > DateTime.UtcNow).ShouldBeTrue();
        }

        [Fact]
        public async Task GetWithRegistrationsAsync_ShouldIncludeRegistrationsAndParticipants()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 30);

            _context.CourseOccasions.Add(occasion);
            await _context.SaveChangesAsync();

            var participant1 = await CreateTestParticipantAsync("john@test.com");
            var participant2 = await CreateTestParticipantAsync("jane@test.com");

            // Create registrations and update occasion's participant count
            var registration1 = new CourseRegistration(participant1.Id, occasion.Id);
            var registration2 = new CourseRegistration(participant2.Id, occasion.Id);

            _context.CourseRegistrations.AddRange(registration1, registration2);

            // Update occasion's current participants
            occasion.TryRegisterParticipant();
            occasion.TryRegisterParticipant();

            await _context.SaveChangesAsync();

            // Clear cache to force fresh load
            _memoryCache.Remove($"occasion_with_regs_{occasion.Id}");

            // Act
            var result = await _repository.GetWithRegistrationsAsync(occasion.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Registrations.ShouldNotBeNull();
            result.Registrations.Count.ShouldBe(2);
            result.CurrentParticipants.ShouldBe(2);
        }

        [Fact]
        public async Task IsOccasionFullAsync_WhenNotFull_ShouldReturnFalse()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 2); // Max 2

            _context.CourseOccasions.Add(occasion);
            await _context.SaveChangesAsync();

            // Add one participant
            var participant = await CreateTestParticipantAsync();
            var registration = new CourseRegistration(participant.Id, occasion.Id);
            _context.CourseRegistrations.Add(registration);

            // Update occasion's participant count
            occasion.TryRegisterParticipant();
            await _context.SaveChangesAsync();

            // Clear cache
            _memoryCache.Remove($"occasion_full_{occasion.Id}");

            // Act
            var result = await _repository.IsOccasionFullAsync(occasion.Id);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task IsOccasionFullAsync_WhenFull_ShouldReturnTrue()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 1); // Max 1

            _context.CourseOccasions.Add(occasion);
            await _context.SaveChangesAsync();

            // Add one participant (reaching max)
            var participant = await CreateTestParticipantAsync();
            var registration = new CourseRegistration(participant.Id, occasion.Id);
            _context.CourseRegistrations.Add(registration);

            // Update occasion's participant count
            occasion.TryRegisterParticipant();
            await _context.SaveChangesAsync();

            // Clear cache
            _memoryCache.Remove($"occasion_full_{occasion.Id}");

            // Act
            var result = await _repository.IsOccasionFullAsync(occasion.Id);

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateOccasionDetails()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 30);

            _context.CourseOccasions.Add(occasion);
            await _context.SaveChangesAsync();

            // Update
            occasion.UpdateDetails(
                DateTime.UtcNow.AddDays(15),
                DateTime.UtcNow.AddDays(25),
                25);

            // Act
            await _repository.UpdateAsync(occasion);
            await _context.SaveChangesAsync();

            // Assert
            var updated = await _repository.GetByIdAsync(occasion.Id);
            updated.ShouldNotBeNull();
            updated.MaxParticipants.ShouldBe(25);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveOccasion()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 30);

            _context.CourseOccasions.Add(occasion);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(occasion.Id);
            await _context.SaveChangesAsync();

            // Assert
            var deleted = await _repository.GetByIdAsync(occasion.Id);
            deleted.ShouldBeNull();
        }
    }
}