using EducationalCompany.Api.Domain.Common;
using System.Collections.ObjectModel;


namespace EducationalCompany.Api.Domain.Entities
{
    public class Course : BaseEntity
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public int DurationHours { get; private set; }
        public decimal Price { get; private set; }
        public ICollection<CourseOccasion> Occasions { get; private set; } = new List<CourseOccasion>();

        protected Course()
        {
        }

        public Course(string name, string description, int durationHours, decimal price)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(name)); ;
            DurationHours = durationHours > 0 ? durationHours : throw new ArgumentException("duration must be positive");
            Price = price >= 0 ? price : throw new ArgumentException("price can't be nagtive");
            CreatedAt = DateTime.UtcNow;
        }

        public void Update(string name, string description, int durationHours, decimal price)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(name)); ;
            DurationHours = durationHours > 0 ? durationHours : throw new ArgumentException("duration must be positive");
            Price = price >= 0 ? price : throw new ArgumentException("price can't be nagtive");
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
