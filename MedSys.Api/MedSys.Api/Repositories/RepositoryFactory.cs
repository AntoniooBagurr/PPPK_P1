using MedSys.Api.Data;

namespace MedSys.Api.Repositories;

public class RepositoryFactory : IRepositoryFactory
{
    private readonly AppDb _db;
    public RepositoryFactory(AppDb db) => _db = db;
    public IRepository<T> Create<T>() where T : class => new EfRepository<T>(_db);
}
