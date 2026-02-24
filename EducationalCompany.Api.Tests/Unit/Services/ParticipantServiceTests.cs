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
    public class ParticipantServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IParticipantRepository> _mockParticipantRepo;
        private readonly Mock<ICourseRegistrationRepository> _mockCourseRegistrationRepo;
        private readonly ParticipantService _service;

        public ParticipantServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockParticipantRepo = new Mock<IParticipantRepository>();
            _mockCourseRegistrationRepo = new Mock<ICourseRegistrationRepository>();

            _mockUnitOfWork.Setup(u => u.Participants).Returns(_mockParticipantRepo.Object);
            _mockUnitOfWork.Setup(u => u.CourseRegistrations).Returns(_mockCourseRegistrationRepo.Object);

            _service = new ParticipantService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetAllParticipantsAsync_ShouldReturnAllParticipants()
        {
            // Arrange
            var participants = new List<Participant>
            {
                new Participant("John", "Doe", "john@test.com", "1234567890", "123 Main St"),
                new Participant("Jane", "Smith", "jane@test.com", "0987654321", "456 Oak Ave")
            };

            _mockParticipantRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(participants);

            // Act
            var result = await _service.GetAllParticipantsAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Count().ShouldBe(2);
            result.First().Email.ShouldBe("john@test.com");

            _mockParticipantRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllParticipantsAsync_WhenNoParticipants_ShouldReturnEmptyList()
        {
            // Arrange
            _mockParticipantRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Participant>());

            // Act
            var result = await _service.GetAllParticipantsAsync();

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetParticipantByIdAsync_WithValidId_ShouldReturnParticipant()
        {
            // Arrange
            var participantId = Guid.NewGuid();
            var participant = new Participant("John", "Doe", "john@test.com", "1234567890", "123 Main St");

            _mockParticipantRepo.Setup(r => r.GetByIdAsync(participantId))
                .ReturnsAsync(participant);

            // Act
            var result = await _service.GetParticipantByIdAsync(participantId);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(participant.Id);
            result.Email.ShouldBe("john@test.com");

            _mockParticipantRepo.Verify(r => r.GetByIdAsync(participantId), Times.Once);
        }

        [Fact]
        public async Task GetParticipantByIdAsync_WithInvalidId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _mockParticipantRepo.Setup(r => r.GetByIdAsync(invalidId))
                .ReturnsAsync((Participant)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.GetParticipantByIdAsync(invalidId));

            exception.Message.ShouldContain(invalidId.ToString());
        }

        [Fact]
        public async Task CreateParticipantAsync_WithValidDto_ShouldCreateAndReturnParticipant()
        {
            // Arrange
            var createDto = new CreateParticipantDto
            {
                FirstName = "New",
                LastName = "User",
                Email = "new.user@test.com",
                Phone = "5551234567",
                Address = "789 New St"
            };

            _mockParticipantRepo.Setup(r => r.SearchParticipantsAsync(createDto.Email))
                .ReturnsAsync(new List<Participant>());

            Participant capturedParticipant = null;
            _mockParticipantRepo.Setup(r => r.AddAsync(It.IsAny<Participant>()))
                .Callback<Participant>(p => capturedParticipant = p)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateParticipantAsync(createDto);

            // Assert
            result.ShouldNotBeNull();
            result.FirstName.ShouldBe(createDto.FirstName);
            result.LastName.ShouldBe(createDto.LastName);
            result.Email.ShouldBe(createDto.Email);
            result.Phone.ShouldBe(createDto.Phone);
            result.Address.ShouldBe(createDto.Address);

            _mockParticipantRepo.Verify(r => r.SearchParticipantsAsync(createDto.Email), Times.Once);
            _mockParticipantRepo.Verify(r => r.AddAsync(It.IsAny<Participant>()), Times.Once);

            capturedParticipant.ShouldNotBeNull();
            capturedParticipant.Email.ShouldBe(createDto.Email);
        }

        [Fact]
        public async Task CreateParticipantAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var createDto = new CreateParticipantDto
            {
                FirstName = "Duplicate",
                LastName = "User",
                Email = "duplicate@test.com",
                Phone = "5551234567",
                Address = "789 New St"
            };

            var existingParticipants = new List<Participant>
            {
                new Participant("Existing", "User", "duplicate@test.com", "1112223333", "Old Address")
            };

            _mockParticipantRepo.Setup(r => r.SearchParticipantsAsync(createDto.Email))
                .ReturnsAsync(existingParticipants);

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.CreateParticipantAsync(createDto));

            exception.Message.ShouldContain("already exists");

            _mockParticipantRepo.Verify(r => r.AddAsync(It.IsAny<Participant>()), Times.Never);
        }

        [Fact]
        public async Task UpdateParticipantAsync_WithValidData_ShouldUpdateParticipant()
        {
            // Arrange
            var participantId = Guid.NewGuid();
            var existingParticipant = new Participant(
                "Old", "Name", "old@test.com", "1112223333", "Old Address");

            var updateDto = new UpdateParticipantDto
            {
                FirstName = "Updated",
                LastName = "Name",
                Email = "updated@test.com",
                Phone = "4445556666",
                Address = "Updated Address"
            };

            _mockParticipantRepo.Setup(r => r.GetByIdAsync(participantId))
                .ReturnsAsync(existingParticipant);

            _mockParticipantRepo.Setup(r => r.SearchParticipantsAsync(updateDto.Email))
                .ReturnsAsync(new List<Participant>()); // No existing with this email

            // Act
            await _service.UpdateParticipantAsync(participantId, updateDto);

            // Assert
            _mockParticipantRepo.Verify(r => r.GetByIdAsync(participantId), Times.Once);
            _mockParticipantRepo.Verify(r => r.SearchParticipantsAsync(updateDto.Email), Times.Once);
            _mockParticipantRepo.Verify(r => r.UpdateAsync(existingParticipant), Times.Once);

            // Verify the participant was updated
            existingParticipant.FirstName.ShouldBe(updateDto.FirstName);
            existingParticipant.LastName.ShouldBe(updateDto.LastName);
            existingParticipant.Email.ShouldBe(updateDto.Email);
            existingParticipant.Phone.ShouldBe(updateDto.Phone);
            existingParticipant.Address.ShouldBe(updateDto.Address);
        }

        [Fact]
        public async Task UpdateParticipantAsync_WithSameEmail_ShouldNotCheckForDuplicates()
        {
            // Arrange
            var participantId = Guid.NewGuid();
            var existingParticipant = new Participant(
                "Old", "Name", "same@test.com", "1112223333", "Old Address");

            var updateDto = new UpdateParticipantDto
            {
                FirstName = "Updated",
                LastName = "Name",
                Email = "same@test.com", // Same email
                Phone = "4445556666",
                Address = "Updated Address"
            };

            _mockParticipantRepo.Setup(r => r.GetByIdAsync(participantId))
                .ReturnsAsync(existingParticipant);

            // Act
            await _service.UpdateParticipantAsync(participantId, updateDto);

            // Assert
            _mockParticipantRepo.Verify(r => r.GetByIdAsync(participantId), Times.Once);
            _mockParticipantRepo.Verify(r => r.SearchParticipantsAsync(It.IsAny<string>()), Times.Never);
            _mockParticipantRepo.Verify(r => r.UpdateAsync(existingParticipant), Times.Once);
        }

        [Fact]
        public async Task UpdateParticipantAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var participantId = Guid.NewGuid();
            var existingParticipant = new Participant(
                "Old", "Name", "old@test.com", "1112223333", "Old Address");

            var anotherParticipant = new Participant(
                "Another", "User", "duplicate@test.com", "9998887777", "Another Address");
            anotherParticipant.GetType().GetProperty("Id")?.SetValue(anotherParticipant, Guid.NewGuid());

            var updateDto = new UpdateParticipantDto
            {
                FirstName = "Updated",
                LastName = "Name",
                Email = "duplicate@test.com", // Email that belongs to another participant
                Phone = "4445556666",
                Address = "Updated Address"
            };

            _mockParticipantRepo.Setup(r => r.GetByIdAsync(participantId))
                .ReturnsAsync(existingParticipant);

            _mockParticipantRepo.Setup(r => r.SearchParticipantsAsync(updateDto.Email))
                .ReturnsAsync(new List<Participant> { anotherParticipant });

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.UpdateParticipantAsync(participantId, updateDto));

            exception.Message.ShouldContain("already exists");

            _mockParticipantRepo.Verify(r => r.UpdateAsync(It.IsAny<Participant>()), Times.Never);
        }

        [Fact]
        public async Task UpdateParticipantAsync_WithNonExistentParticipant_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var updateDto = new UpdateParticipantDto
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@test.com",
                Phone = "1234567890",
                Address = "Test Address"
            };

            _mockParticipantRepo.Setup(r => r.GetByIdAsync(nonExistentId))
                .ReturnsAsync((Participant)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.UpdateParticipantAsync(nonExistentId, updateDto));

            exception.Message.ShouldContain(nonExistentId.ToString());
        }

        [Fact]
        public async Task DeleteParticipantAsync_WithValidIdAndNoRegistrations_ShouldDeleteParticipant()
        {
            // Arrange
            var participantId = Guid.NewGuid();
            var participant = new Participant("Delete", "Me", "delete@test.com", "1231231234", "Delete Address");

            _mockParticipantRepo.Setup(r => r.GetByIdAsync(participantId))
                .ReturnsAsync(participant);

            _mockCourseRegistrationRepo.Setup(r => r.GetRegistrationsByParticipantAsync(participantId))
                .ReturnsAsync(new List<CourseRegistration>());

            // Act
            await _service.DeleteParticipantAsync(participantId);

            // Assert
            _mockParticipantRepo.Verify(r => r.GetByIdAsync(participantId), Times.Once);
            _mockCourseRegistrationRepo.Verify(r => r.GetRegistrationsByParticipantAsync(participantId), Times.Once);
            _mockParticipantRepo.Verify(r => r.DeleteAsync(participantId), Times.Once);
        }

        [Fact]
        public async Task DeleteParticipantAsync_WithExistingRegistrations_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var participantId = Guid.NewGuid();
            var participant = new Participant("Delete", "Me", "delete@test.com", "1231231234", "Delete Address");

            var registrations = new List<CourseRegistration>
            {
                new CourseRegistration(participantId, Guid.NewGuid())
            };

            _mockParticipantRepo.Setup(r => r.GetByIdAsync(participantId))
                .ReturnsAsync(participant);

            _mockCourseRegistrationRepo.Setup(r => r.GetRegistrationsByParticipantAsync(participantId))
                .ReturnsAsync(registrations);

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _service.DeleteParticipantAsync(participantId));

            exception.Message.ShouldContain("Cannot delete participant who has course registrations");

            _mockParticipantRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteParticipantAsync_WithNonExistentParticipant_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            _mockParticipantRepo.Setup(r => r.GetByIdAsync(nonExistentId))
                .ReturnsAsync((Participant)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.DeleteParticipantAsync(nonExistentId));

            exception.Message.ShouldContain(nonExistentId.ToString());

            _mockCourseRegistrationRepo.Verify(r => r.GetRegistrationsByParticipantAsync(It.IsAny<Guid>()), Times.Never);
            _mockParticipantRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task SearchParticipantsAsync_WithValidTerm_ShouldReturnMatchingParticipants()
        {
            // Arrange
            var searchTerm = "john";
            var participants = new List<Participant>
            {
                new Participant("John", "Doe", "john@test.com", "1111111111", "Address 1"),
                new Participant("Johnny", "Smith", "johnny@test.com", "2222222222", "Address 2")
            };

            _mockParticipantRepo.Setup(r => r.SearchParticipantsAsync(searchTerm))
                .ReturnsAsync(participants);

            // Act
            var result = await _service.SearchParticipantsAsync(searchTerm);

            // Assert
            result.ShouldNotBeNull();
            result.Count().ShouldBe(2);
            result.First().FirstName.ShouldBe("John");

            _mockParticipantRepo.Verify(r => r.SearchParticipantsAsync(searchTerm), Times.Once);
        }

        [Fact]
        public async Task SearchParticipantsAsync_WithNoMatches_ShouldReturnEmptyList()
        {
            // Arrange
            var searchTerm = "nonexistent";

            _mockParticipantRepo.Setup(r => r.SearchParticipantsAsync(searchTerm))
                .ReturnsAsync(new List<Participant>());

            // Act
            var result = await _service.SearchParticipantsAsync(searchTerm);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetParticipantWithRegistrationsAsync_WithValidId_ShouldReturnParticipantWithRegistrations()
        {
            // Arrange
            var participantId = Guid.NewGuid();
            var participant = new Participant("John", "Doe", "john@test.com", "1234567890", "123 Main St");

            _mockParticipantRepo.Setup(r => r.GetParticipantWithRegistrationsAsync(participantId))
                .ReturnsAsync(participant);

            // Act
            var result = await _service.GetParticipantWithRegistrationsAsync(participantId);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(participant.Id);
            result.Email.ShouldBe("john@test.com");

            _mockParticipantRepo.Verify(r => r.GetParticipantWithRegistrationsAsync(participantId), Times.Once);
        }

        [Fact]
        public async Task GetParticipantWithRegistrationsAsync_WithInvalidId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            _mockParticipantRepo.Setup(r => r.GetParticipantWithRegistrationsAsync(invalidId))
                .ReturnsAsync((Participant)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _service.GetParticipantWithRegistrationsAsync(invalidId));

            exception.Message.ShouldContain(invalidId.ToString());
        }

        [Fact]
        public void MapToDto_ShouldMapAllPropertiesCorrectly()
        {
            // This tests the private MapToDto method through public methods
            // Arrange
            var participant = new Participant(
                "Test",
                "Mapping",
                "test.mapping@test.com",
                "5555555555",
                "456 Test Ave");

            _mockParticipantRepo.Setup(r => r.GetByIdAsync(participant.Id))
                .ReturnsAsync(participant);

            // Act
            var result = _service.GetParticipantByIdAsync(participant.Id).Result;

            // Assert
            result.Id.ShouldBe(participant.Id);
            result.FirstName.ShouldBe(participant.FirstName);
            result.LastName.ShouldBe(participant.LastName);
            result.Email.ShouldBe(participant.Email);
            result.Phone.ShouldBe(participant.Phone);
            result.Address.ShouldBe(participant.Address);
            result.CreatedAt.ShouldBe(participant.CreatedAt);
            result.UpdatedAt.ShouldBe(participant.UpdatedAt);
        }
    }
}