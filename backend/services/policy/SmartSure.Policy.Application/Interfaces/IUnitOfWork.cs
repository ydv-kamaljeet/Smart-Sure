namespace SmartSure.Policy.Application.Interfaces;

public interface IUnitOfWork
{
    Task SaveChangesAsync();
}
