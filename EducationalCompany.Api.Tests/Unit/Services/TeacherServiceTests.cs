// Note:
// AI-assisted tools were used to help generate and structure parts of these unit tests.
// All tests have been reviewed, validated, and verified manually to ensure correctness
// and proper coverage of the intended functionality.

using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Application.DTOs;
using EducationalCompany.Application.Services;
using EducationalCompany.Domain.Entities;
using EducationalCompany.Infrastructure;
using EducationalCompany.Infrastructure.Repositories;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EducationalCompany.Tests.Unit.Services
{
    public class TeacherServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITeacherRepository> _mockTeacherRepo;
        private readonly Mock<ICourseOccasionRepository> _mockCourseOccasionRepo;
        private readonly TeacherService _service;

        public TeacherServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTeacherRepo = new Mock<ITeacherRepository>();
            _mockCourseOccasionRepo = new Mock<ICourseOccasionRepository>();

            _mockUnitOfWork.Setup(u => u.Teachers).Returns(_mockTeacherRepo.Object);
            _mockUnitOfWork.Setup(u => u.CourseOccasions).Returns(_mockCourseOccasionRepo.Object);

            _service = new TeacherService(_mockUnitOfWork.Object);
        }

        // Helper method to create a test teacher
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
                email ?? $"john.doe.{Guid.NewGuid()}@test.com",
                phone,
                specialization);
        }

        [Fact]
        public async Task GetAllTeachersAsync_ShouldReturnAllTeachers()
        {
            // Arrange
            var teachers = new List<Teacher>
            {
                CreateTestTeacher("John", "Doe", "john@test.com"),
                CreateTestTeacher("Jane", "Smith", "jane@test.com"),
                CreateTestTeacher("Bob", "Johnson", "bob@test.com")
            };

            _mockTeacherRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(teachers);

            // Act
            var result = await _service.GetAllTeachersAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Count().ShouldBe(3);
            result.First().FirstName.ShouldBe("John");
            result.Last().LastName.ShouldBe("Johnson");

            _mockTeacherRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllTeachersAsync_WhenNoTeachers_ShouldReturnEmptyList()
        {
            // Arrange
            _mockTeacherRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Teacher>());

            // Act
            var result = await _service.GetAllTeachersAsync();

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetTeacherByIdAsync_WithValidId_ShouldReturnTeacher()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var teacher = CreateTestTeacher("John", "Doe", "john@test.com");

            _mockTeacherRepo.Setup(r => r.GetByIdAsync(teacherId))
                .ReturnsAsync(teacher);

            // Act
            var result = await _service.GetTeacherByIdAsync(teacherId);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(teacher.Id);
            result.FirstName.ShouldBe("John");
            result.LastName.ShouldBe("Doe");
            result.Email.ShouldBe("john@test.com");

            _mockTeacherRepo.Verify(r => r.GetByIdAsync(teacherId), Times.Once);
        }

        [Fact]
        public async Task GetTeacherByIdAsync_WithInvalidId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _mockTeacherRepo.Setup(r => r.GetByIdAsync(invalidId))
                .ReturnsAsync((Teacher)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.GetTeacherByIdAsync(invalidId));

            exception.Message.ShouldContain(invalidId.ToString());
        }

        [Fact]
        public async Task CreateTeacherAsync_WithValidDto_ShouldCreateAndReturnTeacher()
        {
            // Arrange
            var createDto = new CreateTeacherDto
            {
                FirstName = "New",
                LastName = "Teacher",
                Email = "new.teacher@test.com",
                Phone = "5551234567",
                Specialization = "Physics"
            };

            _mockTeacherRepo.Setup(r => r.SearchTeachersAsync(createDto.Email))
                .ReturnsAsync(new List<Teacher>());

            Teacher capturedTeacher = null;
            _mockTeacherRepo.Setup(r => r.AddAsync(It.IsAny<Teacher>()))
                .Callback<Teacher>(t => capturedTeacher = t)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateTeacherAsync(createDto);

            // Assert
            result.ShouldNotBeNull();
            result.FirstName.ShouldBe(createDto.FirstName);
            result.LastName.ShouldBe(createDto.LastName);
            result.Email.ShouldBe(createDto.Email);
            result.Phone.ShouldBe(createDto.Phone);
            result.Specialization.ShouldBe(createDto.Specialization);

            _mockTeacherRepo.Verify(r => r.SearchTeachersAsync(createDto.Email), Times.Once);
            _mockTeacherRepo.Verify(r => r.AddAsync(It.IsAny<Teacher>()), Times.Once);

            capturedTeacher.ShouldNotBeNull();
            capturedTeacher.Email.ShouldBe(createDto.Email);
            capturedTeacher.Specialization.ShouldBe(createDto.Specialization);
        }

        [Fact]
        public async Task CreateTeacherAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var createDto = new CreateTeacherDto
            {
                FirstName = "Duplicate",
                LastName = "Teacher",
                Email = "duplicate@test.com",
                Phone = "5551234567",
                Specialization = "Chemistry"
            };

            var existingTeachers = new List<Teacher>
            {
                CreateTestTeacher("Existing", "Teacher", "duplicate@test.com")
            };

            _mockTeacherRepo.Setup(r => r.SearchTeachersAsync(createDto.Email))
                .ReturnsAsync(existingTeachers);

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.CreateTeacherAsync(createDto));

            exception.Message.ShouldContain("already exists");

            _mockTeacherRepo.Verify(r => r.AddAsync(It.IsAny<Teacher>()), Times.Never);
        }

        [Fact]
        public async Task UpdateTeacherAsync_WithValidData_ShouldUpdateTeacher()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var existingTeacher = CreateTestTeacher(
                "Old",
                "Name",
                "old@test.com",
                "1112223333",
                "Old Specialization");

            var updateDto = new UpdateTeacherDto
            {
                FirstName = "Updated",
                LastName = "Name",
                Email = "updated@test.com",
                Phone = "4445556666",
                Specialization = "Updated Specialization"
            };

            _mockTeacherRepo.Setup(r => r.GetByIdAsync(teacherId))
                .ReturnsAsync(existingTeacher);

            _mockTeacherRepo.Setup(r => r.SearchTeachersAsync(updateDto.Email))
                .ReturnsAsync(new List<Teacher>()); // No existing with this email

            // Act
            await _service.UpdateTeacherAsync(teacherId, updateDto);

            // Assert
            _mockTeacherRepo.Verify(r => r.GetByIdAsync(teacherId), Times.Once);
            _mockTeacherRepo.Verify(r => r.SearchTeachersAsync(updateDto.Email), Times.Once);
            _mockTeacherRepo.Verify(r => r.UpdateAsync(existingTeacher), Times.Once);

            // Verify the teacher was updated
            existingTeacher.FirstName.ShouldBe(updateDto.FirstName);
            existingTeacher.LastName.ShouldBe(updateDto.LastName);
            existingTeacher.Email.ShouldBe(updateDto.Email);
            existingTeacher.Phone.ShouldBe(updateDto.Phone);
            existingTeacher.Specialization.ShouldBe(updateDto.Specialization);
        }

        [Fact]
        public async Task UpdateTeacherAsync_WithSameEmail_ShouldNotCheckForDuplicates()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var existingTeacher = CreateTestTeacher(
                "Old",
                "Name",
                "same@test.com",
                "1112223333",
                "Specialization");

            var updateDto = new UpdateTeacherDto
            {
                FirstName = "Updated",
                LastName = "Name",
                Email = "same@test.com", // Same email
                Phone = "4445556666",
                Specialization = "Updated Specialization"
            };

            _mockTeacherRepo.Setup(r => r.GetByIdAsync(teacherId))
                .ReturnsAsync(existingTeacher);

            // Act
            await _service.UpdateTeacherAsync(teacherId, updateDto);

            // Assert
            _mockTeacherRepo.Verify(r => r.GetByIdAsync(teacherId), Times.Once);
            _mockTeacherRepo.Verify(r => r.SearchTeachersAsync(It.IsAny<string>()), Times.Never);
            _mockTeacherRepo.Verify(r => r.UpdateAsync(existingTeacher), Times.Once);
        }

        [Fact]
        public async Task UpdateTeacherAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var existingTeacher = CreateTestTeacher(
                "Old",
                "Name",
                "old@test.com",
                "1112223333",
                "Specialization");

            var anotherTeacherId = Guid.NewGuid();
            var anotherTeacher = CreateTestTeacher(
                "Another",
                "Teacher",
                "duplicate@test.com",
                "9998887777",
                "Other Specialization");

            // Set the Id of anotherTeacher (since it's generated in constructor)
            var idProperty = typeof(Teacher).GetProperty("Id");
            idProperty?.SetValue(anotherTeacher, anotherTeacherId);

            var updateDto = new UpdateTeacherDto
            {
                FirstName = "Updated",
                LastName = "Name",
                Email = "duplicate@test.com", // Email that belongs to another teacher
                Phone = "4445556666",
                Specialization = "Updated Specialization"
            };

            _mockTeacherRepo.Setup(r => r.GetByIdAsync(teacherId))
                .ReturnsAsync(existingTeacher);

            _mockTeacherRepo.Setup(r => r.SearchTeachersAsync(updateDto.Email))
                .ReturnsAsync(new List<Teacher> { anotherTeacher });

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.UpdateTeacherAsync(teacherId, updateDto));

            exception.Message.ShouldContain("already exists");

            _mockTeacherRepo.Verify(r => r.UpdateAsync(It.IsAny<Teacher>()), Times.Never);
        }

        [Fact]
        public async Task UpdateTeacherAsync_WithNonExistentTeacher_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var updateDto = new UpdateTeacherDto
            {
                FirstName = "Test",
                LastName = "Teacher",
                Email = "test@test.com",
                Phone = "1234567890",
                Specialization = "Test Specialization"
            };

            _mockTeacherRepo.Setup(r => r.GetByIdAsync(nonExistentId))
                .ReturnsAsync((Teacher)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.UpdateTeacherAsync(nonExistentId, updateDto));

            exception.Message.ShouldContain(nonExistentId.ToString());
        }

        [Fact]
        public async Task DeleteTeacherAsync_WithValidIdAndNoAssignments_ShouldDeleteTeacher()
        {
            var teacherId = Guid.NewGuid();
            var teacher = CreateTestTeacher();

            _mockTeacherRepo.Setup(r => r.GetByIdAsync(teacherId))
                .ReturnsAsync(teacher);

            _mockCourseOccasionRepo.Setup(r => r.GetByTeacherIdAsync(teacherId))
                .ReturnsAsync(new List<CourseOccasion>()); // EMPTY

            await _service.DeleteTeacherAsync(teacherId);

            _mockTeacherRepo.Verify(r => r.GetByIdAsync(teacherId), Times.Once);
            _mockCourseOccasionRepo.Verify(r => r.GetByTeacherIdAsync(teacherId), Times.Once);
            _mockTeacherRepo.Verify(r => r.DeleteAsync(teacherId), Times.Once);
        }

        [Fact]
        public async Task DeleteTeacherAsync_WithExistingAssignments_ShouldThrowInvalidOperationException()
        {
            // arrange 
            var teacherId = Guid.NewGuid();
            var teacher = CreateTestTeacher();

            var occasions = new List<CourseOccasion>
            {
        new CourseOccasion(
            Guid.NewGuid(),
            DateTime.Now.AddDays(10),
            DateTime.Now.AddDays(20),
            30)
          };

            _mockTeacherRepo.Setup(r => r.GetByIdAsync(teacherId))
                .ReturnsAsync(teacher);

            _mockCourseOccasionRepo.Setup(r => r.GetByTeacherIdAsync(teacherId))
                .ReturnsAsync(occasions);

            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.DeleteTeacherAsync(teacherId));

            exception.Message.ShouldContain("Cannot delete teacher who is assigned to course occasions");

            _mockTeacherRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteTeacherAsync_WithNonExistentTeacher_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            _mockTeacherRepo.Setup(r => r.GetByIdAsync(nonExistentId))
                .ReturnsAsync((Teacher)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.DeleteTeacherAsync(nonExistentId));

            exception.Message.ShouldContain(nonExistentId.ToString());

            _mockCourseOccasionRepo.Verify(r => r.GetByCourseIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockTeacherRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task SearchTeachersAsync_WithValidTerm_ShouldReturnMatchingTeachers()
        {
            // Arrange
            var searchTerm = "john";
            var teachers = new List<Teacher>
            {
                CreateTestTeacher("John", "Doe", "john@test.com"),
                CreateTestTeacher("Johnny", "Smith", "johnny@test.com"),
                CreateTestTeacher("Jane", "Johnson", "jane@test.com") // Won't match
            };

            var matchingTeachers = teachers.Take(2).ToList();

            _mockTeacherRepo.Setup(r => r.SearchTeachersAsync(searchTerm))
                .ReturnsAsync(matchingTeachers);

            // Act
            var result = await _service.SearchTeachersAsync(searchTerm);

            // Assert
            result.ShouldNotBeNull();
            result.Count().ShouldBe(2);
            result.All(t => t.FirstName.Contains("John") || t.FirstName.Contains("Johnny")).ShouldBeTrue();

            _mockTeacherRepo.Verify(r => r.SearchTeachersAsync(searchTerm), Times.Once);
        }

        [Fact]
        public async Task SearchTeachersAsync_WithNoMatches_ShouldReturnEmptyList()
        {
            // Arrange
            var searchTerm = "nonexistent";

            _mockTeacherRepo.Setup(r => r.SearchTeachersAsync(searchTerm))
                .ReturnsAsync(new List<Teacher>());

            // Act
            var result = await _service.SearchTeachersAsync(searchTerm);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetTeacherWithOccasionsAsync_WithValidId_ShouldReturnTeacherWithOccasions()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var teacher = CreateTestTeacher();

            _mockTeacherRepo.Setup(r => r.GetTeacherWithOccasionsAsync(teacherId))
                .ReturnsAsync(teacher);

            // Act
            var result = await _service.GetTeacherWithOccasionsAsync(teacherId);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(teacher.Id);
            result.FirstName.ShouldBe(teacher.FirstName);
            result.LastName.ShouldBe(teacher.LastName);
            result.Email.ShouldBe(teacher.Email);

            _mockTeacherRepo.Verify(r => r.GetTeacherWithOccasionsAsync(teacherId), Times.Once);
        }

        [Fact]
        public async Task GetTeacherWithOccasionsAsync_WithInvalidId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            _mockTeacherRepo.Setup(r => r.GetTeacherWithOccasionsAsync(invalidId))
                .ReturnsAsync((Teacher)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.GetTeacherWithOccasionsAsync(invalidId));

            exception.Message.ShouldContain(invalidId.ToString());
        }

        [Fact]
        public void MapToDto_ShouldMapAllPropertiesCorrectly()
        {
            // This tests the private MapToDto method through public methods
            // Arrange
            var teacher = CreateTestTeacher(
                "Test",
                "Mapping",
                "test.mapping@test.com",
                "5555555555",
                "Test Specialization");

            _mockTeacherRepo.Setup(r => r.GetByIdAsync(teacher.Id))
                .ReturnsAsync(teacher);

            // Act
            var result = _service.GetTeacherByIdAsync(teacher.Id).Result;

            // Assert
            result.Id.ShouldBe(teacher.Id);
            result.FirstName.ShouldBe(teacher.FirstName);
            result.LastName.ShouldBe(teacher.LastName);
            result.Email.ShouldBe(teacher.Email);
            result.Phone.ShouldBe(teacher.Phone);
            result.Specialization.ShouldBe(teacher.Specialization);
            result.CreatedDate.ShouldBe(teacher.CreatedDate);
            result.UpdatedDate.ShouldBe(teacher.UpdatedDate);
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
            var searchTerm = "";
            var allTeachers = new List<Teacher>
            {
                CreateTestTeacher("John", "Doe", "john@test.com"),
                CreateTestTeacher("Jane", "Smith", "jane@test.com")
            };

            _mockTeacherRepo.Setup(r => r.SearchTeachersAsync(searchTerm))
                .ReturnsAsync(allTeachers);

            // Act
            var result = await _service.SearchTeachersAsync(searchTerm);

            // Assert
            result.ShouldNotBeNull();
            result.Count().ShouldBe(2);
        }

        [Fact]
        public async Task CreateTeacherAsync_ShouldSetCorrectProperties()
        {
            // Arrange
            var createDto = new CreateTeacherDto
            {
                FirstName = "Property",
                LastName = "Test",
                Email = "property.test@test.com",
                Phone = "7778889999",
                Specialization = "Property Testing"
            };

            _mockTeacherRepo.Setup(r => r.SearchTeachersAsync(createDto.Email))
                .ReturnsAsync(new List<Teacher>());

            Teacher capturedTeacher = null;
            _mockTeacherRepo.Setup(r => r.AddAsync(It.IsAny<Teacher>()))
                .Callback<Teacher>(t => capturedTeacher = t)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateTeacherAsync(createDto);

            // Assert
            capturedTeacher.ShouldNotBeNull();
            capturedTeacher.FirstName.ShouldBe(createDto.FirstName);
            capturedTeacher.LastName.ShouldBe(createDto.LastName);
            capturedTeacher.Email.ShouldBe(createDto.Email);
            capturedTeacher.Phone.ShouldBe(createDto.Phone);
            capturedTeacher.Specialization.ShouldBe(createDto.Specialization);
            capturedTeacher.CreatedDate.ShouldNotBe(default);
            capturedTeacher.UpdatedDate.ShouldBe(default);
        }
    }
}