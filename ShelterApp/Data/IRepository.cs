using System.Linq.Expressions;

namespace ShelterApp.Data
{
    public interface IRepository<T> where T : class
    {
        // Get all entities (optional query customization with predicate, include, etc.)
        Task<IEnumerable<T>> GetAllAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            string? includeProperties = null);

        // Get a single entity by its primary key (e.g., ID) (optional query customization with predicate, include, etc.)
        Task<T?> GetByIdAsync(object id,
            Expression<Func<T, bool>>? filter = null,
            string? includeProperties = null);

        // Add a new entity
        Task AddAsync(T entity);

        // Add multiple entities
        Task AddRangeAsync(IEnumerable<T> entities);

        // Update an existing entity
        void Update(T entity);

        // Remove an entity by its instance
        void Remove(T entity);

        // Remove an entity by its primary key (e.g., ID)
        Task RemoveByIdAsync(object id);

        // Remove multiple entities
        void RemoveRange(IEnumerable<T> entities);

        // Save changes to the database (optional, if not part of UnitOfWork)
        Task SaveChangesAsync();

        Task<int> CountAsync();
    }
}
