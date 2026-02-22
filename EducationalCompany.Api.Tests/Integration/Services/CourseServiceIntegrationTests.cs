// Note:
// AI-assisted tools were used to help generate and structure parts of these unit tests.
// All tests have been reviewed, validated, and verified manually to ensure correctness
// and proper coverage of the intended functionality.

using EducationalCompany.Application.DTOs;
using EducationalCompany.Application.Interfaces;
using EducationalCompany.Application.Services;
using EducationalCompany.Domain.Entities;
using EducationalCompany.Infrastructure;
using EducationalCompany.Infrastructure.Data;
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
    public class CourseServiceIntegrationTests : IAsyncLifetime
    {
        private ApplicationDbContext _context;
        private IUnitOfWork _unitOfWork;
        private ICourseService _service;
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
            services.AddScoped<ICourseRepository, CourseRepository>();
            services.AddScoped<ICourseOccasionRepository, CourseOccasionRepository>();

            // Register UnitOfWork
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Register services
            services.AddScoped<ICourseService, CourseService>();

            _serviceProvider = services.BuildServiceProvider();
            _scope = _serviceProvider.CreateScope();

            _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _unitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            _service = _scope.ServiceProvider.GetRequiredService<ICourseService>();
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

        private Course CreateTestCourse(string name = null, string description = null, int durationHours = 40, decimal price = 1000m)
        {
            return new Course(
                name ?? $"Course {Guid.NewGuid()}",
                description ?? "Test Description",
                durationHours,
                price);
        }

        [Fact]
        public async Task CreateCourseAsync_WithValidDto_ShouldCreateCourse()
        {
            // Arrange
            var createDto = new CreateCourseDto
            {
                Name = "Data Structures",
                Description = "Advanced data structures and algorithms",
                DurationHours = 45,
                Price = 750m
            };

            // Act
            var result = await _service.CreateCourseAsync(createDto);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldNotBe(Guid.Empty);
            result.Name.ShouldBe(createDto.Name);
            result.Description.ShouldBe(createDto.Description);
            result.DurationHours.ShouldBe(createDto.DurationHours);
            result.Price.ShouldBe(createDto.Price);

            // Verify in database
            var savedCourse = await _unitOfWork.Courses.GetByIdAsync(result.Id);
            savedCourse.ShouldNotBeNull();
            savedCourse.Name.ShouldBe(createDto.Name);
        }

        [Fact]
        public async Task CreateCourseAsync_WithDuplicateName_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var existingCourse = CreateTestCourse("Unique Course");
            _context.Courses.Add(existingCourse);
            await _context.SaveChangesAsync();

            var createDto = new CreateCourseDto
            {
                Name = "Unique Course", // Same name
                Description = "Another description",
                DurationHours = 30,
                Price = 500m
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.CreateCourseAsync(createDto));

            exception.Message.ShouldContain("already exists");
        }

        [Fact]
        public async Task GetCourseByIdAsync_WithValidId_ShouldReturnCourse()
        {
            // Arrange
            var course = CreateTestCourse("Database Design");
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetCourseByIdAsync(course.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(course.Id);
            result.Name.ShouldBe("Database Design");
        }

        [Fact]
        public async Task GetCourseByIdAsync_WithInvalidId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.GetCourseByIdAsync(invalidId));

            exception.Message.ShouldContain(invalidId.ToString());
        }

        [Fact]
        public async Task GetAllCoursesAsync_WithMultipleCourses_ShouldReturnAll()
        {
            // Arrange
            var courses = new[]
            {
                CreateTestCourse("Web Development"),
                CreateTestCourse("Mobile App Development"),
                CreateTestCourse("Cloud Computing")
            };

            _context.Courses.AddRange(courses);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _service.GetAllCoursesAsync()).ToList();

            // Assert
            results.Count.ShouldBe(3);
            results.Select(c => c.Name).ShouldContain("Web Development");
            results.Select(c => c.Name).ShouldContain("Mobile App Development");
            results.Select(c => c.Name).ShouldContain("Cloud Computing");
        }

        [Fact]
        public async Task GetAllCoursesAsync_WhenNoCourses_ShouldReturnEmptyList()
        {
            // Act
            var results = await _service.GetAllCoursesAsync();

            // Assert
            results.ShouldBeEmpty();
        }

        [Fact]
        public async Task UpdateCourseAsync_WithValidData_ShouldUpdateCourse()
        {
            // Arrange
            var course = CreateTestCourse("Old Name", "Old Description", 40, 1000m);
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateCourseDto
            {
                Name = "New Name",
                Description = "New Description",
                DurationHours = 50,
                Price = 1200m
            };

            // Act
            await _service.UpdateCourseAsync(course.Id, updateDto);

            // Assert
            var updated = await _unitOfWork.Courses.GetByIdAsync(course.Id);
            updated.Name.ShouldBe("New Name");
            updated.Description.ShouldBe("New Description");
            updated.DurationHours.ShouldBe(50);
            updated.Price.ShouldBe(1200m);
        }

        [Fact]
        public async Task UpdateCourseAsync_WithSameName_ShouldNotCheckForDuplicates()
        {
            // Arrange
            var course = CreateTestCourse("Same Name");
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateCourseDto
            {
                Name = "Same Name", // Same name
                Description = "Updated Description",
                DurationHours = 50,
                Price = 1200m
            };

            // Act
            await _service.UpdateCourseAsync(course.Id, updateDto);

            // Assert
            var updated = await _unitOfWork.Courses.GetByIdAsync(course.Id);
            updated.Description.ShouldBe("Updated Description");
        }

        [Fact]
        public async Task UpdateCourseAsync_WithDuplicateName_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var course1 = CreateTestCourse("Course 1");
            var course2 = CreateTestCourse("Course 2");
            _context.Courses.AddRange(course1, course2);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateCourseDto
            {
                Name = "Course 2", // Trying to use course2's name
                Description = "Updated",
                DurationHours = 50,
                Price = 1200m
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.UpdateCourseAsync(course1.Id, updateDto));

            exception.Message.ShouldContain("already exists");
        }

        [Fact]
        public async Task UpdateCourseAsync_WithNonExistentCourse_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var updateDto = new UpdateCourseDto
            {
                Name = "Non Existent",
                Description = "Description",
                DurationHours = 40,
                Price = 500m
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.UpdateCourseAsync(nonExistentId, updateDto));

            exception.Message.ShouldContain(nonExistentId.ToString());
        }

        [Fact]
        public async Task DeleteCourseAsync_WithValidId_ShouldDeleteCourse()
        {
            // Arrange
            var course = CreateTestCourse("To Be Deleted");
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            // Act
            await _service.DeleteCourseAsync(course.Id);

            // Assert
            var deleted = await _unitOfWork.Courses.GetByIdAsync(course.Id);
            deleted.ShouldBeNull();
        }

        [Fact]
        public async Task DeleteCourseAsync_WithNonExistentCourse_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.DeleteCourseAsync(nonExistentId));

            exception.Message.ShouldContain(nonExistentId.ToString());
        }

        [Fact]
        public async Task SearchCoursesAsync_WithSearchTerm_ShouldReturnMatchingCourses()
        {
            // Arrange
            var courses = new[]
            {
                CreateTestCourse("Python Programming", "Learn Python"),
                CreateTestCourse("Java Programming", "Learn Java"),
                CreateTestCourse("JavaScript Basics", "Learn JavaScript"),
                CreateTestCourse("Database Design", "Learn SQL")
            };

            _context.Courses.AddRange(courses);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _service.SearchCoursesAsync("Programming")).ToList();

            // Assert
            results.Count.ShouldBe(2);
            results.Select(c => c.Name).ShouldContain("Python Programming");
            results.Select(c => c.Name).ShouldContain("Java Programming");
        }

        [Fact]
        public async Task SearchCoursesAsync_WithNoMatches_ShouldReturnEmptyList()
        {
            // Arrange
            var courses = new[]
            {
                CreateTestCourse("Math"),
                CreateTestCourse("Physics")
            };

            _context.Courses.AddRange(courses);
            await _context.SaveChangesAsync();

            // Act
            var results = await _service.SearchCoursesAsync("Nonexistent");

            // Assert
            results.ShouldBeEmpty();
        }

        [Fact]
        public async Task SearchCoursesAsync_WithEmptySearchTerm_ShouldReturnAllCourses()
        {
            // Arrange
            var courses = new[]
            {
                CreateTestCourse("Course A"),
                CreateTestCourse("Course B"),
                CreateTestCourse("Course C")
            };

            _context.Courses.AddRange(courses);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _service.SearchCoursesAsync("")).ToList();

            // Assert
            results.Count.ShouldBe(3);
        }

        [Fact]
        public void MapToDto_ShouldMapAllPropertiesCorrectly()
        {
            // Arrange
            var course = CreateTestCourse("Test Course", "Test Description", 40, 500m);

            _context.Courses.Add(course);
            _context.SaveChanges();

            // Act
            var result = _service.GetCourseByIdAsync(course.Id).Result;

            // Assert
            result.Id.ShouldBe(course.Id);
            result.Name.ShouldBe(course.Name);
            result.Description.ShouldBe(course.Description);
            result.DurationHours.ShouldBe(course.DurationHours);
            result.Price.ShouldBe(course.Price);
        }

        [Fact]
        public async Task CreateCourseAsync_WithNullDto_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Should.ThrowAsync<ArgumentNullException>(async () =>
                await _service.CreateCourseAsync(null));
        }

        [Fact]
        public async Task UpdateCourseAsync_WithNullDto_ShouldThrowArgumentNullException()
        {
            // Arrange
            var courseId = Guid.NewGuid();

            // Act & Assert
            await Should.ThrowAsync<ArgumentNullException>(async () =>
                await _service.UpdateCourseAsync(courseId, null));
        }

        [Fact]
        public async Task CreateCourseAsync_WithInvalidPrice_ShouldHandleCorrectly()
        {
            // Arrange
            var createDto = new CreateCourseDto
            {
                Name = "Advanced Course",
                Description = "Description",
                DurationHours = 40,
                Price = -100m // Invalid price
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<ArgumentException>(async () =>
                await _service.CreateCourseAsync(createDto));
        }
    }
}