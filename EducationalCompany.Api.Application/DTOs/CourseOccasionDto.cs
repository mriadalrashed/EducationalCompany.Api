
namespace EducationalCompany.Api.Application.DTOs
{
    // DTO used to return course occasion data
    public class CourseOccasionDto
    {
        public Guid Id { get; set; }
        public Guid CourseId { get; set; }
        public Guid? TeacherId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxParticipants { get; set; }
        public int CurrentParticipants { get; set; }
        public bool IsFull { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public CourseDto Course { get; set; }
        public TeacherDto Teacher { get; set; }
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

  

    // DTO used to assign a teacher to a course occasion
    public class AssignTeacherDto
    {
        public Guid TeacherId { get; set; } // Teacher ID
    }

}
