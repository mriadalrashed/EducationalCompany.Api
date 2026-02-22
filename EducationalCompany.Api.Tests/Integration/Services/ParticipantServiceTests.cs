// Note:
// AI-assisted tools were used to help generate and structure parts of these unit tests.
// All tests have been reviewed, validated, and verified manually to ensure correctness
// and proper coverage of the intended functionality.

using EducationalCompany.Application.DTOs;
using EducationalCompany.Application.Services;
using EducationalCompany.Domain.Entities;
using EducationalCompany.Infrastructure.Data;
using EducationalCompany.Infrastructure.Repositories;
using EducationalCompany.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EducationalCompany.Tests.Integration.Services
{
    public class ParticipantServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ParticipantService _participantService;
        private readonly ServiceProvider _serviceProvider;
        private readonly IMemoryCache _memoryCache;
        private readonly IServiceScope _scope;

        public ParticipantServiceTests()
        {
            var services = new ServiceCollection();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase($"TestDatabase_{Guid.NewGuid()}"));

            services.AddMemoryCache();

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IParticipantRepository, ParticipantRepository>();
            services.AddScoped<ICourseRegistrationRepository, CourseRegistrationRepository>();
            services.AddScoped<ParticipantService>();

            _serviceProvider = services.BuildServiceProvider();

            _scope = _serviceProvider.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _unitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            _participantService = _scope.ServiceProvider.GetRequiredService<ParticipantService>();
            _memoryCache = _scope.ServiceProvider.GetRequiredService<IMemoryCache>();

            _context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            try
            {
                if (_context != null)
                {
                    _context.Database.EnsureDeleted();
                }
            }
            catch (ObjectDisposedException)
            {
                // Ignore if already disposed
            }
            finally
            {
                if (_unitOfWork != null)
                {
                    _unitOfWork.Dispose();
                }

                if (_context != null)
                {
                    _context.Dispose();
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
                    _serviceProvider.Dispose();
                }
            }
        }

        [Fact]
        public async Task UpdateParticipantAsync_ValidUpdate_ShouldUpdateSuccessfully()
        {
            var participant = new Participant(
                "Eve",
                "Miller",
                "eve@example.com",
                "1231231234",
                "Original Address");

            await _unitOfWork.Participants.AddAsync(participant);
            await _unitOfWork.CompleteAsync();

            var updateDto = new UpdateParticipantDto
            {
                FirstName = "Eve Updated",
                LastName = "Miller Updated",
                Email = "eve.updated@example.com",
                Phone = "9998887777",
                Address = "Updated Address"
            };

            await _participantService.UpdateParticipantAsync(participant.Id, updateDto);
            await _unitOfWork.CompleteAsync();

            var updatedParticipant = await _unitOfWork.Participants.GetByIdAsync(participant.Id);
            updatedParticipant.ShouldNotBeNull();
            updatedParticipant.FirstName.ShouldBe(updateDto.FirstName);
            updatedParticipant.LastName.ShouldBe(updateDto.LastName);
            updatedParticipant.Email.ShouldBe(updateDto.Email);
            updatedParticipant.Phone.ShouldBe(updateDto.Phone);
            updatedParticipant.Address.ShouldBe(updateDto.Address);
            updatedParticipant.UpdatedDate.ShouldNotBe(default);
        }

        [Fact]
        public async Task CreateParticipantAsync_ValidParticipant_ShouldCreateSuccessfully()
        {
            var createDto = new CreateParticipantDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Phone = "1234567890",
                Address = "123 Main St"
            };

            var result = await _participantService.CreateParticipantAsync(createDto);
            await _unitOfWork.CompleteAsync();

            result.ShouldNotBeNull();
            result.Id.ShouldNotBe(Guid.Empty);
            result.FirstName.ShouldBe(createDto.FirstName);
            result.LastName.ShouldBe(createDto.LastName);
            result.Email.ShouldBe(createDto.Email);
            result.Phone.ShouldBe(createDto.Phone);
            result.Address.ShouldBe(createDto.Address);
            result.CreatedDate.ShouldNotBe(default);
            result.UpdatedDate.ShouldBe(default);

            var savedParticipant = await _unitOfWork.Participants.GetByIdAsync(result.Id);
            savedParticipant.ShouldNotBeNull();
            savedParticipant.Email.ShouldBe(createDto.Email);
        }

        [Fact]
        public async Task CreateParticipantAsync_DuplicateEmail_ShouldThrowInvalidOperationException()
        {
            var createDto1 = new CreateParticipantDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "duplicate@example.com",
                Phone = "1234567890",
                Address = "123 Main St"
            };

            var createDto2 = new CreateParticipantDto
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "duplicate@example.com",
                Phone = "0987654321",
                Address = "456 Oak Ave"
            };

            await _participantService.CreateParticipantAsync(createDto1);
            await _unitOfWork.CompleteAsync();

            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _participantService.CreateParticipantAsync(createDto2));

            exception.Message.ShouldContain("already exists");
        }

        [Fact]
        public async Task GetParticipantByIdAsync_ExistingParticipant_ShouldReturnParticipant()
        {
            var participant = new Participant(
                "Alice",
                "Johnson",
                "alice@example.com",
                "5551234567",
                "789 Pine St");

            await _unitOfWork.Participants.AddAsync(participant);
            await _unitOfWork.CompleteAsync();

            var result = await _participantService.GetParticipantByIdAsync(participant.Id);

            result.ShouldNotBeNull();
            result.Id.ShouldBe(participant.Id);
            result.FirstName.ShouldBe(participant.FirstName);
            result.LastName.ShouldBe(participant.LastName);
            result.Email.ShouldBe(participant.Email);
        }

        [Fact]
        public async Task GetParticipantByIdAsync_NonExistentParticipant_ShouldThrowKeyNotFoundException()
        {
            var nonExistentId = Guid.NewGuid();

            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _participantService.GetParticipantByIdAsync(nonExistentId));

            exception.Message.ShouldContain(nonExistentId.ToString());
        }

        [Fact]
        public async Task GetAllParticipantsAsync_MultipleParticipants_ShouldReturnAll()
        {
            var participants = new[]
            {
                new Participant("Bob", "Brown", "bob@example.com", "1112223333", "111 Elm St"),
                new Participant("Carol", "Davis", "carol@example.com", "4445556666", "222 Maple Ave"),
                new Participant("David", "Wilson", "david@example.com", "7778889999", "333 Cedar Ln")
            };

            foreach (var p in participants)
            {
                await _unitOfWork.Participants.AddAsync(p);
            }
            await _unitOfWork.CompleteAsync();

            var result = (await _participantService.GetAllParticipantsAsync()).ToList();

            result.Count.ShouldBe(3);
            result.Select(p => p.Email).ShouldContain("bob@example.com");
            result.Select(p => p.Email).ShouldContain("carol@example.com");
            result.Select(p => p.Email).ShouldContain("david@example.com");
        }

        [Fact]
        public async Task UpdateParticipantAsync_UpdateToExistingEmail_ShouldThrowInvalidOperationException()
        {
            var participant1 = new Participant(
                "Frank",
                "Thomas",
                "frank@example.com",
                "1111111111",
                "Address 1");

            var participant2 = new Participant(
                "Grace",
                "White",
                "grace@example.com",
                "2222222222",
                "Address 2");

            await _unitOfWork.Participants.AddAsync(participant1);
            await _unitOfWork.Participants.AddAsync(participant2);
            await _unitOfWork.CompleteAsync();

            var updateDto = new UpdateParticipantDto
            {
                FirstName = "Frank",
                LastName = "Thomas",
                Email = "grace@example.com",
                Phone = "1111111111",
                Address = "Address 1"
            };

            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _participantService.UpdateParticipantAsync(participant1.Id, updateDto));

            exception.Message.ShouldContain("already exists");
        }

        [Fact]
        public async Task DeleteParticipantAsync_ParticipantWithNoRegistrations_ShouldDeleteSuccessfully()
        {
            var participant = new Participant(
                "Henry",
                "Clark",
                "henry@example.com",
                "3333333333",
                "Delete Me");

            await _unitOfWork.Participants.AddAsync(participant);
            await _unitOfWork.CompleteAsync();

            await _participantService.DeleteParticipantAsync(participant.Id);
            await _unitOfWork.CompleteAsync();

            var deletedParticipant = await _unitOfWork.Participants.GetByIdAsync(participant.Id);
            deletedParticipant.ShouldBeNull();
        }

        [Fact]
        public async Task DeleteParticipantAsync_NonExistentParticipant_ShouldThrowKeyNotFoundException()
        {
            var nonExistentId = Guid.NewGuid();

            var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
                await _participantService.DeleteParticipantAsync(nonExistentId));

            exception.Message.ShouldContain(nonExistentId.ToString());
        }

        [Fact]
        public async Task SearchParticipantsAsync_WithSearchTerm_ShouldReturnMatchingParticipants()
        {
            var participants = new[]
            {
                new Participant("Ivy", "Adams", "ivy.adams@example.com", "4444444444", "123 Test St"),
                new Participant("Jack", "Baker", "jack.baker@example.com", "5555555555", "456 Test Ave"),
                new Participant("Kevin", "Adams", "kevin.adams@example.com", "6666666666", "789 Test Blvd")
            };

            foreach (var p in participants)
            {
                await _unitOfWork.Participants.AddAsync(p);
            }
            await _unitOfWork.CompleteAsync();

            var resultsByLastName = (await _participantService.SearchParticipantsAsync("Adams")).ToList();

            resultsByLastName.Count.ShouldBe(2);
            resultsByLastName.Select(p => p.Email).ShouldContain("ivy.adams@example.com");
            resultsByLastName.Select(p => p.Email).ShouldContain("kevin.adams@example.com");
        }
    }
}