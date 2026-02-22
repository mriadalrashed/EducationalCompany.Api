using EducationalCompany.Api.Domain.Entities;

namespace EducationalCompany.Api.Tests.Fixtures
{
    // Provides test data objects for unit tests
    public static class TestDataFactory
    {
        // Create a valid course entity for testing
        public static Course CreateCourse()
        {
            return new Course("test course", "test description", 40, 1000);
        }

        // Create a valid participant (optionally with custom email)
        public static Participant CreateValidParticipant(string email = null)
        {
            return new Participant(
                "test firstName",
                "test lastName",
                email ?? $"test.{Guid.NewGuid()}@example.com",
                "070000000",
                "123 centrum");
        }

        // Create multiple participants for bulk testing
        public static List<Participant> CreateMultipleParticipants(int count)
        {
            var participants = new List<Participant>();

            for (int i = 0; i < count; i++)
            {
                participants.Add(
                    new Participant(
                        $"First {i}",
                        $"Last {i}",
                        $"User{i}@example.com",
                        $"07000000{i:D4}",
                        $"{i} Test ave"));
            }

            return participants;
        }

        // Create participant intended for registration-related tests
        public static Participant CreateParticpantWithRegistration()
        {
            return CreateValidParticipant("Registred.User@gmail.com");
        }
    }
}