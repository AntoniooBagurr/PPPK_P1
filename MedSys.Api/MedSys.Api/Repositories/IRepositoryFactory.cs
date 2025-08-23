namespace MedSys.Api.Repositories;

public interface IRepositoryFactory
{
    IRepository<T> Create<T>() where T : class;
}
