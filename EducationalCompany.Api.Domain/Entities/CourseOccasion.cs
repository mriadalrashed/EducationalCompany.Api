using EducationalCompany.Api.Domain.Common;
using System.Collections.ObjectModel;

namespace EducationalCompany.Api.Domain.Entities
{
    public class CourseOccasion : BaseEntity
    {
        public Guid CourseId { get; private set; }
        public Guid TeacherId { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public int MaxParticipants { get; private set; }
        public int CurrentParticipants { get; private set; }
        public bool IsFull => CurrentParticipants >= MaxParticipants;

        public Course Course { get; protected set; }
        public Teacher Teacher { get; protected set; }

        public ICollection<CourseRegistration> Registrations { get; private set; } = new List<CourseRegistration>();

        protected CourseOccasion()
        {
        }

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

        public void AssignTeacher(Guid teacherId)
        {
            TeacherId = teacherId;
            UpdatedAt = DateTime.UtcNow;
        }

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
