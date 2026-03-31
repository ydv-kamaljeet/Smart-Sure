using System.Collections.Generic;
using System.Threading.Tasks;
using SmartSure.Claims.Domain.Entities;

namespace SmartSure.Claims.Application.Interfaces;

public interface IClaimDocumentRepository
{
    Task<IEnumerable<ClaimDocument>> GetDocumentsByClaimIdAsync(int claimId);
    Task<ClaimDocument?> GetDocumentByIdAsync(int documentId);
    Task AddDocumentAsync(ClaimDocument document);
    Task DeleteDocumentAsync(ClaimDocument document);
}
