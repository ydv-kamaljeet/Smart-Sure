namespace SmartSure.Admin.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync();
}
