using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Infrastructure;
using Moq;

namespace EducationalCompany.Api.Tests.Helpers
{
    // Helper class to create mocked UnitOfWork for testing
    public static class MockUnitOFWorkFactory
    {
        // Creates mocked UnitOfWork and CourseRepository
        public static (Mock<IUnitOFWork>, Mock<ICourseRepository>) Create()
        {
            // Mock for Course repository
            var courseRepoMock = new Mock<ICourseRepository>();

            // Mock for UnitOfWork
            var uowMock = new Mock<IUnitOFWork>();

            // Mock for Participant repository
            var mockParticipantRepo = new Mock<IParticipantRepository>();

            // Mock for CourseRegistration repository
            var mockRegistrationRepo = new Mock<ICourseRegistrationRepository>();

            // Setup repositories inside UnitOfWork
            uowMock.Setup(u => u.Participants).Returns(mockParticipantRepo.Object);
            uowMock.Setup(u => u.CourseRegistrations).Returns(mockRegistrationRepo.Object);
            uowMock.Setup(u => u.Courses).Returns(courseRepoMock.Object);

            return (uowMock, courseRepoMock);
        }
    }
}