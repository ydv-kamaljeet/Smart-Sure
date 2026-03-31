using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CG.Web.MegaApiClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartSure.Shared.Infrastructure.Interfaces;

namespace SmartSure.Shared.Infrastructure.Services;

public class MegaOptions
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class MegaStorageService : IMegaStorageService
{
    private readonly MegaOptions _options;
    private readonly ILogger<MegaStorageService> _logger;

    public MegaStorageService(IOptions<MegaOptions> options, ILogger<MegaStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderName)
    {
        var client = new MegaApiClient();
        
        try
        {
            _logger.LogInformation("Logging in to MEGA.io with email: {Email}", _options.Email);
            await client.LoginAsync(_options.Email, _options.Password);

            var nodes = await client.GetNodesAsync();
            var root = nodes.Single(x => x.Type == NodeType.Root);
            
            // Find or Create Folder
            var folder = nodes.FirstOrDefault(x => x.Type == NodeType.Directory && x.Name == folderName);
            if (folder == null)
            {
                _logger.LogInformation("Creating folder '{FolderName}' in MEGA root", folderName);
                folder = await client.CreateFolderAsync(folderName, root);
            }

            // Upload File
            _logger.LogInformation("Uploading file '{FileName}' to MEGA", fileName);
            var node = await client.UploadAsync(fileStream, fileName, folder);

            // Generate Shareable Link
            var uri = await client.GetDownloadLinkAsync(node);
            
            _logger.LogInformation("Successfully uploaded file. Generated link: {Link}", uri);
            
            await client.LogoutAsync();
            return uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to MEGA.io");
            if (client.IsLoggedIn) await client.LogoutAsync();
            throw;
        }
    }
}
