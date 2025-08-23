using System.Linq.Expressions;

namespace MedSys.Api.Repositories;

public interface IRepository<T> where T : class
{
    Task<T?> GetAsync(Guid id);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    Task RemoveAsync(T entity);
    Task SaveChangesAsync();
}
