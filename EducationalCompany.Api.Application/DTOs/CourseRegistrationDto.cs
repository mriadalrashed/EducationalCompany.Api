namespace EducationalCompany.Api.Application.DTOs
{
    // DTO used to return course registration data
    public class CourseRegistrationDto
    {
        public Guid Id { get;  set; }
        public Guid ParticipantId { get;  set; }
        public Guid CourseOccasionId { get;  set; }
        public DateTime RegistrationDate { get;  set; }
        public string Status { get;  set; }
        public DateTime? ConfirmedAt { get;  set; }
        public DateTime? CancelledAt { get;  set; }
        public DateTime CreatedAt { get;  set; }
        public DateTime? UpdatedAt { get;  set; }
        public ParticipantDto Participant { get;  set; }
        public CourseOccasionDto CourseOccasion { get;  set; }
    }

    // DTO used when creating a course registration
    public class CreateCourseRegistrationDto
    {
        public Guid ParticipantId { get; set; }
        public Guid CourseOccasionId { get; set; }
    }
    // DTO used when updating registration status
    public class UpdateRegistrationStatusDto
    {
        public string Status { get; set; } // New status
    }  
}
