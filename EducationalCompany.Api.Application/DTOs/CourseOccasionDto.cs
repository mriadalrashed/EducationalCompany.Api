
namespace EducationalCompany.Api.Application.DTOs
{
    public class CourseOccasionDto
    {
        public Guid CourseId { get; set; }
        public Guid TeacherId { get; set; }
        public string Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxParticipants { get; set; }
        public int CurrentParticipants { get; set; }
        public bool IsFull => CurrentParticipants >= MaxParticipants;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public CourseDto Course { get;  set; }
        public TeacherDto Teacher { get;  set; }
    }

    public class CreateCourseOccasionDto
    {
        public Guid CourseId { get; set; }
        public Guid TeacherId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxParticipants { get; set; }
    }
    public class UpdateCourseOccasionDto
    {
        public int MaxParticipants { get; set; }
    }

    public class AssignTeacherDto
    {
        public Guid TeacherId { get; set; }
    }

}
