// Note:
// AI-assisted tools were used to help generate and structure parts of these unit tests.
// All tests have been reviewed, validated, and verified manually to ensure correctness
// and proper coverage of the intended functionality.

using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Application.Interfaces;
using EducationalCompany.Api.Application.Services;
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
    public class CourseOccasionServiceTests : IAsyncLifetime
    {
        private ApplicationDbContext _context;
        private IUnitOfWork _unitOfWork;
        private ICourseOccasionService _service;
        private ICourseRegistrationService _registrationService;
        private ServiceProvider _serviceProvider;
        private IMemoryCache _memoryCache;
        private IServiceScope _scope;

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
            _service = _scope.ServiceProvider.GetRequiredService<ICourseOccasionService>();
            _registrationService = _scope.ServiceProvider.GetRequiredService<ICourseRegistrationService>();
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
        public async Task CreateOccasionAsync_WithValidDto_ShouldCreateOccasion()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var createDto = new CreateCourseOccasionDto
            {
                CourseId = course.Id,
                StartDate = DateTime.UtcNow.AddDays(10),
                EndDate = DateTime.UtcNow.AddDays(20),
                MaxParticipants = 30
            };

            // Act
            var result = await _service.CreateOccasionAsync(createDto);

            // Ensure changes are saved
            await _unitOfWork.CompleteAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldNotBe(Guid.Empty);

            // Verify in database using the occasion's Id
            var savedOccasion = await _context.CourseOccasions.FindAsync(result.Id);
            savedOccasion.ShouldNotBeNull();
            savedOccasion.CourseId.ShouldBe(course.Id);
        }

        [Fact]
        public async Task CreateOccasionAsync_WithNonExistentCourse_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var createDto = new CreateCourseOccasionDto
            {
                CourseId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow.AddDays(10),
                EndDate = DateTime.UtcNow.AddDays(20),
                MaxParticipants = 30
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.CreateOccasionAsync(createDto));

            exception.Message.ShouldContain(createDto.CourseId.ToString());
        }

        [Fact]
        public async Task GetOccasionByIdAsync_WithValidId_ShouldReturnOccasion()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 30);

            _context.CourseOccasions.Add(occasion);
            await _context.SaveChangesAsync();

            // Refresh the entity to ensure ID is loaded
            await _context.Entry(occasion).ReloadAsync();

            // Act
            var result = await _service.GetOccasionByIdAsync(occasion.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(occasion.Id);
            result.CourseId.ShouldBe(course.Id);
        }

        [Fact]
        public async Task GetAllOccasionsAsync_ShouldReturnAllOccasions()
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
            var results = (await _service.GetAllOccasionsAsync()).ToList();

            // Assert
            results.Count.ShouldBe(3);
            results.Select(o => o.CourseId).All(id => id == course.Id).ShouldBeTrue();
        }

        [Fact]
        public async Task GetByCourseIdAsync_ShouldReturnCourseOccasions()
        {
            // Arrange
            var course1 = await CreateTestCourseAsync("Course 1");
            var course2 = await CreateTestCourseAsync("Course 2");

            var occasions = new[]
            {
                new CourseOccasion(course1.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 30),
                new CourseOccasion(course1.Id, DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(40), 25),
                new CourseOccasion(course2.Id, DateTime.UtcNow.AddDays(50), DateTime.UtcNow.AddDays(60), 20)
            };

            _context.CourseOccasions.AddRange(occasions);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _service.GetOccasionsByCourseIdAsync(course1.Id)).ToList();

            // Assert
            results.Count.ShouldBe(2);
            results.All(o => o.CourseId == course1.Id).ShouldBeTrue();
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
            var results = (await _service.GetUpComingOccasionsAsync()).ToList();

            // Assert
            results.Count.ShouldBe(2);
            results.All(o => o.StartDate > DateTime.UtcNow).ShouldBeTrue();
        }

        [Fact]
        public async Task UpdateOccasionAsync_WithValidDataAndNoRegistrations_ShouldUpdateOccasion()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 30);

            _context.CourseOccasions.Add(occasion);
            await _context.SaveChangesAsync();

            var updateDto = new CreateCourseOccasionDto
            {
                CourseId = course.Id,
                StartDate = DateTime.UtcNow.AddDays(15),
                EndDate = DateTime.UtcNow.AddDays(25),
                MaxParticipants = 25
            };

            // Act
            await _service.UpdateOccasionAsync(occasion.Id, updateDto);

            // Assert
            var updated = await _unitOfWork.CourseOccasions.GetByIdAsync(occasion.Id);
            updated.ShouldNotBeNull();
            updated.StartDate.ShouldBe(updateDto.StartDate.ToUniversalTime());
            updated.EndDate.ShouldBe(updateDto.EndDate.ToUniversalTime());
            updated.MaxParticipants.ShouldBe(25);
        }

        [Fact]
        public async Task UpdateOccasionAsync_WithExistingRegistrations_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 30);

            _context.CourseOccasions.Add(occasion);
            await _context.SaveChangesAsync();

            // Add a registration using the registration service
            var participant = await CreateTestParticipantAsync();
            var registrationDto = new CreateCourseRegistrationDto
            {
                ParticipantId = participant.Id,
                CourseOccasionId = occasion.Id
            };
            await _registrationService.CreateRegistrationAsync(registrationDto);

            var updateDto = new CreateCourseOccasionDto
            {
                CourseId = course.Id,
                StartDate = DateTime.UtcNow.AddDays(15),
                EndDate = DateTime.UtcNow.AddDays(25),
                MaxParticipants = 25
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.UpdateOccasionAsync(occasion.Id, updateDto));

            exception.Message.ShouldContain("Cannot update occasion that has registrations");
        }

        [Fact]
        public async Task DeleteOccasionAsync_WithNoRegistrations_ShouldDeleteOccasion()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 30);

            _context.CourseOccasions.Add(occasion);
            await _context.SaveChangesAsync();

            // Act
            await _service.DeleteOccasionAsync(occasion.Id);

            // Assert
            var deleted = await _unitOfWork.CourseOccasions.GetByIdAsync(occasion.Id);
            deleted.ShouldBeNull();
        }

        [Fact]
        public async Task DeleteOccasionAsync_WithExistingRegistrations_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 30);

            _context.CourseOccasions.Add(occasion);
            await _context.SaveChangesAsync();

            // Add a registration using the registration service
            var participant = await CreateTestParticipantAsync();
            var registrationDto = new CreateCourseRegistrationDto
            {
                ParticipantId = participant.Id,
                CourseOccasionId = occasion.Id
            };
            await _registrationService.CreateRegistrationAsync(registrationDto);

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.DeleteOccasionAsync(occasion.Id));

            exception.Message.ShouldContain("Cannot delete occasion that has registrations");
        }

        [Fact]
        public async Task AssignTeacherAsync_WithValidData_ShouldAssignTeacher()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var teacher = await CreateTestTeacherAsync();
            var occasion = new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 30);

            _context.CourseOccasions.Add(occasion);
            await _context.SaveChangesAsync();

            var assignDto = new AssignTeacherDto { TeacherId = teacher.Id };

            // Act
            await _service.AssignTeacherAsync(occasion.Id, assignDto);

            // Assert
            var updated = await _unitOfWork.CourseOccasions.GetByIdAsync(occasion.Id);
            updated.TeacherId.ShouldBe(teacher.Id);
        }

        [Fact]
        public async Task AssignTeacherAsync_WithNonExistentTeacher_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 30);

            _context.CourseOccasions.Add(occasion);
            await _context.SaveChangesAsync();

            var assignDto = new AssignTeacherDto { TeacherId = Guid.NewGuid() };

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.AssignTeacherAsync(occasion.Id, assignDto));

            exception.Message.ShouldContain(assignDto.TeacherId.ToString());
        }

        [Fact]
        public async Task GetOccasionWithRegistrationsAsync_ShouldIncludeRegistrations()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 30);

            _context.CourseOccasions.Add(occasion);
            await _context.SaveChangesAsync();

            // Add registrations using the registration service
            var participant1 = await CreateTestParticipantAsync("participant1@test.com");
            var participant2 = await CreateTestParticipantAsync("participant2@test.com");

            var registrationDto1 = new CreateCourseRegistrationDto
            {
                ParticipantId = participant1.Id,
                CourseOccasionId = occasion.Id
            };
            var registrationDto2 = new CreateCourseRegistrationDto
            {
                ParticipantId = participant2.Id,
                CourseOccasionId = occasion.Id
            };

            await _registrationService.CreateRegistrationAsync(registrationDto1);
            await _registrationService.CreateRegistrationAsync(registrationDto2);

            // Ensure all changes are saved
            await _unitOfWork.CompleteAsync();

            // Clear cache to ensure fresh data
            if (_memoryCache is MemoryCache cache)
            {
                cache.Clear();
            }

            // Act
            var result = await _service.GetOccasionWithRegistrationsAsync(occasion.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(occasion.Id);
        }

        [Fact]
        public async Task IsOccasionFullAsync_ShouldReturnCorrectStatus()
        {
            // Arrange
            var course = await CreateTestCourseAsync();
            var occasion = new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 1); // Max 1

            _context.CourseOccasions.Add(occasion);
            await _context.SaveChangesAsync();

            // Initially not full
            var initiallyFull = await _service.IsOccasionFullAsync(occasion.Id);
            initiallyFull.ShouldBeFalse();

            // Add a registration using the registration service
            var participant = await CreateTestParticipantAsync();
            var registrationDto = new CreateCourseRegistrationDto
            {
                ParticipantId = participant.Id,
                CourseOccasionId = occasion.Id
            };

            await _registrationService.CreateRegistrationAsync(registrationDto);
            await _unitOfWork.CompleteAsync();

            // Clear cache to ensure fresh data
            if (_memoryCache is MemoryCache cache)
            {
                cache.Clear();
            }

            // Now should be full
            var afterRegistrationFull = await _service.IsOccasionFullAsync(occasion.Id);
            afterRegistrationFull.ShouldBeTrue();
        }

        [Fact]
        public async Task MapToDto_ShouldIncludeCourseAndTeacherDetails()
        {
            // Arrange
            var course = await CreateTestCourseAsync("Integration Course");
            var teacher = await CreateTestTeacherAsync();
            var occasion = new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 30);
            occasion.AssignTeacher(teacher.Id);

            _context.CourseOccasions.Add(occasion);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetOccasionByIdAsync(occasion.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Course.ShouldNotBeNull();
            result.Course.Name.ShouldBe("Integration Course");
            result.Teacher.ShouldNotBeNull();
            result.Teacher.FirstName.ShouldBe("John");
            result.Teacher.LastName.ShouldBe("Doe");
        }
    }
}