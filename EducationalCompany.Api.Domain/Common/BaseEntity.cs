namespace EducationalCompany.Api.Domain.Common
{
    // Base entity that gives every entity an Id and timestamps
    public abstract class BaseEntity
    {
        public Guid Id { get; protected set; }

        public DateTime CreatedAt { get; protected set; }

        public DateTime? UpdatedAt { get; protected set; }

        // Set creation time when the entity is created
        protected BaseEntity()
        {
            CreatedAt = DateTime.UtcNow;
        }

        // Update the modified time when the entity changes
        public void UpdateModifiedDate()
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
