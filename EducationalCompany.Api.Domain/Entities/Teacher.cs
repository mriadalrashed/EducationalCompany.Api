using EducationalCompany.Api.Domain.Common;

namespace EducationalCompany.Api.Domain.Entities
{
    // Represents a teacher who can be assigned to course occasions
    public class Teacher : BaseEntity
    {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Email { get; private set; }
        public string Phone { get; private set; }
        public string Specialization { get; private set; }

        // List of course occasions taught by this teacher
        public ICollection<CourseOccasion> CourseOccasion { get; private set; } = new List<CourseOccasion>();

        // List of course occasions taught by this teacher
        protected Teacher()
        {
        }

        // Creates a new teacher with basic validation
        public Teacher(string firstName, string lastName, string email, string phoneNumber)
        {
            FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
            LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            Phone = Phone ?? throw new ArgumentNullException(nameof(phoneNumber));
            Specialization = Specialization ?? throw new ArgumentNullException(nameof(Specialization));
            CreatedAt = DateTime.UtcNow;
        }

        // Updates teacher information
        public void Update(string firstName, string lastName, string email, string phoneNumber)
        {
            FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
            LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            Phone = Phone ?? throw new ArgumentNullException(nameof(phoneNumber));
            Specialization = Specialization ?? throw new ArgumentNullException(nameof(Specialization));
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
