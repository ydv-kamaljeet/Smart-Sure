using System.IO;
using System.Threading.Tasks;

namespace SmartSure.Shared.Infrastructure.Interfaces;

public interface IMegaStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderName);
}
