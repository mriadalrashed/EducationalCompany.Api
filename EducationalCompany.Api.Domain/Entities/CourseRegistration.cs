using EducationalCompany.Api.Domain.Common;

namespace EducationalCompany.Api.Domain.Entities
{
    public class CourseRegistration : BaseEntity
    {
        public Guid ParticipantId { get; private set; }
        public Guid CourseOccasionId { get; private set; }
        public  DateTime RegistrationDate { get; private set; }

        public string Status { get; private set; }

        public DateTime? ConfirmedAt { get; private set; }

        public DateTime? CancelledAt { get; private set; }

        public Participant Participant { get; protected set; }

        public CourseOccasion CourseOccasion { get; protected set; }

        protected CourseRegistration()
        {
        }

        public CourseRegistration(Guid participantId, Guid courseOccasionId)
        {
            ParticipantId = participantId;
            CourseOccasionId = courseOccasionId;
            RegistrationDate = DateTime.UtcNow;
            Status = "Pending";
            CreatedAt = DateTime.UtcNow;
        }
        public void Confirm()
        {
            if (Status != "Pending")
            {
                throw new InvalidOperationException("Only pending registrations can be confirmed.");
            }
            Status = "Confirmed";
            ConfirmedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            if (Status == "Cancelled")
            {
                throw new InvalidOperationException("Registration is already cancelled.");
            }
            Status = "Cancelled";
            CancelledAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
