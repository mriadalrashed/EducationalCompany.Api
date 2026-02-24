namespace EducationalCompany.Api.Domain.Interfaces
{
    // Generic repository contract for basic CRUD operations
    public interface IRepository<T> where T : class
    {
        // Retrieves an entity by its Id
        Task<T> GetByIdAsync(Guid id);

        // Retrieves all entities of type T
        Task<IEnumerable<T>> GetAllAsync();

        // Adds a new entity
        Task AddAsync(T entity);

        // Updates an existing entity
        Task UpdateAsync(T entity);

        // Deletes an entity by Id
        Task DeleteAsync(Guid id);

        // Checks if an entity exists by Id
        Task<bool> ExistsAsync(Guid id);
    }
}
