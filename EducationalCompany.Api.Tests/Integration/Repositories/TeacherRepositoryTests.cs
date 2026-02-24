// Note:
// AI-assisted tools were used to help generate and structure parts of these unit tests.
// All tests have been reviewed, validated, and verified manually to ensure correctness
// and proper coverage of the intended functionality.

using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Infrastructure.Data;
using EducationalCompany.Api.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EducationalCompany.Tests.Integration.Repositories
{
    public class TeacherRepositoryTests : IAsyncLifetime
    {
        private ApplicationDbContext _context;
        private TeacherRepository _repository;

        public async Task InitializeAsync()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"TestDatabase_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new TeacherRepository(_context);

            await _context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
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
        public async Task AddAsync_ValidTeacher_ShouldAddToDatabase()
        {
            // Arrange
            var teacher = CreateTestTeacher("Jane", "Smith", "jane.smith@test.com", "5551234567", "Physics");

            // Act
            await _repository.AddAsync(teacher);
            await _context.SaveChangesAsync();

            // Assert
            var savedTeacher = await _context.Teachers.FindAsync(teacher.Id);
            savedTeacher.ShouldNotBeNull();
            savedTeacher.FirstName.ShouldBe("Jane");
            savedTeacher.LastName.ShouldBe("Smith");
            savedTeacher.Email.ShouldBe("jane.smith@test.com");
            savedTeacher.Specialization.ShouldBe("Physics");
        }

        [Fact]
        public async Task GetByIdAsync_ExistingTeacher_ShouldReturnTeacher()
        {
            // Arrange
            var teacher = CreateTestTeacher("Albert", "Einstein", "albert@physics.com", "5559876543", "Physics");
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(teacher.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(teacher.Id);
            result.FirstName.ShouldBe("Albert");
            result.LastName.ShouldBe("Einstein");
            result.Email.ShouldBe("albert@physics.com");
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentTeacher_ShouldReturnNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _repository.GetByIdAsync(nonExistentId);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetAllAsync_WithMultipleTeachers_ShouldReturnAll()
        {
            // Arrange
            var teachers = new[]
            {
                CreateTestTeacher("John", "Doe", "john@test.com", "1111111111", "Math"),
                CreateTestTeacher("Jane", "Smith", "jane@test.com", "2222222222", "Science"),
                CreateTestTeacher("Bob", "Johnson", "bob@test.com", "3333333333", "History")
            };

            _context.Teachers.AddRange(teachers);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _repository.GetAllAsync()).ToList();

            // Assert
            results.Count.ShouldBe(3);
            results.Select(t => t.Email).ShouldContain("john@test.com");
            results.Select(t => t.Email).ShouldContain("jane@test.com");
            results.Select(t => t.Email).ShouldContain("bob@test.com");
        }

        [Fact]
        public async Task GetTeacherWithOccasionsAsync_ShouldIncludeOccasionsAndCourses()
        {
            // Arrange
            var teacher = CreateTestTeacher("Richard", "Feynman", "feynman@physics.com", "5555555555", "Physics");
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            var course1 = await CreateTestCourseAsync("Quantum Mechanics");
            var course2 = await CreateTestCourseAsync("Electrodynamics");

            var occasion1 = await CreateTestOccasionAsync(course1.Id, teacher.Id);
            var occasion2 = await CreateTestOccasionAsync(course2.Id, teacher.Id);

            // Act
            var result = await _repository.GetTeacherWithOccasionsAsync(teacher.Id);

            // Assert
            result.ShouldNotBeNull();
            result.CourseOccasions.ShouldNotBeNull();
            result.CourseOccasions.Count.ShouldBe(2);

            var occasions = result.CourseOccasions.ToList();
            occasions.Any(o => o.Course.Name == "Quantum Mechanics").ShouldBeTrue();
            occasions.Any(o => o.Course.Name == "Electrodynamics").ShouldBeTrue();
        }

        [Fact]
        public async Task SearchTeachersAsync_ByFirstName_ShouldReturnMatchingTeachers()
        {
            // Arrange - Create teachers with distinct first names to avoid false matches
            var teachers = new[]
            {
        CreateTestTeacher("John", "Doe", "john.doe@test.com", "1111111111", "Math"),
        CreateTestTeacher("Johnny", "Smith", "johnny.smith@test.com", "2222222222", "Science"),
        CreateTestTeacher("Jane", "jonson", "jane.jonson@test.com", "3333333333", "History"),
        CreateTestTeacher("Michael", "Jordan", "michael.jordan@test.com", "4444444444", "Basketball") // Extra teacher to verify count
    };

            _context.Teachers.AddRange(teachers);
            await _context.SaveChangesAsync();

            // Act - Search for "John" which should match John and Johnny
            var results = (await _repository.SearchTeachersAsync("John")).ToList();

            // Assert
            results.Count.ShouldBe(2);
            results.Select(t => t.FirstName).ShouldContain("John");
            results.Select(t => t.FirstName).ShouldContain("Johnny");
            results.Select(t => t.FirstName).ShouldNotContain("Jane");
            results.Select(t => t.FirstName).ShouldNotContain("Michael");
        }


        [Fact]
        public async Task SearchTeachersAsync_ByFirstName_ExactMatch_ShouldReturnCorrectCount()
        {
            // Arrange
            var teachers = new[]
            {
        CreateTestTeacher("Jonathan", "Smith", "jonathan.smith@test.com", "1111111111", "Math"),
        CreateTestTeacher("John", "Doe", "john.doe@test.com", "2222222222", "Science"),
        CreateTestTeacher("John", "Adams", "john.adams@test.com", "3333333333", "History"),
        CreateTestTeacher("Jonson", "Mike", "jonson.mike@test.com", "4444444444", "Physics")
    };

            _context.Teachers.AddRange(teachers);
            await _context.SaveChangesAsync();

            // Act - Search for "John" (should match both John Doe and John Adams, but not Jonathan or Johnson)
            var results = (await _repository.SearchTeachersAsync("John")).ToList();

            // Assert
            results.Count.ShouldBe(2);
            results.All(t => t.FirstName == "John").ShouldBeTrue();
        }

        [Fact]
        public async Task SearchTeachersAsync_BySpecialization_ShouldReturnMatchingTeachers()
        {
            // Arrange
            var teachers = new[]
            {
                CreateTestTeacher("John", "Doe", "john@test.com", "1111111111", "Mathematics"),
                CreateTestTeacher("John", "Smith", "jane@test.com", "2222222222", "Mathematics"),
                CreateTestTeacher("Bob", "Johnson", "bob@test.com", "3333333333", "Physics")
            };

            _context.Teachers.AddRange(teachers);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _repository.SearchTeachersAsync("Mathematics")).ToList();

            // Assert
            results.Count.ShouldBe(2);
            results.Select(t => t.Specialization).All(s => s == "Mathematics").ShouldBeTrue();
        }

        [Fact]
        public async Task SearchTeachersAsync_ByEmail_ShouldReturnMatchingTeachers()
        {
            // Arrange
            var teachers = new[]
            {
                CreateTestTeacher("John", "Doe", "john.doe@university.com", "1111111111", "Math"),
                CreateTestTeacher("Jane", "Smith", "jane.smith@university.com", "2222222222", "Science"),
                CreateTestTeacher("Bob", "Johnson", "bob.johnson@college.com", "3333333333", "History")
            };

            _context.Teachers.AddRange(teachers);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _repository.SearchTeachersAsync("@university.com")).ToList();

            // Assert
            results.Count.ShouldBe(2);
            results.Select(t => t.Email).ShouldContain("john.doe@university.com");
            results.Select(t => t.Email).ShouldContain("jane.smith@university.com");
        }

        [Fact]
        public async Task SearchTeachersAsync_WithEmptySearchTerm_ShouldReturnAllTeachers()
        {
            // Arrange
            var teachers = new[]
            {
                CreateTestTeacher("John", "Doe"),
                CreateTestTeacher("Jane", "Smith"),
                CreateTestTeacher("Bob", "Johnson")
            };

            _context.Teachers.AddRange(teachers);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _repository.SearchTeachersAsync("")).ToList();

            // Assert
            results.Count.ShouldBe(3);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateTeacher()
        {
            // Arrange
            var teacher = CreateTestTeacher("Original", "Name", "original@test.com", "1111111111", "Original");
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            // Detach to simulate fresh context
            _context.Entry(teacher).State = EntityState.Detached;

            // Update
            teacher.Update("Updated", "Name", "updated@test.com", "2222222222", "Updated Specialization");

            // Act
            await _repository.UpdateAsync(teacher);
            await _context.SaveChangesAsync();

            // Assert
            var updated = await _repository.GetByIdAsync(teacher.Id);
            updated.FirstName.ShouldBe("Updated");
            updated.LastName.ShouldBe("Name");
            updated.Email.ShouldBe("updated@test.com");
            updated.Phone.ShouldBe("2222222222");
            updated.Specialization.ShouldBe("Updated Specialization");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveTeacher()
        {
            // Arrange
            var teacher = CreateTestTeacher("Delete", "Me", "delete@test.com", "9999999999", "ToBeDeleted");
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(teacher.Id);
            await _context.SaveChangesAsync();

            // Assert
            var deleted = await _repository.GetByIdAsync(teacher.Id);
            deleted.ShouldBeNull();
        }
    }
}