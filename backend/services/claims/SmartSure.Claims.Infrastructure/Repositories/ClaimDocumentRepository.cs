using Microsoft.EntityFrameworkCore;
using SmartSure.Claims.Application.Interfaces;
using SmartSure.Claims.Domain.Entities;
using SmartSure.Claims.Infrastructure.Data;

namespace SmartSure.Claims.Infrastructure.Repositories;

public class ClaimDocumentRepository : IClaimDocumentRepository
{
    private readonly ClaimsDbContext _context;

    public ClaimDocumentRepository(ClaimsDbContext context)
    {
        _context = context;
    }

    public async Task AddDocumentAsync(ClaimDocument document)
    {
        await _context.ClaimDocuments.AddAsync(document);
    }

    public Task DeleteDocumentAsync(ClaimDocument document)
    {
        _context.ClaimDocuments.Remove(document);
        return Task.CompletedTask;
    }

    public async Task<ClaimDocument?> GetDocumentByIdAsync(int documentId)
    {
        return await _context.ClaimDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
    }

    public async Task<IEnumerable<ClaimDocument>> GetDocumentsByClaimIdAsync(int claimId)
    {
        return await _context.ClaimDocuments
                             .Where(d => d.ClaimId == claimId)
                             .OrderByDescending(d => d.UploadedAt)
                             .ToListAsync();
    }
}
