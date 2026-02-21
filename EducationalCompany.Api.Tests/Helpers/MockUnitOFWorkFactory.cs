using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Infrastructure;
using Moq;

namespace EducationalCompany.Api.Tests.Helpers
{
    public static class MockUnitOFWorkFactory
    {
        public static (Mock<IUnitOFWork>,Mock<ICourseRepository>) Create()
        {
            var courseRepoMock = new Mock<ICourseRepository>();
            var uowMock = new Mock<IUnitOFWork>();
            var mockParticipantRepo = new Mock<IParticipantRepository>();
            var mockRegistrationRepo = new Mock<ICourseRegistrationRepository>();

            uowMock.Setup(u => u.Participants).Returns(mockParticipantRepo.Object);
            uowMock.Setup(u => u.CourseRegistrations).Returns(mockRegistrationRepo.Object);
            uowMock.Setup(u => u.Courses).Returns(courseRepoMock.Object);
            return (uowMock, courseRepoMock);
           
        }
    }
}
