// Note:
// AI-assisted tools were used to help generate and structure parts of these unit tests.
// All tests have been reviewed, validated, and verified manually to ensure correctness
// and proper coverage of the intended functionality.

using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Application.Interfaces;
using EducationalCompany.Api.Application.Services;
using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Infrastructure;
using EducationalCompany.Api.Infrastructure.Data;
using EducationalCompany.Api.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EducationalCompany.Tests.Integration.Services
{
    public class CourseRegistrationServiceTests : IAsyncLifetime
    {
        private ApplicationDbContext _context;
        private IUnitOfWork _unitOfWork;
        private ICourseRegistrationService _service;
        private ICourseOccasionService _courseOccasionService;
        private ServiceProvider _serviceProvider;
        private IMemoryCache _memoryCache;
        private IServiceScope _scope;

        // Update the InitializeAsync method:

        public async Task InitializeAsync()
        {
            var services = new ServiceCollection();

            // Add in-memory database with transaction warning suppressed
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase($"TestDatabase_{Guid.NewGuid()}")
                       .ConfigureWarnings(warnings =>
                           warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            // Add memory cache
            services.AddMemoryCache();

            // Register repositories
            services.AddScoped<ICourseRepository, CourseRepository>();
            services.AddScoped<ICourseOccasionRepository, CourseOccasionRepository>();
            services.AddScoped<ITeacherRepository, TeacherRepository>();
            services.AddScoped<ICourseRegistrationRepository, CourseRegistrationRepository>();
            services.AddScoped<IParticipantRepository, ParticipantRepository>();

            // Register UnitOfWork
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Register services
            services.AddScoped<ICourseOccasionService, CourseOccasionService>();
            services.AddScoped<ICourseRegistrationService, CourseRegistrationService>();

            _serviceProvider = services.BuildServiceProvider();
            _scope = _serviceProvider.CreateScope();

            _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _unitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            _service = _scope.ServiceProvider.GetRequiredService<ICourseRegistrationService>();
            _memoryCache = _scope.ServiceProvider.GetRequiredService<IMemoryCache>();

            await _context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            if (_context != null)
            {
                await _context.Database.EnsureDeletedAsync();
                await _context.DisposeAsync();
            }

            if (_memoryCache != null)
            {
                _memoryCache.Dispose();
            }

            if (_scope != null)
            {
                _scope.Dispose();
            }

            if (_serviceProvider != null)
            {
                await _serviceProvider.DisposeAsync();
            }
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
        public async Task CreateRegistrationAsync_WithValidData_ShouldCreateRegistration()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = await CreateTestOccasionAsync(course.Id, 2); // Max 2 participants
            var participant = await CreateTestParticipantAsync();

            var createDto = new CreateCourseRegistrationDto
            {
                ParticipantId = participant.Id,
                CourseOccasionId = occasion.Id
            };

            // Act
            var result = await _service.CreateRegistrationAsync(createDto);

            // Assert
            result.ShouldNotBeNull();
            result.ParticipantId.ShouldBe(participant.Id);
            result.CourseOccasionId.ShouldBe(occasion.Id);
            result.Status.ShouldBe("Pending");

            // Verify in database
            var savedRegistration = await _unitOfWork.CourseRegistrations.GetByIdAsync(result.Id);
            savedRegistration.ShouldNotBeNull();

            // Verify occasion participant count increased
            var updatedOccasion = await _unitOfWork.CourseOccasions.GetByIdAsync(occasion.Id);
            updatedOccasion.CurrentParticipants.ShouldBe(1);
        }

        [Fact]
        public async Task CreateRegistrationAsync_WithNonExistentParticipant_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = await CreateTestOccasionAsync(course.Id);

            var createDto = new CreateCourseRegistrationDto
            {
                ParticipantId = Guid.NewGuid(),
                CourseOccasionId = occasion.Id
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.CreateRegistrationAsync(createDto));

            exception.Message.ShouldContain(createDto.ParticipantId.ToString());
        }

        [Fact]
        public async Task CreateRegistrationAsync_WithNonExistentOccasion_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var participant = await CreateTestParticipantAsync();

            var createDto = new CreateCourseRegistrationDto
            {
                ParticipantId = participant.Id,
                CourseOccasionId = Guid.NewGuid()
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.CreateRegistrationAsync(createDto));

            exception.Message.ShouldContain(createDto.CourseOccasionId.ToString());
        }

        [Fact]
        public async Task CreateRegistrationAsync_WhenOccasionIsFull_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = await CreateTestOccasionAsync(course.Id, 1); // Max 1 participant

            // Add first participant
            var participant1 = await CreateTestParticipantAsync("user1@test.com");
            var registration1 = new CourseRegistration(participant1.Id, occasion.Id);
            _context.CourseRegistrations.Add(registration1);
            occasion.TryRegisterParticipant(); // Increment count
            await _context.SaveChangesAsync();

            // Try to add second participant
            var participant2 = await CreateTestParticipantAsync("user2@test.com");
            var createDto = new CreateCourseRegistrationDto
            {
                ParticipantId = participant2.Id,
                CourseOccasionId = occasion.Id
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.CreateRegistrationAsync(createDto));

            exception.Message.ShouldContain("Course occasion is full");
        }

        [Fact]
        public async Task CreateRegistrationAsync_WhenParticipantAlreadyRegistered_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = await CreateTestOccasionAsync(course.Id);
            var participant = await CreateTestParticipantAsync();

            // Create first registration
            var registration = new CourseRegistration(participant.Id, occasion.Id);
            _context.CourseRegistrations.Add(registration);
            occasion.TryRegisterParticipant();
            await _context.SaveChangesAsync();

            // Try to create duplicate registration
            var createDto = new CreateCourseRegistrationDto
            {
                ParticipantId = participant.Id,
                CourseOccasionId = occasion.Id
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.CreateRegistrationAsync(createDto));

            exception.Message.ShouldContain("already registered");
        }

        [Fact]
        public async Task GetRegistrationByIdAsync_WithValidId_ShouldReturnRegistration()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = await CreateTestOccasionAsync(course.Id);
            var participant = await CreateTestParticipantAsync();

            var registration = new CourseRegistration(participant.Id, occasion.Id);
            _context.CourseRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetRegistrationByIdAsync(registration.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(registration.Id);
            result.ParticipantId.ShouldBe(participant.Id);
            result.CourseOccasionId.ShouldBe(occasion.Id);
        }

        [Fact]
        public async Task GetAllRegistrationsAsync_ShouldReturnAllRegistrations()
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
            var results = (await _service.GetAllRegistrationsAsync()).ToList();

            // Assert
            results.Count.ShouldBe(2);
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
            var results = (await _service.GetRegistrationsByParticipantAsync(participant.Id)).ToList();

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
            var results = (await _service.GetRegistrationsByOccasionAsync(occasion.Id)).ToList();

            // Assert
            results.Count.ShouldBe(2);
            results.All(r => r.CourseOccasionId == occasion.Id).ShouldBeTrue();
        }

        [Fact]
        public async Task GetRegistrationDetailsAsync_ShouldIncludeParticipantAndCourseOccasion()
        {
            // Arrange
            var course = await CreateTestCourseAsync("Advanced Math");
            var occasion = await CreateTestOccasionAsync(course.Id);
            var participant = await CreateTestParticipantAsync("student@university.com");

            var registration = new CourseRegistration(participant.Id, occasion.Id);
            _context.CourseRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetRegistrationDetailsAsync(registration.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Participant.ShouldNotBeNull();
            result.Participant.Email.ShouldBe("student@university.com");
            result.CourseOccasion.ShouldNotBeNull();
            result.CourseOccasion.Course.Name.ShouldBe("Advanced Math");
        }

        [Fact]
        public async Task ConfirmRegistrationAsync_ShouldUpdateStatusToConfirmed()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = await CreateTestOccasionAsync(course.Id);
            var participant = await CreateTestParticipantAsync();

            var registration = new CourseRegistration(participant.Id, occasion.Id);
            _context.CourseRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            // Act
            await _service.ConfirmRegistrationAsync(registration.Id);

            // Assert
            var updated = await _unitOfWork.CourseRegistrations.GetByIdAsync(registration.Id);
            updated.Status.ShouldBe("Confirmed");
            updated.ConfirmedAt.ShouldNotBeNull();
        }

        [Fact]
        public async Task CancelRegistrationAsync_ShouldUpdateStatusAndDecrementParticipantCount()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = await CreateTestOccasionAsync(course.Id, 2);
            var participant = await CreateTestParticipantAsync();

            var registration = new CourseRegistration(participant.Id, occasion.Id);
            _context.CourseRegistrations.Add(registration);
            occasion.TryRegisterParticipant(); // Increment count
            await _context.SaveChangesAsync();

            // Verify initial count
            var initialOccasion = await _unitOfWork.CourseOccasions.GetByIdAsync(occasion.Id);
            initialOccasion.CurrentParticipants.ShouldBe(1);

            // Act
            await _service.CancelRegistrationAsync(registration.Id);

            // Assert
            var updatedRegistration = await _unitOfWork.CourseRegistrations.GetByIdAsync(registration.Id);
            updatedRegistration.Status.ShouldBe("Cancelled");
            updatedRegistration.CancelledAt.ShouldNotBeNull();

            var updatedOccasion = await _unitOfWork.CourseOccasions.GetByIdAsync(occasion.Id);
            updatedOccasion.CurrentParticipants.ShouldBe(0);
        }

        [Fact]
        public async Task UpdateRegistrationStatusAsync_ToConfirmed_ShouldConfirmRegistration()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = await CreateTestOccasionAsync(course.Id);
            var participant = await CreateTestParticipantAsync();

            var registration = new CourseRegistration(participant.Id, occasion.Id);
            _context.CourseRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateRegistrationStatusDto { Status = "confirmed" };

            // Act
            await _service.UpdateRegistrationsStatusAsync(registration.Id, updateDto);

            // Assert
            var updated = await _unitOfWork.CourseRegistrations.GetByIdAsync(registration.Id);
            updated.Status.ShouldBe("Confirmed");
            updated.ConfirmedAt.ShouldNotBeNull();
        }

        [Fact]
        public async Task UpdateRegistrationStatusAsync_ToCancelled_ShouldCancelRegistration()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = await CreateTestOccasionAsync(course.Id, 2);
            var participant = await CreateTestParticipantAsync();

            var registration = new CourseRegistration(participant.Id, occasion.Id);
            _context.CourseRegistrations.Add(registration);
            occasion.TryRegisterParticipant();
            await _context.SaveChangesAsync();

            var updateDto = new UpdateRegistrationStatusDto { Status = "cancelled" };

            // Act
            await _service.UpdateRegistrationsStatusAsync(registration.Id, updateDto);

            // Assert
            var updated = await _unitOfWork.CourseRegistrations.GetByIdAsync(registration.Id);
            updated.Status.ShouldBe("Cancelled");
            updated.CancelledAt.ShouldNotBeNull();

            var updatedOccasion = await _unitOfWork.CourseOccasions.GetByIdAsync(occasion.Id);
            updatedOccasion.CurrentParticipants.ShouldBe(0);
        }

        [Fact]
        public async Task UpdateRegistrationStatusAsync_WithInvalidStatus_ShouldThrowArgumentException()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = await CreateTestOccasionAsync(course.Id);
            var participant = await CreateTestParticipantAsync();

            var registration = new CourseRegistration(participant.Id, occasion.Id);
            _context.CourseRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateRegistrationStatusDto { Status = "invalid" };

            // Act & Assert
            var exception = await Should.ThrowAsync<ArgumentException>(async () =>
                await _service.UpdateRegistrationsStatusAsync(registration.Id, updateDto));

            exception.Message.ShouldContain("Invalid status");
        }

        [Fact]
        public async Task DeleteRegistrationAsync_ShouldRemoveRegistrationAndDecrementParticipantCount()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = await CreateTestOccasionAsync(course.Id, 2);
            var participant = await CreateTestParticipantAsync();

            var registration = new CourseRegistration(participant.Id, occasion.Id);
            _context.CourseRegistrations.Add(registration);
            occasion.TryRegisterParticipant();
            await _context.SaveChangesAsync();

            // Act
            await _service.DeleteRegistrationAsync(registration.Id);

            // Assert
            var deleted = await _unitOfWork.CourseRegistrations.GetByIdAsync(registration.Id);
            deleted.ShouldBeNull();

            var updatedOccasion = await _unitOfWork.CourseOccasions.GetByIdAsync(occasion.Id);
            updatedOccasion.CurrentParticipants.ShouldBe(0);
        }

        [Fact]
        public async Task MapToDto_ShouldIncludeAllRelatedData()
        {
            // Arrange
            var course = await CreateTestCourseAsync("Physics 101");
            var occasion = await CreateTestOccasionAsync(course.Id);
            var participant = await CreateTestParticipantAsync("physics.student@test.com");

            var registration = new CourseRegistration(participant.Id, occasion.Id);
            _context.CourseRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetRegistrationDetailsAsync(registration.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(registration.Id);
            result.Participant.ShouldNotBeNull();
            result.Participant.Email.ShouldBe("physics.student@test.com");
            result.CourseOccasion.ShouldNotBeNull();
            result.CourseOccasion.Course.Name.ShouldBe("Physics 101");
        }
    }
}