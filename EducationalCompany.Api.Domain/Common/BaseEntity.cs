namespace EducationalCompany.Api.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; protected set; }

        public DateTime CreatedAt { get; protected set; }

        public DateTime? UpdatedAt { get; protected set; } 

        protected BaseEntity()
        {
            CreatedAt = DateTime.UtcNow;
        }
        public void UpdateModifiedDate()
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
