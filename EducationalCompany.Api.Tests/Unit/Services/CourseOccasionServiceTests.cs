// Note:
// AI-assisted tools were used to help generate and structure parts of these unit tests.
// All tests have been reviewed, validated, and verified manually to ensure correctness
// and proper coverage of the intended functionality.

using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Application.Services;
using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Infrastructure;
using EducationalCompany.Api.Infrastructure.Repositories;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EducationalCompany.Tests.Unit.Services
{
    public class CourseOccasionServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICourseOccasionRepository> _mockCourseOccasionRepo;
        private readonly Mock<ICourseRepository> _mockCourseRepo;
        private readonly Mock<ITeacherRepository> _mockTeacherRepo;
        private readonly Mock<ICourseRegistrationRepository> _mockCourseRegistrationRepo;
        private readonly CourseOccasionService _service;

        public CourseOccasionServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCourseOccasionRepo = new Mock<ICourseOccasionRepository>();
            _mockCourseRepo = new Mock<ICourseRepository>();
            _mockTeacherRepo = new Mock<ITeacherRepository>();
            _mockCourseRegistrationRepo = new Mock<ICourseRegistrationRepository>();

            _mockUnitOfWork.Setup(u => u.CourseOccasions).Returns(_mockCourseOccasionRepo.Object);
            _mockUnitOfWork.Setup(u => u.Courses).Returns(_mockCourseRepo.Object);
            _mockUnitOfWork.Setup(u => u.Teachers).Returns(_mockTeacherRepo.Object);
            _mockUnitOfWork.Setup(u => u.CourseRegistrations).Returns(_mockCourseRegistrationRepo.Object);

            _service = new CourseOccasionService(_mockUnitOfWork.Object);
        }

        // Helper method to create a CourseOccasion instance for testing
        private CourseOccasion CreateTestOccasion(
            Guid? courseId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int maxParticipants = 30,
            Guid? teacherId = null)
        {
            // Use reflection or a test-specific constructor if available
            // Since we can't see the actual constructor, we'll assume it exists
            // You may need to adjust this based on the actual constructor
            var occasion = new CourseOccasion(
                courseId ?? Guid.NewGuid(),
                startDate ?? DateTime.Now.AddDays(10),
                endDate ?? DateTime.Now.AddDays(20),
                maxParticipants);

            // If TeacherId can be set via property
            if (teacherId.HasValue)
            {
                var teacherIdProperty = typeof(CourseOccasion).GetProperty("TeacherId");
                teacherIdProperty?.SetValue(occasion, teacherId.Value);
            }

            return occasion;
        }

        [Fact]
        public async Task GetAllOccasionsAsync_ShouldReturnAllOccasions()
        {
            // Arrange
            var occasions = new List<CourseOccasion>
            {
                CreateTestOccasion(),
                CreateTestOccasion()
            };

            _mockCourseOccasionRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(occasions);

            // Setup MapToDto for each occasion
            foreach (var occasion in occasions)
            {
                SetupMapToDtoForOccasion(occasion);
            }

            // Act
            var result = await _service.GetAllOccasionsAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Count().ShouldBe(2);

            _mockCourseOccasionRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetOccasionByIdAsync_WithValidId_ShouldReturnOccasion()
        {
            // Arrange
            var occasionId = Guid.NewGuid();
            var occasion = CreateTestOccasion();

            _mockCourseOccasionRepo.Setup(r => r.GetByIdAsync(occasionId))
                .ReturnsAsync(occasion);

            SetupMapToDtoForOccasion(occasion);

            // Act
            var result = await _service.GetOccasionByIdAsync(occasionId);

            // Assert
            result.ShouldNotBeNull();

            _mockCourseOccasionRepo.Verify(r => r.GetByIdAsync(occasionId), Times.Once);
        }

        [Fact]
        public async Task GetOccasionByIdAsync_WithInvalidId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _mockCourseOccasionRepo.Setup(r => r.GetByIdAsync(invalidId))
                .ReturnsAsync((CourseOccasion)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.GetOccasionByIdAsync(invalidId));

            exception.Message.ShouldContain(invalidId.ToString());
        }

        [Fact]
        public async Task CreateOccasionAsync_WithValidData_ShouldCreateOccasion()
        {
            // Arrange
            var courseId = Guid.NewGuid();
            var createDto = new CreateCourseOccasionDto
            {
                CourseId = courseId,
                StartDate = DateTime.Now.AddDays(10),
                EndDate = DateTime.Now.AddDays(20),
                MaxParticipants = 30
            };

            var course = new Course("Test Course", "Description", 40, 1000m);

            // Setup course repository
            _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId))
                .ReturnsAsync(course);

            // Setup the add method
            _mockCourseOccasionRepo.Setup(r => r.AddAsync(It.IsAny<CourseOccasion>()))
                .Returns(Task.CompletedTask);

            // Setup GetWithRegistrationsAsync to return the created occasion
            var createdOccasion = new CourseOccasion(courseId, createDto.StartDate, createDto.EndDate, createDto.MaxParticipants);
            _mockCourseOccasionRepo.Setup(r => r.GetWithRegistrationsAsync(It.IsAny<Guid>()))
                .ReturnsAsync(createdOccasion);

            // Setup MapToDto dependencies
            SetupMapToDtoForOccasion(createdOccasion);

            // Act
            var result = await _service.CreateOccasionAsync(createDto);

            // Assert
            result.ShouldNotBeNull();

            _mockCourseRepo.Verify(r => r.GetByIdAsync(courseId), Times.Once); // Change from Once 
            _mockCourseOccasionRepo.Verify(r => r.AddAsync(It.IsAny<CourseOccasion>()), Times.Once);
        }
        [Fact]
        public async Task CreateOccasionAsync_WithNonExistentCourse_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var createDto = new CreateCourseOccasionDto
            {
                CourseId = Guid.NewGuid(),
                StartDate = DateTime.Now.AddDays(10),
                EndDate = DateTime.Now.AddDays(20),
                MaxParticipants = 30
            };

            _mockCourseRepo.Setup(r => r.GetByIdAsync(createDto.CourseId))
                .ReturnsAsync((Course)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.CreateOccasionAsync(createDto));

            exception.Message.ShouldContain(createDto.CourseId.ToString());
        }

        [Fact]
        public async Task UpdateOccasionAsync_WithValidDataAndNoRegistrations_ShouldUpdateOccasion()
        {
            // Arrange
            var occasionId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var occasion = CreateTestOccasion(courseId: courseId);
            var course = new Course("Test Course", "Description", 40, 1000m);

            var updateDto = new CreateCourseOccasionDto
            {
                CourseId = courseId,
                StartDate = DateTime.Now.AddDays(15),
                EndDate = DateTime.Now.AddDays(25),
                MaxParticipants = 25
            };

            _mockCourseOccasionRepo.Setup(r => r.GetByIdAsync(occasionId))
                .ReturnsAsync(occasion);

            _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId))
                .ReturnsAsync(course);

            _mockCourseRegistrationRepo.Setup(r => r.GetRegistrationsByOccasionAsync(occasionId))
                .ReturnsAsync(new List<CourseRegistration>());

            // Act
            await _service.UpdateOccasionAsync(occasionId, updateDto);

            // Assert
            _mockCourseOccasionRepo.Verify(r => r.UpdateAsync(occasion), Times.Once);
        }

        [Fact]
        public async Task UpdateOccasionAsync_WithNonExistentOccasion_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var occasionId = Guid.NewGuid();
            var updateDto = new CreateCourseOccasionDto
            {
                CourseId = Guid.NewGuid(),
                StartDate = DateTime.Now.AddDays(15),
                EndDate = DateTime.Now.AddDays(25),
                MaxParticipants = 25
            };

            _mockCourseOccasionRepo.Setup(r => r.GetByIdAsync(occasionId))
                .ReturnsAsync((CourseOccasion)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.UpdateOccasionAsync(occasionId, updateDto));

            exception.Message.ShouldContain(occasionId.ToString());
        }

        [Fact]
        public async Task UpdateOccasionAsync_WithExistingRegistrations_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var occasionId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var occasion = CreateTestOccasion(courseId: courseId);
            var course = new Course("Test Course", "Description", 40, 1000m);

            var updateDto = new CreateCourseOccasionDto
            {
                CourseId = courseId,
                StartDate = DateTime.Now.AddDays(15),
                EndDate = DateTime.Now.AddDays(25),
                MaxParticipants = 25
            };

            var registrations = new List<CourseRegistration>
            {
                new CourseRegistration(Guid.NewGuid(), occasionId)
            };

            _mockCourseOccasionRepo.Setup(r => r.GetByIdAsync(occasionId))
                .ReturnsAsync(occasion);

            _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId))
                .ReturnsAsync(course);

            _mockCourseRegistrationRepo.Setup(r => r.GetRegistrationsByOccasionAsync(occasionId))
                .ReturnsAsync(registrations);

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.UpdateOccasionAsync(occasionId, updateDto));

            exception.Message.ShouldContain("Cannot update occasion that has registrations");
        }

        [Fact]
        public async Task DeleteOccasionAsync_WithValidIdAndNoRegistrations_ShouldDeleteOccasion()
        {
            // Arrange
            var occasionId = Guid.NewGuid();
            var occasion = CreateTestOccasion();

            _mockCourseOccasionRepo.Setup(r => r.GetByIdAsync(occasionId))
                .ReturnsAsync(occasion);

            _mockCourseRegistrationRepo.Setup(r => r.GetRegistrationsByOccasionAsync(occasionId))
                .ReturnsAsync(new List<CourseRegistration>());

            // Act
            await _service.DeleteOccasionAsync(occasionId);

            // Assert
            _mockCourseOccasionRepo.Verify(r => r.DeleteAsync(occasionId), Times.Once);
        }

        [Fact]
        public async Task DeleteOccasionAsync_WithExistingRegistrations_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var occasionId = Guid.NewGuid();
            var occasion = CreateTestOccasion();

            var registrations = new List<CourseRegistration>
            {
                new CourseRegistration(Guid.NewGuid(), occasionId)
            };

            _mockCourseOccasionRepo.Setup(r => r.GetByIdAsync(occasionId))
                .ReturnsAsync(occasion);

            _mockCourseRegistrationRepo.Setup(r => r.GetRegistrationsByOccasionAsync(occasionId))
                .ReturnsAsync(registrations);

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.DeleteOccasionAsync(occasionId));

            exception.Message.ShouldContain("Cannot delete occasion that has registrations");

            _mockCourseOccasionRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetByCourseIdAsync_ShouldReturnCourseOccasions()
        {
            // Arrange
            var courseId = Guid.NewGuid();
            var occasions = new List<CourseOccasion>
            {
                CreateTestOccasion(courseId: courseId),
                CreateTestOccasion(courseId: courseId)
            };

            _mockCourseOccasionRepo.Setup(r => r.GetByCourseIdAsync(courseId))
                .ReturnsAsync(occasions);

            foreach (var occasion in occasions)
            {
                SetupMapToDtoForOccasion(occasion);
            }

            // Act
            var result = await _service.GetOccasionsByCourseIdAsync(courseId);

            // Assert
            result.ShouldNotBeNull();
            result.Count().ShouldBe(2);

            _mockCourseOccasionRepo.Verify(r => r.GetByCourseIdAsync(courseId), Times.Once);
        }

        [Fact]
        public async Task GetUpcomingOccasionsAsync_ShouldReturnUpcomingOccasions()
        {
            // Arrange
            var occasions = new List<CourseOccasion>
            {
                CreateTestOccasion(startDate: DateTime.Now.AddDays(5)),
                CreateTestOccasion(startDate: DateTime.Now.AddDays(10))
            };

            _mockCourseOccasionRepo.Setup(r => r.GetUpcomingOccasionsAsync())
                .ReturnsAsync(occasions);

            foreach (var occasion in occasions)
            {
                SetupMapToDtoForOccasion(occasion);
            }

            // Act
            var result = await _service.GetUpComingOccasionsAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Count().ShouldBe(2);

            _mockCourseOccasionRepo.Verify(r => r.GetUpcomingOccasionsAsync(), Times.Once);
        }

        [Fact]
        public async Task AssignTeacherAsync_WithValidData_ShouldAssignTeacher()
        {
            // Arrange
            var occasionId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var assignDto = new AssignTeacherDto { TeacherId = teacherId };

            var occasion = CreateTestOccasion();
            var teacher = new Teacher("John", "Doe", "john.teacher@test.com", "1234567890", "Math");

            _mockCourseOccasionRepo.Setup(r => r.GetByIdAsync(occasionId))
                .ReturnsAsync(occasion);

            _mockTeacherRepo.Setup(r => r.GetByIdAsync(teacherId))
                .ReturnsAsync(teacher);

            // Act
            await _service.AssignTeacherAsync(occasionId, assignDto);

            // Assert
            _mockCourseOccasionRepo.Verify(r => r.UpdateAsync(occasion), Times.Once);
        }

        [Fact]
        public async Task AssignTeacherAsync_WithNonExistentOccasion_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var occasionId = Guid.NewGuid();
            var assignDto = new AssignTeacherDto { TeacherId = Guid.NewGuid() };

            _mockCourseOccasionRepo.Setup(r => r.GetByIdAsync(occasionId))
                .ReturnsAsync((CourseOccasion)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.AssignTeacherAsync(occasionId, assignDto));

            exception.Message.ShouldContain(occasionId.ToString());
        }

        [Fact]
        public async Task AssignTeacherAsync_WithNonExistentTeacher_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var occasionId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var assignDto = new AssignTeacherDto { TeacherId = teacherId };

            var occasion = CreateTestOccasion();

            _mockCourseOccasionRepo.Setup(r => r.GetByIdAsync(occasionId))
                .ReturnsAsync(occasion);

            _mockTeacherRepo.Setup(r => r.GetByIdAsync(teacherId))
                .ReturnsAsync((Teacher)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.AssignTeacherAsync(occasionId, assignDto));

            exception.Message.ShouldContain(teacherId.ToString());
        }

        [Fact]
        public async Task GetOccasionWithRegistrationsAsync_WithValidId_ShouldReturnOccasionWithRegistrations()
        {
            // Arrange
            var occasionId = Guid.NewGuid();
            var occasion = CreateTestOccasion();

            _mockCourseOccasionRepo.Setup(r => r.GetWithRegistrationsAsync(occasionId))
                .ReturnsAsync(occasion);

            SetupMapToDtoForOccasion(occasion);

            // Act
            var result = await _service.GetOccasionWithRegistrationsAsync(occasionId);

            // Assert
            result.ShouldNotBeNull();

            _mockCourseOccasionRepo.Verify(r => r.GetWithRegistrationsAsync(occasionId), Times.Once);
        }

        [Fact]
        public async Task IsOccasionFullAsync_ShouldReturnTrueWhenFull()
        {
            // Arrange
            var occasionId = Guid.NewGuid();

            _mockCourseOccasionRepo.Setup(r => r.IsOccasionFullAsync(occasionId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.IsOccasionFullAsync(occasionId);

            // Assert
            result.ShouldBeTrue();

            _mockCourseOccasionRepo.Verify(r => r.IsOccasionFullAsync(occasionId), Times.Once);
        }

        [Fact]
        public async Task IsOccasionFullAsync_ShouldReturnFalseWhenNotFull()
        {
            // Arrange
            var occasionId = Guid.NewGuid();

            _mockCourseOccasionRepo.Setup(r => r.IsOccasionFullAsync(occasionId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.IsOccasionFullAsync(occasionId);

            // Assert
            result.ShouldBeFalse();
        }

        // Helper method to setup MapToDto
        private void SetupMapToDtoForOccasion(CourseOccasion occasion)
        {
            // Setup course if needed
            if (occasion.CourseId != Guid.Empty)
            {
                var course = new Course("Test Course", "Description", 40, 1000m);
                _mockCourseRepo.Setup(r => r.GetByIdAsync(occasion.CourseId))
                    .ReturnsAsync(course);
            }

            // Setup teacher if needed
            if (occasion.TeacherId.HasValue && occasion.TeacherId.Value != Guid.Empty)
            {
                var teacher = new Teacher("Test", "Teacher", "teacher@test.com", "1234567890", "Math");
                _mockTeacherRepo.Setup(r => r.GetByIdAsync(occasion.TeacherId.Value))
                    .ReturnsAsync(teacher);
            }
        }
    }
}