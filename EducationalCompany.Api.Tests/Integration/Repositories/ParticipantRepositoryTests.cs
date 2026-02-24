// Note:
// AI-assisted tools were used to help generate and structure parts of these unit tests.
// All tests have been reviewed, validated, and verified manually to ensure correctness
// and proper coverage of the intended functionality.

using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Infrastructure.Data;
using EducationalCompany.Api.Infrastructure.Repositories;
using EducationalCompany.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EducationalCompany.Tests.Integration.Repositories
{
    public class ParticipantRepositoryIntegrationTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ParticipantRepository _repository;
        private readonly IMemoryCache _memoryCache;

        public ParticipantRepositoryIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"TestDatabase_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _repository = new ParticipantRepository(_context);

            _context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            _memoryCache.Dispose();
        }

        [Fact]
        public async Task AddAsync_ValidParticipant_ShouldAddToDatabase()
        {
            var participant = new Participant(
                "John",
                "Doe",
                "john.doe@test.com",
                "1234567890",
                "123 Main St");

            await _repository.AddAsync(participant);
            await _context.SaveChangesAsync();

            var savedParticipant = await _context.Participants.FindAsync(participant.Id);
            savedParticipant.ShouldNotBeNull();
            savedParticipant.Email.ShouldBe("john.doe@test.com");
            savedParticipant.CreatedAt.ShouldNotBe(default);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingParticipant_ShouldReturnParticipant()
        {
            var participant = new Participant(
                "Jane",
                "Smith",
                "jane.smith@test.com",
                "0987654321",
                "456 Oak Ave");

            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(participant.Id);

            result.ShouldNotBeNull();
            result.Id.ShouldBe(participant.Id);
            result.Email.ShouldBe("jane.smith@test.com");
        }

        [Fact]
        public async Task GetAllAsync_WithMultipleParticipants_ShouldReturnAll()
        {
            var participants = new[]
            {
                new Participant("Bob", "Brown", "bob.brown@test.com", "1111111111", "111 Elm St"),
                new Participant("Alice", "Johnson", "alice.johnson@test.com", "2222222222", "222 Maple Ave"),
                new Participant("Charlie", "Wilson", "charlie.wilson@test.com", "3333333333", "333 Oak Dr")
            };

            await _context.Participants.AddRangeAsync(participants);
            await _context.SaveChangesAsync();

            var results = (await _repository.GetAllAsync()).ToList();

            results.Count.ShouldBe(3);
            results.Select(p => p.Email).ShouldContain("bob.brown@test.com");
            results.Select(p => p.Email).ShouldContain("alice.johnson@test.com");
            results.Select(p => p.Email).ShouldContain("charlie.wilson@test.com");
        }

        [Fact]
        public async Task UpdateAsync_ExistingParticipant_ShouldUpdateSuccessfully()
        {
            var participant = new Participant(
                "Original",
                "Name",
                "original@test.com",
                "1112223333",
                "Original Address");

            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();

            _context.Entry(participant).State = EntityState.Detached;

            var updatedParticipant = new Participant(
                "Updated",
                "Name",
                "updated@test.com",
                "4445556666",
                "Updated Address");

            var idProperty = typeof(Participant).GetProperty("Id");
            idProperty?.SetValue(updatedParticipant, participant.Id);

            await _repository.UpdateAsync(updatedParticipant);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(participant.Id);
            result.ShouldNotBeNull();
            result.FirstName.ShouldBe("Updated");
            result.Email.ShouldBe("updated@test.com");
        }

        [Fact]
        public async Task DeleteAsync_ExistingParticipant_ShouldRemoveFromDatabase()
        {
            var participant = new Participant(
                "Delete",
                "Me",
                "delete.me@test.com",
                "9998887777",
                "Delete Address");

            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();

            await _repository.DeleteAsync(participant.Id);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(participant.Id);
            result.ShouldBeNull();
        }

        [Fact]
        public async Task SearchParticipantsAsync_WithEmail_ShouldReturnMatchingParticipants()
        {
            var participants = new[]
            {
                new Participant("Bob", "Johnson", "bob.johnson@test.com", "1111111111", "111 Pine St"),
                new Participant("Alice", "Johnson", "alice.johnson@test.com", "2222222222", "222 Pine St"),
                new Participant("Charlie", "Brown", "charlie.brown@test.com", "3333333333", "333 Elm St")
            };

            await _context.Participants.AddRangeAsync(participants);
            await _context.SaveChangesAsync();

            var results = (await _repository.SearchParticipantsAsync("bob.johnson@test.com")).ToList();

            results.Count.ShouldBe(1);
            results[0].Email.ShouldBe("bob.johnson@test.com");
        }

        [Fact]
        public async Task SearchParticipantsAsync_WithLastName_ShouldReturnMatchingParticipants()
        {
            var participants = new[]
            {
                new Participant("David", "Williams", "david.williams@test.com", "4444444444", "444 Maple Dr"),
                new Participant("Daniel", "Williams", "daniel.williams@test.com", "5555555555", "555 Maple Dr"),
                new Participant("Sarah", "Wilson", "sarah.wilson@test.com", "6666666666", "666 Cedar Ct")
            };

            await _context.Participants.AddRangeAsync(participants);
            await _context.SaveChangesAsync();

            var results = (await _repository.SearchParticipantsAsync("Williams")).ToList();

            results.Count.ShouldBe(2);
            results.All(p => p.LastName == "Williams").ShouldBeTrue();
        }
    }
}