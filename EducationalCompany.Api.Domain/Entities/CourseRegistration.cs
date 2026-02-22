using EducationalCompany.Api.Domain.Common;

namespace EducationalCompany.Api.Domain.Entities
{
    // Represents a participant registration for a course occasion
    public class CourseRegistration : BaseEntity
    {
        public Guid ParticipantId { get; private set; }
        public Guid CourseOccasionId { get; private set; }

        // Date when the registration was created
        public DateTime RegistrationDate { get; private set; }

        // Registration status (Pending, Confirmed, Cancelled)
        public string Status { get; private set; }

        public DateTime? ConfirmedAt { get; private set; }

        public DateTime? CancelledAt { get; private set; }

        public Participant Participant { get; protected set; }

        public CourseOccasion CourseOccasion { get; protected set; }

        // Required by EF Core
        protected CourseRegistration()
        {
        }

        // Creates a new registration with default status "Pending"
        public CourseRegistration(Guid participantId, Guid courseOccasionId)
        {
            ParticipantId = participantId;
            CourseOccasionId = courseOccasionId;
            RegistrationDate = DateTime.UtcNow;
            Status = "Pending";
            CreatedAt = DateTime.UtcNow;
        }

        // Confirms the registration (only if it is still pending)
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


        // Cancels the registration
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
