using EducationalCompany.Api.Domain.Common;

namespace EducationalCompany.Api.Domain.Entities
{
    public class Teacher : BaseEntity
    {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Email { get; private set; }
        public string Phone { get; private set; }
        public string Specialization { get; private set; }
        public ICollection<CourseOccasion> CourseOccasion { get; private set; } = new List<CourseOccasion>();

        protected Teacher()
        {
        }
        public Teacher(string firstName, string lastName, string email, string phoneNumber)
        {
            FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
            LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            Phone = Phone ?? throw new ArgumentNullException(nameof(phoneNumber));
            Specialization = Specialization ?? throw new ArgumentNullException(nameof(Specialization));
            CreatedAt = DateTime.UtcNow;
        }
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
