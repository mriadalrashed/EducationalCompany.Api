namespace EducationalCompany.Api.Application.DTOs
{
    // DTO used to return course data to the client
    public class CourseDto 
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int DurationHours { get; set; }
        public decimal Price { get; set; }
        //public DateTime CreatedAt { get; set; }
        //public DateTime UpdatedAt { get; set; }
    }

    // DTO used when creating a new course
    public class CreateCourseDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int DurationHours { get; set; }
        public decimal Price { get; set; }
    }


    // DTO used when updating an existing course
    public class UpdateCourseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int DurationHours { get; set; }
        public decimal Price { get; set; }
    }
}

