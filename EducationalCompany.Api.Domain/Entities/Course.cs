using EducationalCompany.Api.Domain.Common;
using System.Collections.ObjectModel;


namespace EducationalCompany.Api.Domain.Entities
{
    // Represents a course in the system
    public class Course : BaseEntity
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public int DurationHours { get; private set; }
        public decimal Price { get; private set; }

        // List of course occasions (sessions)
        public ICollection<CourseOccasion> Occasions { get; private set; } = new List<CourseOccasion>();

        // Required by EF Core
        protected Course()
        {
        }

        // Creates a new course with validation
        public Course( string name, string description, int durationHours, decimal price)
        {   
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description)); ;
            DurationHours = durationHours > 0 ? durationHours : throw new ArgumentException("duration must be positive");
            Price = price >= 0 ? price : throw new ArgumentException("price can't be negtive");
            CreatedAt = DateTime.UtcNow;
        }

        // Updates course details with validation
        public void Update(string name, string description, int durationHours, decimal price)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description)); ;
            DurationHours = durationHours > 0 ? durationHours : throw new ArgumentException("duration must be positive");
            Price = price >= 0 ? price : throw new ArgumentException("price can't be negtive");
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
