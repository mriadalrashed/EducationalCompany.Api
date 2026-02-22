using EducationalCompany.Api.Domain.Common;
using System.Collections.ObjectModel;

namespace EducationalCompany.Api.Domain.Entities
{
    // Represents a specific scheduled session of a course
    public class CourseOccasion : BaseEntity
    {
        public Guid CourseId { get; private set; }
        public Guid TeacherId { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public int MaxParticipants { get; private set; }
        public int CurrentParticipants { get; private set; }

        // Returns true if the session has reached maximum capacity
        public bool IsFull => CurrentParticipants >= MaxParticipants;

        public Course Course { get; protected set; }
        public Teacher Teacher { get; protected set; }

        // List of participant registrations for this session
        public ICollection<CourseRegistration> Registrations { get; private set; } = new List<CourseRegistration>();

        // Required by EF Core
        protected CourseOccasion()
        {
        }


        // Creates a new course occasion with validation rules
        public CourseOccasion(Guid courseId, Guid teacherId, DateTime startDate, DateTime endDate, int maxParticipants)
        {
            if (endDate <= startDate)
            {
                throw new ArgumentException("endDate must be after startDate");
            }

            if (maxParticipants <= 0)
            {
                throw new ArgumentException("maxParticipants must be positive");
            }

            CourseId = courseId;
            TeacherId = teacherId;
            StartDate = startDate;
            EndDate = endDate;
            MaxParticipants = maxParticipants;
            CurrentParticipants = 0;
            CreatedAt = DateTime.UtcNow;
        }


        // Updates schedule and capacity with validation
        public void Update(DateTime startDate, DateTime endDate, int maxParticipants)
        {
            if (endDate <= startDate)
            {
                throw new ArgumentException("endDate must be after startDate");
            }
            if (maxParticipants <= 0)
            {
                throw new ArgumentException("maxParticipants must be positive");
            }
            StartDate = startDate;
            EndDate = endDate;
            MaxParticipants = maxParticipants;
            UpdatedAt = DateTime.UtcNow;
        }

        // Assigns a new teacher to this session
        public void AssignTeacher(Guid teacherId)
        {
            TeacherId = teacherId;
            UpdatedAt = DateTime.UtcNow;
        }

        // Attempts to register a participant (fails if full)
        public bool TryRegisterParticipant()
        {
            if (IsFull)
            {
                return false;
            }
            CurrentParticipants++;
            UpdatedAt = DateTime.UtcNow;
            return true;
        }

        // Cancels a registration and decreases participant count
        public void CancelRegistration()
        {           
            if (CurrentParticipants > 0)
            {
                CurrentParticipants--;
            }
            UpdatedAt = DateTime.UtcNow;
        }

    }
}
