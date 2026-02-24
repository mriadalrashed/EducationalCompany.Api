using EducationalCompany.Api.Domain.Common;

namespace EducationalCompany.Api.Domain.Entities
{
    // Represents a student who can register for course occasions
    public class Participant : BaseEntity
    {
        public string FirstName { get; private set; }

        public string LastName { get; private set; }

        public string Email { get; private set; }

        public string Phone { get; private set; }

        public string Address { get; private set; }

        // List of course registrations for this participant
        public ICollection<CourseRegistration> Registrations { get; private set; } = new List<CourseRegistration>();

        // Required by EF Core
        protected Participant()
        {
        }

        // Creates a new participant with basic validation
        public Participant(string firstName, string lastName, string email, string phone, string address)
        {
            FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
            LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            Phone = phone ?? throw new ArgumentNullException(nameof(phone));
            Address = address ?? throw new ArgumentNullException(nameof(address));
            CreatedAt = DateTime.UtcNow;
        }

        // Updates participant personal information
        public void Update(string firstName, string lastName, string email, string phone, string address)
        {
            FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
            LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            Phone = phone ?? throw new ArgumentNullException(nameof(phone));
            Address = address ?? throw new ArgumentNullException(nameof(address));
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
