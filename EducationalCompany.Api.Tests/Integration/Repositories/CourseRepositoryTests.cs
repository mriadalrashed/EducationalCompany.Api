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
    public class CourseRepositoryTests : IAsyncLifetime
    {
        private ApplicationDbContext _context;
        private CourseRepository _repository;
        private IMemoryCache _memoryCache;

        public async Task InitializeAsync()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"TestDatabase_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _repository = new CourseRepository(_context, _memoryCache);

            await _context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
            _memoryCache.Dispose();
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
        public async Task AddAsync_ValidCourse_ShouldAddToDatabase()
        {
            // Arrange
            var course = CreateTestCourse("Mathematics 101", "Basic Mathematics", 40, 500m);

            // Act
            await _repository.AddAsync(course);
            await _context.SaveChangesAsync();

            // Assert
            var savedCourse = await _context.Courses.FindAsync(course.Id);
            savedCourse.ShouldNotBeNull();
            savedCourse.Name.ShouldBe("Mathematics 101");
            savedCourse.Description.ShouldBe("Basic Mathematics");
            savedCourse.DurationHours.ShouldBe(40);
            savedCourse.Price.ShouldBe(500m);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingCourse_ShouldReturnCourse()
        {
            // Arrange
            var course = CreateTestCourse("Physics 101", "Basic Physics", 30, 600m);
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(course.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(course.Id);
            result.Name.ShouldBe("Physics 101");
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentCourse_ShouldReturnNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _repository.GetByIdAsync(nonExistentId);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetAllAsync_WithMultipleCourses_ShouldReturnAll()
        {
            // Arrange
            var courses = new[]
            {
                CreateTestCourse("Course 1"),
                CreateTestCourse("Course 2"),
                CreateTestCourse("Course 3")
            };

            _context.Courses.AddRange(courses);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _repository.GetAllAsync()).ToList();

            // Assert
            results.Count.ShouldBe(3);
            results.Select(c => c.Name).ShouldContain("Course 1");
            results.Select(c => c.Name).ShouldContain("Course 2");
            results.Select(c => c.Name).ShouldContain("Course 3");
        }

        [Fact]
        public async Task GetCourseWithOccasionsAsync_ShouldIncludeOccasions()
        {
            // Arrange
            var course = CreateTestCourse("Programming 101");
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var occasions = new[]
            {
                new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(20), 30),
                new CourseOccasion(course.Id, DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(40), 25)
            };

            _context.CourseOccasions.AddRange(occasions);
            await _context.SaveChangesAsync();

            // Clear cache
            _memoryCache.Remove($"course_with_occasions_{course.Id}");

            // Act
            var result = await _repository.GetCourseWithOccasionsAsync(course.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Occasions.ShouldNotBeNull();
            result.Occasions.Count.ShouldBe(2);
        }

        [Fact]
        public async Task SearchCoursesAsync_WithNameSearch_ShouldReturnMatchingCourses()
        {
            // Arrange
            var courses = new[]
            {
                CreateTestCourse("Mathematics Advanced", "Advanced Math", 40, 500m),
                CreateTestCourse("Physics Advanced", "Advanced Physics", 30, 600m),
                CreateTestCourse("Chemistry Basics", "Basic Chemistry", 20, 400m)
            };

            _context.Courses.AddRange(courses);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _repository.SearchCoursesAsync("Advanced")).ToList();

            // Assert
            results.Count.ShouldBe(2);
            results.Select(c => c.Name).ShouldContain("Mathematics Advanced");
            results.Select(c => c.Name).ShouldContain("Physics Advanced");
            results.Select(c => c.Name).ShouldNotContain("Chemistry Basics");
        }

        [Fact]
        public async Task SearchCoursesAsync_WithDescriptionSearch_ShouldReturnMatchingCourses()
        {
            // Arrange
            var courses = new[]
            {
                CreateTestCourse("Math 101", "Basic Mathematics course", 40, 500m),
                CreateTestCourse("Physics 101", "Basic Physics course", 30, 600m),
                CreateTestCourse("Chemistry 101", "Advanced Chemistry", 20, 400m)
            };

            _context.Courses.AddRange(courses);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _repository.SearchCoursesAsync("Basic")).ToList();

            // Assert
            results.Count.ShouldBe(2);
            results.Select(c => c.Description).ShouldContain("Basic Mathematics course");
            results.Select(c => c.Description).ShouldContain("Basic Physics course");
        }

        [Fact]
        public async Task SearchCoursesAsync_WithEmptySearchTerm_ShouldReturnAllCourses()
        {
            // Arrange
            var courses = new[]
            {
                CreateTestCourse("Course 1"),
                CreateTestCourse("Course 2"),
                CreateTestCourse("Course 3")
            };

            _context.Courses.AddRange(courses);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _repository.SearchCoursesAsync("")).ToList();

            // Assert
            results.Count.ShouldBe(3);
        }

        [Fact]
        public async Task CourseNameExistsAsync_WhenNameExists_ShouldReturnTrue()
        {
            // Arrange
            var course = CreateTestCourse("Unique Course Name");
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.CourseNameExistsAsync("Unique Course Name");

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task CourseNameExistsAsync_WhenNameDoesNotExist_ShouldReturnFalse()
        {
            // Act
            var result = await _repository.CourseNameExistsAsync("Non Existent Course");

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateCourse()
        {
            // Arrange
            var course = CreateTestCourse("Original Name", "Original Description", 40, 500m);
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            // Detach to simulate fresh context
            _context.Entry(course).State = EntityState.Detached;

            // Update
            course.Update("Updated Name", "Updated Description", 50, 600m);

            // Act
            await _repository.UpdateAsync(course);
            await _context.SaveChangesAsync();

            // Assert
            var updated = await _repository.GetByIdAsync(course.Id);
            updated.Name.ShouldBe("Updated Name");
            updated.Description.ShouldBe("Updated Description");
            updated.DurationHours.ShouldBe(50);
            updated.Price.ShouldBe(600m);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveCourse()
        {
            // Arrange
            var course = CreateTestCourse("To Be Deleted");
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(course.Id);
            await _context.SaveChangesAsync();

            // Assert
            var deleted = await _repository.GetByIdAsync(course.Id);
            deleted.ShouldBeNull();
        }

        [Fact]
        public async Task Cache_ShouldWorkForGetCourseWithOccasions()
        {
            // Arrange
            var course = CreateTestCourse("Cached Course");
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            // Act - First call should hit database
            var firstCall = await _repository.GetCourseWithOccasionsAsync(course.Id);

            // Modify cache key to verify second call uses cache
            var cacheKey = $"course_with_occasions_{course.Id}";
            var cachedEntry = _memoryCache.Get(cacheKey);

            cachedEntry.ShouldNotBeNull();

            // Second call should use cache
            var secondCall = await _repository.GetCourseWithOccasionsAsync(course.Id);

            // Assert
            firstCall.Id.ShouldBe(secondCall.Id);
            firstCall.Name.ShouldBe(secondCall.Name);
        }
    }
}