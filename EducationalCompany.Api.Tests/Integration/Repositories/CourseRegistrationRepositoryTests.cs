// Note:
// AI-assisted tools were used to help generate and structure parts of these unit tests.
// All tests have been reviewed, validated, and verified manually to ensure correctness
// and proper coverage of the intended functionality.

using EducationalCompany.Domain.Entities;
using EducationalCompany.Infrastructure.Data;
using EducationalCompany.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EducationalCompany.Tests.Integration.Repositories
{
    public class CourseRegistrationRepositoryTests : IAsyncLifetime
    {
        private ApplicationDbContext _context;
        private CourseRegistrationRepository _repository;

        public async Task InitializeAsync()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"TestDatabase_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new CourseRegistrationRepository(_context);

            await _context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }

        private async Task<Course> CreateTestCourseAsync(string name = "Test Course")
        {
            var course = new Course(name, "Test Description", 40, 1000m);
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            return course;
        }

        private async Task<CourseOccasion> CreateTestOccasionAsync(Guid courseId, int maxParticipants = 30)
        {
            var occasion = new CourseOccasion(
                courseId,
                DateTime.UtcNow.AddDays(10),
                DateTime.UtcNow.AddDays(20),
                maxParticipants);
            _context.CourseOccasions.Add(occasion);
            await _context.SaveChangesAsync();
            return occasion;
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
        public async Task AddAsync_ValidRegistration_ShouldAddToDatabase()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = await CreateTestOccasionAsync(course.Id);
            var participant = await CreateTestParticipantAsync();

            var registration = new CourseRegistration(participant.Id, occasion.Id);

            // Act
            await _repository.AddAsync(registration);
            await _context.SaveChangesAsync();

            // Assert
            var savedRegistration = await _context.CourseRegistrations.FindAsync(registration.Id);
            savedRegistration.ShouldNotBeNull();
            savedRegistration.ParticipantId.ShouldBe(participant.Id);
            savedRegistration.CourseOccasionId.ShouldBe(occasion.Id);
            savedRegistration.Status.ShouldBe("Pending"); // Assuming default status
        }

        [Fact]
        public async Task GetByIdAsync_ExistingRegistration_ShouldReturnRegistration()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = await CreateTestOccasionAsync(course.Id);
            var participant = await CreateTestParticipantAsync();

            var registration = new CourseRegistration(participant.Id, occasion.Id);
            _context.CourseRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(registration.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(registration.Id);
            result.ParticipantId.ShouldBe(participant.Id);
            result.CourseOccasionId.ShouldBe(occasion.Id);
        }

        [Fact]
        public async Task GetAllAsync_WithMultipleRegistrations_ShouldReturnAll()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = await CreateTestOccasionAsync(course.Id);
            var participant1 = await CreateTestParticipantAsync("user1@test.com");
            var participant2 = await CreateTestParticipantAsync("user2@test.com");
            var participant3 = await CreateTestParticipantAsync("user3@test.com");

            var registrations = new[]
            {
                new CourseRegistration(participant1.Id, occasion.Id),
                new CourseRegistration(participant2.Id, occasion.Id),
                new CourseRegistration(participant3.Id, occasion.Id)
            };

            _context.CourseRegistrations.AddRange(registrations);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _repository.GetAllAsync()).ToList();

            // Assert
            results.Count.ShouldBe(3);
        }

        [Fact]
        public async Task GetRegistrationsByParticipantAsync_ShouldReturnParticipantRegistrations()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion1 = await CreateTestOccasionAsync(course.Id);
            var occasion2 = await CreateTestOccasionAsync(course.Id);
            var participant = await CreateTestParticipantAsync();

            var registrations = new[]
            {
                new CourseRegistration(participant.Id, occasion1.Id),
                new CourseRegistration(participant.Id, occasion2.Id)
            };

            _context.CourseRegistrations.AddRange(registrations);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _repository.GetRegistrationsByParticipantAsync(participant.Id)).ToList();

            // Assert
            results.Count.ShouldBe(2);
            results.All(r => r.ParticipantId == participant.Id).ShouldBeTrue();
        }

        [Fact]
        public async Task GetRegistrationsByOccasionAsync_ShouldReturnOccasionRegistrations()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = await CreateTestOccasionAsync(course.Id);
            var participant1 = await CreateTestParticipantAsync("user1@test.com");
            var participant2 = await CreateTestParticipantAsync("user2@test.com");

            var registrations = new[]
            {
                new CourseRegistration(participant1.Id, occasion.Id),
                new CourseRegistration(participant2.Id, occasion.Id)
            };

            _context.CourseRegistrations.AddRange(registrations);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _repository.GetRegistrationsByOccasionAsync(occasion.Id)).ToList();

            // Assert
            results.Count.ShouldBe(2);
            results.All(r => r.CourseOccasionId == occasion.Id).ShouldBeTrue();
        }

        [Fact]
        public async Task GetRegistrationDetailsAsync_ShouldIncludeParticipantAndCourseOccasion()
        {
            // Arrange
            var course = await CreateTestCourseAsync("Math Course");
            var occasion = await CreateTestOccasionAsync(course.Id);
            var participant = await CreateTestParticipantAsync("student@test.com");

            var registration = new CourseRegistration(participant.Id, occasion.Id);
            _context.CourseRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetRegistrationDetailsAsync(registration.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Participant.ShouldNotBeNull();
            result.Participant.Email.ShouldBe("student@test.com");
            result.CourseOccasion.ShouldNotBeNull();
            result.CourseOccasion.Course.ShouldNotBeNull();
            result.CourseOccasion.Course.Name.ShouldBe("Math Course");
        }

        [Fact]
        public async Task HasRegistrationAsync_WhenExists_ShouldReturnTrue()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = await CreateTestOccasionAsync(course.Id);
            var participant = await CreateTestParticipantAsync();

            var registration = new CourseRegistration(participant.Id, occasion.Id);
            _context.CourseRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.HasRegistrationAsync(participant.Id, occasion.Id);

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task HasRegistrationAsync_WhenNotExists_ShouldReturnFalse()
        {
            // Arrange
            var participantId = Guid.NewGuid();
            var occasionId = Guid.NewGuid();

            // Act
            var result = await _repository.HasRegistrationAsync(participantId, occasionId);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateRegistration()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = await CreateTestOccasionAsync(course.Id);
            var participant = await CreateTestParticipantAsync();

            var registration = new CourseRegistration(participant.Id, occasion.Id);
            _context.CourseRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            // Detach to simulate fresh context
            _context.Entry(registration).State = EntityState.Detached;

            // Update
            registration.Confirm();

            // Act
            await _repository.UpdateAsync(registration);
            await _context.SaveChangesAsync();

            // Assert
            var updated = await _repository.GetByIdAsync(registration.Id);
            updated.Status.ShouldBe("Confirmed");
            updated.ConfirmedAt.ShouldNotBeNull();
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveRegistration()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = await CreateTestOccasionAsync(course.Id);
            var participant = await CreateTestParticipantAsync();

            var registration = new CourseRegistration(participant.Id, occasion.Id);
            _context.CourseRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(registration.Id);
            await _context.SaveChangesAsync();

            // Assert
            var deleted = await _repository.GetByIdAsync(registration.Id);
            deleted.ShouldBeNull();
        }
    }
}