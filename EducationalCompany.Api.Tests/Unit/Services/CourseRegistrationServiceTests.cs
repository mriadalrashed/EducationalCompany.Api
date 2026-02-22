// Note:
// AI-assisted tools were used to help generate and structure parts of these unit tests.
// All tests have been reviewed, validated, and verified manually to ensure correctness
// and proper coverage of the intended functionality.

using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Application.DTOs;
using EducationalCompany.Application.Interfaces;
using EducationalCompany.Application.Services;
using EducationalCompany.Domain.Entities;
using EducationalCompany.Infrastructure;
using EducationalCompany.Infrastructure.Repositories;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace EducationalCompany.Tests.Unit.Services
{
    public class CourseRegistrationServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICourseRegistrationRepository> _mockRegistrationRepo;
        private readonly Mock<IParticipantRepository> _mockParticipantRepo;
        private readonly Mock<ICourseOccasionRepository> _mockCourseOccasionRepo;
        private readonly Mock<ICourseOccasionService> _mockCourseOccasionService;
        private readonly CourseRegistrationService _service;

        public CourseRegistrationServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockRegistrationRepo = new Mock<ICourseRegistrationRepository>();
            _mockParticipantRepo = new Mock<IParticipantRepository>();
            _mockCourseOccasionRepo = new Mock<ICourseOccasionRepository>();
            _mockCourseOccasionService = new Mock<ICourseOccasionService>();

            _mockUnitOfWork.Setup(u => u.CourseRegistrations).Returns(_mockRegistrationRepo.Object);
            _mockUnitOfWork.Setup(u => u.Participants).Returns(_mockParticipantRepo.Object);
            _mockUnitOfWork.Setup(u => u.CourseOccasions).Returns(_mockCourseOccasionRepo.Object);

            _service = new CourseRegistrationService(_mockUnitOfWork.Object, _mockCourseOccasionService.Object);
        }

        [Fact]
        public async Task GetAllRegistrationsAsync_ShouldReturnAllRegistrations()
        {
            // Arrange
            var registrations = new List<CourseRegistration>
            {
                new CourseRegistration(Guid.NewGuid(), Guid.NewGuid()),
                new CourseRegistration(Guid.NewGuid(), Guid.NewGuid())
            };

            _mockRegistrationRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(registrations);

            // Setup MapToDto to return DTOs
            SetupMapToDtoForRegistrations(registrations);

            // Act
            var result = await _service.GetAllRegistrationsAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Count().ShouldBe(2);

            _mockRegistrationRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetRegistrationByIdAsync_WithValidId_ShouldReturnRegistration()
        {
            // Arrange
            var registrationId = Guid.NewGuid();
            var registration = new CourseRegistration(Guid.NewGuid(), Guid.NewGuid());

            _mockRegistrationRepo.Setup(r => r.GetByIdAsync(registrationId))
                .ReturnsAsync(registration);

            SetupMapToDtoForRegistration(registration);

            // Act
            var result = await _service.GetRegistrationByIdAsync(registrationId);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(registration.Id);

            _mockRegistrationRepo.Verify(r => r.GetByIdAsync(registrationId), Times.Once);
        }

        [Fact]
        public async Task GetRegistrationByIdAsync_WithInvalidId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _mockRegistrationRepo.Setup(r => r.GetByIdAsync(invalidId))
                .ReturnsAsync((CourseRegistration)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.GetRegistrationByIdAsync(invalidId));

            exception.Message.ShouldContain(invalidId.ToString());
        }

        [Fact]
        public async Task CreateRegistrationAsync_WithValidData_ShouldCreateRegistration()
        {
            // Arrange
            var participantId = Guid.NewGuid();
            var courseOccasionId = Guid.NewGuid();
            var createDto = new CreateCourseRegistrationDto
            {
                ParticipantId = participantId,
                CourseOccasionId = courseOccasionId
            };

            var participant = new Participant("John", "Doe", "john@test.com", "1234567890", "123 Main St");
            var courseOccasion = new CourseOccasion(
                Guid.NewGuid(),
                DateTime.Now.AddDays(10),
                DateTime.Now.AddDays(20),
                30);

            // Make sure occasion is not full
            var occasionField = typeof(CourseOccasion).GetField("_currentParticipants",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            occasionField?.SetValue(courseOccasion, 0);

            _mockParticipantRepo.Setup(r => r.GetByIdAsync(participantId))
                .ReturnsAsync(participant);

            _mockCourseOccasionRepo.Setup(r => r.GetByIdAsync(courseOccasionId))
                .ReturnsAsync(courseOccasion);

            _mockRegistrationRepo.Setup(r => r.HasRegistrationAsync(participantId, courseOccasionId))
                .ReturnsAsync(false);

            _mockRegistrationRepo.Setup(r => r.AddAsync(It.IsAny<CourseRegistration>()))
                .Returns(Task.CompletedTask);

            _mockRegistrationRepo.Setup(r => r.GetRegistrationDetailsAsync(It.IsAny<Guid>()))
                .ReturnsAsync((CourseRegistration)null);

            // Act
            var result = await _service.CreateRegistrationAsync(createDto);

            // Assert
            result.ShouldNotBeNull();

            _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _mockParticipantRepo.Verify(r => r.GetByIdAsync(participantId), Times.Exactly(2));
            _mockCourseOccasionRepo.Verify(r => r.GetByIdAsync(courseOccasionId), Times.Exactly(2));
            _mockRegistrationRepo.Verify(r => r.HasRegistrationAsync(participantId, courseOccasionId), Times.Once);
            _mockRegistrationRepo.Verify(r => r.AddAsync(It.IsAny<CourseRegistration>()), Times.Once);
            _mockCourseOccasionRepo.Verify(r => r.UpdateAsync(courseOccasion), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateRegistrationAsync_WithNonExistentParticipant_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var createDto = new CreateCourseRegistrationDto
            {
                ParticipantId = Guid.NewGuid(),
                CourseOccasionId = Guid.NewGuid()
            };

            _mockParticipantRepo.Setup(r => r.GetByIdAsync(createDto.ParticipantId))
                .ReturnsAsync((Participant)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.CreateRegistrationAsync(createDto));

            exception.Message.ShouldContain(createDto.ParticipantId.ToString());

            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateRegistrationAsync_WithNonExistentCourseOccasion_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var createDto = new CreateCourseRegistrationDto
            {
                ParticipantId = Guid.NewGuid(),
                CourseOccasionId = Guid.NewGuid()
            };

            var participant = new Participant("John", "Doe", "john@test.com", "1234567890", "123 Main St");

            _mockParticipantRepo.Setup(r => r.GetByIdAsync(createDto.ParticipantId))
                .ReturnsAsync(participant);

            _mockCourseOccasionRepo.Setup(r => r.GetByIdAsync(createDto.CourseOccasionId))
                .ReturnsAsync((CourseOccasion)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.CreateRegistrationAsync(createDto));

            exception.Message.ShouldContain(createDto.CourseOccasionId.ToString());

            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        // Helper method to set private field using reflection
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        [Fact]
        public async Task CreateRegistrationAsync_WhenOccasionIsFull_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var participantId = Guid.NewGuid();
            var courseOccasionId = Guid.NewGuid();
            var createDto = new CreateCourseRegistrationDto
            {
                ParticipantId = participantId,
                CourseOccasionId = courseOccasionId
            };

            var participant = new Participant("John", "Doe", "john@test.com", "1234567890", "123 Main St");

            // Create a real CourseOccasion with max 1 participant and fill it
            var courseOccasion = new CourseOccasion(
                Guid.NewGuid(),
                DateTime.Now.AddDays(10),
                DateTime.Now.AddDays(20),
                maxParticipants: 1);

            // Set CurrentParticipants to MaxParticipants using the public TryRegisterParticipant
            courseOccasion.TryRegisterParticipant(); // now IsFull == true

            _mockParticipantRepo.Setup(r => r.GetByIdAsync(participantId))
                .ReturnsAsync(participant);

            _mockCourseOccasionRepo.Setup(r => r.GetByIdAsync(courseOccasionId))
                .ReturnsAsync(courseOccasion);

            _mockRegistrationRepo.Setup(r => r.HasRegistrationAsync(participantId, courseOccasionId))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.CreateRegistrationAsync(createDto));

            exception.Message.ShouldBe("Course occasion is full");

            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _mockRegistrationRepo.Verify(r => r.AddAsync(It.IsAny<CourseRegistration>()), Times.Never);
            _mockCourseOccasionRepo.Verify(r => r.UpdateAsync(It.IsAny<CourseOccasion>()), Times.Never);
        }




        [Fact]
        public async Task CreateRegistrationAsync_WhenParticipantAlreadyRegistered_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var createDto = new CreateCourseRegistrationDto
            {
                ParticipantId = Guid.NewGuid(),
                CourseOccasionId = Guid.NewGuid()
            };

            var participant = new Participant("John", "Doe", "john@test.com", "1234567890", "123 Main St");
            var courseOccasion = new CourseOccasion(
                Guid.NewGuid(),
                DateTime.Now.AddDays(10),
                DateTime.Now.AddDays(20),
                30);

            _mockParticipantRepo.Setup(r => r.GetByIdAsync(createDto.ParticipantId))
                .ReturnsAsync(participant);

            _mockCourseOccasionRepo.Setup(r => r.GetByIdAsync(createDto.CourseOccasionId))
                .ReturnsAsync(courseOccasion);

            _mockRegistrationRepo.Setup(r => r.HasRegistrationAsync(createDto.ParticipantId, createDto.CourseOccasionId))
                .ReturnsAsync(true);

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.CreateRegistrationAsync(createDto));

            exception.Message.ShouldContain("already registered");

            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateRegistrationStatusAsync_ToConfirmed_ShouldConfirmRegistration()
        {
            // Arrange
            var registrationId = Guid.NewGuid();
            var registration = new CourseRegistration(Guid.NewGuid(), Guid.NewGuid());
            var updateDto = new UpdateRegistrationStatusDto { Status = "confirmed" };

            _mockRegistrationRepo.Setup(r => r.GetByIdAsync(registrationId))
                .ReturnsAsync(registration);

            // Act
            await _service.UpdateRegistrationStatusAsync(registrationId, updateDto);

            // Assert
            registration.Status.ShouldBe("Confirmed");
            registration.ConfirmedAt.ShouldNotBeNull();

            _mockRegistrationRepo.Verify(r => r.UpdateAsync(registration), Times.Once);
        }

        [Fact]
        public async Task UpdateRegistrationStatusAsync_ToCancelled_ShouldCancelRegistration()
        {
            // Arrange
            var registrationId = Guid.NewGuid();
            var courseOccasionId = Guid.NewGuid();
            var registration = new CourseRegistration(Guid.NewGuid(), courseOccasionId);
            var courseOccasion = new CourseOccasion(
                Guid.NewGuid(),
                DateTime.Now.AddDays(10),
                DateTime.Now.AddDays(20),
                30);
            var updateDto = new UpdateRegistrationStatusDto { Status = "cancelled" };

            _mockRegistrationRepo.Setup(r => r.GetByIdAsync(registrationId))
                .ReturnsAsync(registration);

            _mockCourseOccasionRepo.Setup(r => r.GetByIdAsync(courseOccasionId))
                .ReturnsAsync(courseOccasion);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            await _service.UpdateRegistrationStatusAsync(registrationId, updateDto);

            // Assert
            registration.Status.ShouldBe("Cancelled");
            registration.CancelledAt.ShouldNotBeNull();

            _mockRegistrationRepo.Verify(r => r.UpdateAsync(registration), Times.Once);
            _mockCourseOccasionRepo.Verify(r => r.UpdateAsync(courseOccasion), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateRegistrationStatusAsync_WithInvalidStatus_ShouldThrowArgumentException()
        {
            // Arrange
            var registrationId = Guid.NewGuid();
            var registration = new CourseRegistration(Guid.NewGuid(), Guid.NewGuid());
            var updateDto = new UpdateRegistrationStatusDto { Status = "invalid" };

            _mockRegistrationRepo.Setup(r => r.GetByIdAsync(registrationId))
                .ReturnsAsync(registration);

            // Act & Assert
            var exception = await Should.ThrowAsync<ArgumentException>(async () =>
                await _service.UpdateRegistrationStatusAsync(registrationId, updateDto));

            exception.Message.ShouldContain("Invalid status");
        }

        [Fact]
        public async Task DeleteRegistrationAsync_WithValidId_ShouldDeleteRegistrationAndDecrementOccasionCount()
        {
            // Arrange
            var registrationId = Guid.NewGuid();
            var courseOccasionId = Guid.NewGuid();
            var registration = new CourseRegistration(Guid.NewGuid(), courseOccasionId);
            var courseOccasion = new CourseOccasion(
                Guid.NewGuid(),
                DateTime.Now.AddDays(10),
                DateTime.Now.AddDays(20),
                30);

            _mockRegistrationRepo.Setup(r => r.GetByIdAsync(registrationId))
                .ReturnsAsync(registration);

            _mockCourseOccasionRepo.Setup(r => r.GetByIdAsync(courseOccasionId))
                .ReturnsAsync(courseOccasion);

            // Act
            await _service.DeleteRegistrationAsync(registrationId);

            // Assert
            _mockRegistrationRepo.Verify(r => r.DeleteAsync(registrationId), Times.Once);
            _mockCourseOccasionRepo.Verify(r => r.UpdateAsync(courseOccasion), Times.Once);
        }

        [Fact]
        public async Task GetRegistrationsByParticipantAsync_ShouldReturnParticipantRegistrations()
        {
            // Arrange
            var participantId = Guid.NewGuid();
            var registrations = new List<CourseRegistration>
            {
                new CourseRegistration(participantId, Guid.NewGuid()),
                new CourseRegistration(participantId, Guid.NewGuid())
            };

            _mockRegistrationRepo.Setup(r => r.GetRegistrationsByParticipantAsync(participantId))
                .ReturnsAsync(registrations);

            SetupMapToDtoForRegistrations(registrations);

            // Act
            var result = await _service.GetRegistrationsByParticipantAsync(participantId);

            // Assert
            result.ShouldNotBeNull();
            result.Count().ShouldBe(2);

            _mockRegistrationRepo.Verify(r => r.GetRegistrationsByParticipantAsync(participantId), Times.Once);
        }

        [Fact]
        public async Task GetRegistrationsByOccasionAsync_ShouldReturnOccasionRegistrations()
        {
            // Arrange
            var occasionId = Guid.NewGuid();
            var registrations = new List<CourseRegistration>
            {
                new CourseRegistration(Guid.NewGuid(), occasionId),
                new CourseRegistration(Guid.NewGuid(), occasionId)
            };

            _mockRegistrationRepo.Setup(r => r.GetRegistrationsByOccasionAsync(occasionId))
                .ReturnsAsync(registrations);

            SetupMapToDtoForRegistrations(registrations);

            // Act
            var result = await _service.GetRegistrationsByOccasionAsync(occasionId);

            // Assert
            result.ShouldNotBeNull();
            result.Count().ShouldBe(2);

            _mockRegistrationRepo.Verify(r => r.GetRegistrationsByOccasionAsync(occasionId), Times.Once);
        }

        [Fact]
        public async Task GetRegistrationDetailsAsync_WithValidId_ShouldReturnRegistrationWithDetails()
        {
            // Arrange
            var registrationId = Guid.NewGuid();
            var registration = new CourseRegistration(Guid.NewGuid(), Guid.NewGuid());

            _mockRegistrationRepo.Setup(r => r.GetRegistrationDetailsAsync(registrationId))
                .ReturnsAsync(registration);

            SetupMapToDtoForRegistration(registration);

            // Act
            var result = await _service.GetRegistrationDetailsAsync(registrationId);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(registration.Id);

            _mockRegistrationRepo.Verify(r => r.GetRegistrationDetailsAsync(registrationId), Times.Once);
        }

        [Fact]
        public async Task ConfirmRegistrationAsync_WithValidId_ShouldConfirmRegistration()
        {
            // Arrange
            var registrationId = Guid.NewGuid();
            var registration = new CourseRegistration(Guid.NewGuid(), Guid.NewGuid());

            _mockRegistrationRepo.Setup(r => r.GetByIdAsync(registrationId))
                .ReturnsAsync(registration);

            // Act
            await _service.ConfirmRegistrationAsync(registrationId);

            // Assert
            registration.Status.ShouldBe("Confirmed");
            registration.ConfirmedAt.ShouldNotBeNull();

            _mockRegistrationRepo.Verify(r => r.UpdateAsync(registration), Times.Once);
        }

        [Fact]
        public async Task CancelRegistrationAsync_WithValidId_ShouldCancelRegistrationAndDecrementOccasionCount()
        {
            // Arrange
            var registrationId = Guid.NewGuid();
            var courseOccasionId = Guid.NewGuid();
            var registration = new CourseRegistration(Guid.NewGuid(), courseOccasionId);
            var courseOccasion = new CourseOccasion(
                Guid.NewGuid(),
                DateTime.Now.AddDays(10),
                DateTime.Now.AddDays(20),
                1);

            _mockRegistrationRepo.Setup(r => r.GetByIdAsync(registrationId))
                .ReturnsAsync(registration);

            _mockCourseOccasionRepo.Setup(r => r.GetByIdAsync(courseOccasionId))
                .ReturnsAsync(courseOccasion);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            await _service.CancelRegistrationAsync(registrationId);

            // Assert
            registration.Status.ShouldBe("Cancelled");
            registration.CancelledAt.ShouldNotBeNull();

            _mockRegistrationRepo.Verify(r => r.UpdateAsync(registration), Times.Once);
            _mockCourseOccasionRepo.Verify(r => r.UpdateAsync(courseOccasion), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CancelRegistrationAsync_WithNonExistentRegistration_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            _mockRegistrationRepo.Setup(r => r.GetByIdAsync(nonExistentId))
                .ReturnsAsync((CourseRegistration)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.CancelRegistrationAsync(nonExistentId));

            exception.Message.ShouldContain(nonExistentId.ToString());

            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        // Helper methods to setup MapToDto
        private void SetupMapToDtoForRegistration(CourseRegistration registration)
        {
            // Setup participant
            var participant = new Participant("Test", "User", "test@test.com", "1234567890", "Test Address");
            _mockParticipantRepo.Setup(r => r.GetByIdAsync(registration.ParticipantId))
                .ReturnsAsync(participant);

            // Setup course occasion
            var occasion = new CourseOccasion(
                Guid.NewGuid(),
                DateTime.Now,
                DateTime.Now.AddDays(10),
                30);
             
            _mockCourseOccasionRepo.Setup(r => r.GetByIdAsync(registration.CourseOccasionId))
                .ReturnsAsync(occasion);

            // Setup CourseOccasionService.MapToDto
            var occasionDto = new CourseOccasionDto();
            _mockCourseOccasionService.Setup(s => s.MapToDto(occasion))
                .ReturnsAsync(occasionDto);
        }

        private void SetupMapToDtoForRegistrations(IEnumerable<CourseRegistration> registrations)
        {
            foreach (var reg in registrations)
            {
                SetupMapToDtoForRegistration(reg);
            }
        }
    }
}