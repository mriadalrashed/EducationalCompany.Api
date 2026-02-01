namespace EducationalCompany.Api.Application.DTOs
{
    public class CourseRegistrationDto
    {
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

    public class CreateCourseRegistrationDto
    {
        public Guid ParticipantId { get; set; }
        public Guid CourseOccasionId { get; set; }
    }
    public class UpdateCourseRegistrationDto
    {
        public string Status { get; set; }
    } 
}
