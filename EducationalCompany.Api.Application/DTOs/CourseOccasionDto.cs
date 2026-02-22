
namespace EducationalCompany.Api.Application.DTOs
{
    // DTO used to return course occasion data
    public class CourseOccasionDto
    {
        public Guid CourseId { get; set; }
        public Guid TeacherId { get; set; }
        public string Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxParticipants { get; set; }
        public int CurrentParticipants { get; set; }

        // Indicates if the occasion is full
        public bool IsFull => CurrentParticipants >= MaxParticipants;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public CourseDto Course { get;  set; }
        public TeacherDto Teacher { get;  set; }
    }

    // DTO used when creating a course occasion
    public class CreateCourseOccasionDto
    {
        public Guid CourseId { get; set; }
        public Guid TeacherId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxParticipants { get; set; }
    }

    // DTO used when updating course occasion
    public class UpdateCourseOccasionDto
    {
        public int MaxParticipants { get; set; }
    }

    // DTO used to assign a teacher to a course occasion
    public class AssignTeacherDto
    {
        public Guid TeacherId { get; set; } // Teacher ID
    }

}
