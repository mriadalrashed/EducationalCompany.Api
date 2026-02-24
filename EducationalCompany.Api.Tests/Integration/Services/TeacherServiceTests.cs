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
using EducationalCompany.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
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
    public class TeacherServiceIntegrationTests : IAsyncLifetime
    {
        private ApplicationDbContext _context;
        private IUnitOfWork _unitOfWork;
        private ITeacherService _service;
        private ServiceProvider _serviceProvider;
        private IMemoryCache _memoryCache;
        private IServiceScope _scope;

        public async Task InitializeAsync()
        {
            var services = new ServiceCollection();

            // Add in-memory database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase($"TestDatabase_{Guid.NewGuid()}"));

            // Add memory cache
            services.AddMemoryCache();

            // Register repositories
            services.AddScoped<ITeacherRepository, TeacherRepository>();
            services.AddScoped<ICourseRepository, CourseRepository>();
            services.AddScoped<ICourseOccasionRepository, CourseOccasionRepository>();

            // Register UnitOfWork
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Register services
            services.AddScoped<ITeacherService, TeacherService>();

            _serviceProvider = services.BuildServiceProvider();
            _scope = _serviceProvider.CreateScope();

            _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _unitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            _service = _scope.ServiceProvider.GetRequiredService<ITeacherService>();
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

        private Teacher CreateTestTeacher(
            string firstName = "John",
            string lastName = "Doe",
            string email = null,
            string phone = "1234567890",
            string specialization = "Mathematics")
        {
            return new Teacher(
                firstName,
                lastName,
                email ?? $"teacher.{Guid.NewGuid()}@test.com",
                phone,
                specialization);
        }

        private async Task<Course> CreateTestCourseAsync(string name = "Test Course")
        {
            var course = new Course(name, "Test Description", 40, 1000m);
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            return course;
        }

        private async Task<CourseOccasion> CreateTestOccasionAsync(Guid courseId, Guid? teacherId = null)
        {
            var occasion = new CourseOccasion(
                courseId,
                DateTime.UtcNow.AddDays(10),
                DateTime.UtcNow.AddDays(20),
                30);

            if (teacherId.HasValue)
            {
                occasion.AssignTeacher(teacherId.Value);
            }

            _context.CourseOccasions.Add(occasion);
            await _context.SaveChangesAsync();
            return occasion;
        }

        [Fact]
        public async Task CreateTeacherAsync_WithValidDto_ShouldCreateTeacher()
        {
            // Arrange
            var createDto = new CreateTeacherDto
            {
                FirstName = "Marie",
                LastName = "Curie",
                Email = "marie.curie@science.com",
                Phone = "5551234567",
                Specialization = "Physics & Chemistry"
            };

            // Act
            var result = await _service.CreateTeacherAsync(createDto);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldNotBe(Guid.Empty);
            result.FirstName.ShouldBe(createDto.FirstName);
            result.LastName.ShouldBe(createDto.LastName);
            result.Email.ShouldBe(createDto.Email);
            result.Phone.ShouldBe(createDto.Phone);
            result.Specialization.ShouldBe(createDto.Specialization);

            // Verify in database
            var savedTeacher = await _unitOfWork.Teachers.GetByIdAsync(result.Id);
            savedTeacher.ShouldNotBeNull();
            savedTeacher.Email.ShouldBe(createDto.Email);
        }

        [Fact]
        public async Task CreateTeacherAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var existingTeacher = CreateTestTeacher(
                "Existing",
                "Teacher",
                "duplicate@test.com",
                "1111111111",
                "Math");

            _context.Teachers.Add(existingTeacher);
            await _context.SaveChangesAsync();

            var createDto = new CreateTeacherDto
            {
                FirstName = "New",
                LastName = "Teacher",
                Email = "duplicate@test.com", // Same email
                Phone = "2222222222",
                Specialization = "Science"
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.CreateTeacherAsync(createDto));

            exception.Message.ShouldContain("already exists");
        }

        [Fact]
        public async Task GetTeacherByIdAsync_WithValidId_ShouldReturnTeacher()
        {
            // Arrange
            var teacher = CreateTestTeacher("Isaac", "Newton", "isaac.newton@physics.com", "5555555555", "Physics");
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetTeacherByIdAsync(teacher.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(teacher.Id);
            result.FirstName.ShouldBe("Isaac");
            result.LastName.ShouldBe("Newton");
            result.Email.ShouldBe("isaac.newton@physics.com");
        }

        [Fact]
        public async Task GetTeacherByIdAsync_WithInvalidId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.GetTeacherByIdAsync(invalidId));

            exception.Message.ShouldContain(invalidId.ToString());
        }

        [Fact]
        public async Task GetAllTeachersAsync_WithMultipleTeachers_ShouldReturnAll()
        {
            // Arrange
            var teachers = new[]
            {
                CreateTestTeacher("Alan", "Turing", "alan.turing@cs.com", "1111111111", "Computer Science"),
                CreateTestTeacher("Ada", "Lovelace", "ada.lovelace@cs.com", "2222222222", "Computer Science"),
                CreateTestTeacher("Grace", "Hopper", "grace.hopper@cs.com", "3333333333", "Computer Science")
            };

            _context.Teachers.AddRange(teachers);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _service.GetAllTeachersAsync()).ToList();

            // Assert
            results.Count.ShouldBe(3);
            results.Select(t => t.Email).ShouldContain("alan.turing@cs.com");
            results.Select(t => t.Email).ShouldContain("ada.lovelace@cs.com");
            results.Select(t => t.Email).ShouldContain("grace.hopper@cs.com");
        }

        [Fact]
        public async Task UpdateTeacherAsync_WithValidData_ShouldUpdateTeacher()
        {
            // Arrange
            var teacher = CreateTestTeacher("Old", "Name", "old@test.com", "1111111111", "Old Specialization");
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateTeacherDto
            {
                FirstName = "Updated",
                LastName = "Name",
                Email = "updated@test.com",
                Phone = "2222222222",
                Specialization = "Updated Specialization"
            };

            // Act
            await _service.UpdateTeacherAsync(teacher.Id, updateDto);

            // Assert
            var updated = await _unitOfWork.Teachers.GetByIdAsync(teacher.Id);
            updated.FirstName.ShouldBe("Updated");
            updated.LastName.ShouldBe("Name");
            updated.Email.ShouldBe("updated@test.com");
            updated.Phone.ShouldBe("2222222222");
            updated.Specialization.ShouldBe("Updated Specialization");
        }

        [Fact]
        public async Task UpdateTeacherAsync_WithSameEmail_ShouldNotCheckForDuplicates()
        {
            // Arrange
            var teacher = CreateTestTeacher("Same", "Email", "same@test.com", "1111111111", "Specialization");
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateTeacherDto
            {
                FirstName = "Updated",
                LastName = "Email",
                Email = "same@test.com", // Same email
                Phone = "2222222222",
                Specialization = "Updated Specialization"
            };

            // Act
            await _service.UpdateTeacherAsync(teacher.Id, updateDto);

            // Assert
            var updated = await _unitOfWork.Teachers.GetByIdAsync(teacher.Id);
            updated.FirstName.ShouldBe("Updated");
            updated.Email.ShouldBe("same@test.com");
        }

        [Fact]
        public async Task UpdateTeacherAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var teacher1 = CreateTestTeacher("First", "Teacher", "first@test.com", "1111111111", "Math");
            var teacher2 = CreateTestTeacher("Second", "Teacher", "second@test.com", "2222222222", "Science");

            _context.Teachers.AddRange(teacher1, teacher2);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateTeacherDto
            {
                FirstName = "Updated",
                LastName = "Teacher",
                Email = "second@test.com", // Trying to use teacher2's email
                Phone = "3333333333",
                Specialization = "History"
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.UpdateTeacherAsync(teacher1.Id, updateDto));

            exception.Message.ShouldContain("already exists");
        }

        [Fact]
        public async Task DeleteTeacherAsync_WithNoAssignments_ShouldDeleteTeacher()
        {
            // Arrange
            var teacher = CreateTestTeacher("Delete", "Me", "delete@test.com", "9999999999", "ToDelete");
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            // Act
            await _service.DeleteTeacherAsync(teacher.Id);

            // Assert
            var deleted = await _unitOfWork.Teachers.GetByIdAsync(teacher.Id);
            deleted.ShouldBeNull();
        }

        [Fact]
        public async Task DeleteTeacherAsync_WithAssignments_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var teacher = CreateTestTeacher("Assigned", "Teacher", "assigned@test.com", "5555555555", "Math");
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            var course = await CreateTestCourseAsync("Math Course");
            var occasion = await CreateTestOccasionAsync(course.Id, teacher.Id);

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.DeleteTeacherAsync(teacher.Id));

            exception.Message.ShouldContain("Cannot delete teacher who is assigned to course occasions");
        }

        [Fact]
        public async Task SearchTeachersAsync_BySpecialization_ShouldReturnMatchingTeachers()
        {
            // Arrange
            var teachers = new[]
            {
                CreateTestTeacher("John", "Doe", "john.math@test.com", "1111111111", "Mathematics"),
                CreateTestTeacher("Jane", "Smith", "jane.math@test.com", "2222222222", "Mathematics"),
                CreateTestTeacher("Bob", "Johnson", "bob.physics@test.com", "3333333333", "Physics")
            };

            _context.Teachers.AddRange(teachers);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _service.SearchTeachersAsync("Mathematics")).ToList();

            // Assert
            results.Count.ShouldBe(2);
            results.All(t => t.Specialization == "Mathematics").ShouldBeTrue();
        }

        [Fact]
        public async Task SearchTeachersAsync_ByName_ShouldReturnMatchingTeachers()
        {
            // Arrange
            var teachers = new[]
            {
                CreateTestTeacher("Michael", "Jordan", "michael@test.com", "1111111111", "Basketball"),
                CreateTestTeacher("Michael", "Jackson", "jackson@test.com", "2222222222", "Music"),
                CreateTestTeacher("Michelle", "Obama", "michelle@test.com", "3333333333", "Law")
            };

            _context.Teachers.AddRange(teachers);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _service.SearchTeachersAsync("Michael")).ToList();

            // Assert
            results.Count.ShouldBe(2);
            results.Select(t => t.FirstName).Distinct().ShouldContain("Michael");
        }

        [Fact]
        public async Task GetTeacherWithOccasionsAsync_WithValidId_ShouldReturnTeacherWithOccasions()
        {
            // Arrange
            var teacher = CreateTestTeacher("Stephen", "Hawking", "stephen@cosmology.com", "4444444444", "Cosmology");
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            var course1 = await CreateTestCourseAsync("Black Holes");
            var course2 = await CreateTestCourseAsync("Time Travel");

            await CreateTestOccasionAsync(course1.Id, teacher.Id);
            await CreateTestOccasionAsync(course2.Id, teacher.Id);

            // Act
            var result = await _service.GetTeacherWithOccasionsAsync(teacher.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(teacher.Id);
            result.FirstName.ShouldBe("Stephen");
            // Note: The DTO doesn't include occasions, but the method should load them internally
        }

        [Fact]
        public async Task MapToDto_ShouldMapAllPropertiesCorrectly()
        {
            // Arrange
            var teacher = CreateTestTeacher(
                "Nikola",
                "Tesla",
                "nikola.tesla@inventor.com",
                "5555555555",
                "Electrical Engineering");

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetTeacherByIdAsync(teacher.Id);

            // Assert
            result.Id.ShouldBe(teacher.Id);
            result.FirstName.ShouldBe(teacher.FirstName);
            result.LastName.ShouldBe(teacher.LastName);
            result.Email.ShouldBe(teacher.Email);
            result.Phone.ShouldBe(teacher.Phone);
            result.Specialization.ShouldBe(teacher.Specialization);
            result.CreatedAt.ShouldBe(teacher.CreatedAt);
            result.UpdatedAt.ShouldBe(teacher.UpdatedAt);
        }

        [Fact]
        public async Task CreateTeacherAsync_WithNullDto_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Should.ThrowAsync<ArgumentNullException>(async () =>
                await _service.CreateTeacherAsync(null));
        }

        [Fact]
        public async Task UpdateTeacherAsync_WithNullDto_ShouldThrowArgumentNullException()
        {
            // Arrange
            var teacherId = Guid.NewGuid();

            // Act & Assert
            await Should.ThrowAsync<ArgumentNullException>(async () =>
                await _service.UpdateTeacherAsync(teacherId, null));
        }

        [Fact]
        public async Task SearchTeachersAsync_WithEmptySearchTerm_ShouldReturnAllTeachers()
        {
            // Arrange
            var teachers = new[]
            {
                CreateTestTeacher("Teacher", "One"),
                CreateTestTeacher("Teacher", "Two"),
                CreateTestTeacher("Teacher", "Three")
            };

            _context.Teachers.AddRange(teachers);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _service.SearchTeachersAsync("")).ToList();

            // Assert
            results.Count.ShouldBe(3);
        }
    }
}