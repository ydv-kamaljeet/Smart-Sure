using System.Threading.Tasks;

namespace SmartSure.Claims.Application.Interfaces;

public interface IUnitOfWork
{
    Task SaveChangesAsync();
}
