namespace EducationalCompany.Api.Application.DTOs
{
    public class CourseDto 
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DurationHours { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
    public class CreateCourseDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DurationHours { get; set; }
        public decimal Price { get; set; }
    }

    public class UpdateCourseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DurationHours { get; set; }
        public decimal Price { get; set; }
    }
}

