using MedSys.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace MedSys.Api.Repositories;

public class EfRepository<T> : IRepository<T> where T : class
{
    private readonly AppDb _db;
    public EfRepository(AppDb db) => _db = db;

    public Task<T?> GetAsync(Guid id) => _db.Set<T>().FindAsync(id).AsTask();

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
        await _db.Set<T>().Where(predicate).ToListAsync();

    public async Task AddAsync(T entity) => await _db.Set<T>().AddAsync(entity);

    public Task RemoveAsync(T entity) { _db.Set<T>().Remove(entity); return Task.CompletedTask; }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
