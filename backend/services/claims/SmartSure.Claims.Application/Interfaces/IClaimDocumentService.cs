using System.IO;
using System.Threading.Tasks;
using SmartSure.Claims.Application.DTOs;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Claims.Application.Interfaces;

public interface IClaimDocumentService
{
    Task<IEnumerable<ClaimDocumentDto>> GetDocumentsAsync(int claimId);
    Task<Result<ClaimDocumentDto>> UploadDocumentAsync(int claimId, Guid userId, UploadClaimDocumentDto dto, Stream fileStream, string fileName);
    Task<Result> DeleteDocumentAsync(int claimId, int documentId, Guid userId);
}
