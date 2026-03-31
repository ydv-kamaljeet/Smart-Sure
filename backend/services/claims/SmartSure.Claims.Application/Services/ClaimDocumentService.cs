using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmartSure.Claims.Application.DTOs;
using SmartSure.Claims.Application.Interfaces;
using SmartSure.Claims.Domain.Entities;
using SmartSure.Shared.Common.Models;
using SmartSure.Shared.Infrastructure.Interfaces;

namespace SmartSure.Claims.Application.Services;

public class ClaimDocumentService : IClaimDocumentService
{
    private readonly IClaimRepository _claimRepository;
    private readonly IClaimDocumentRepository _documentRepository;
    private readonly IMegaStorageService _megaStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public ClaimDocumentService(
        IClaimRepository claimRepository, 
        IClaimDocumentRepository documentRepository, 
        IMegaStorageService megaStorageService,
        IUnitOfWork unitOfWork)
    {
        _claimRepository = claimRepository;
        _documentRepository = documentRepository;
        _megaStorageService = megaStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> DeleteDocumentAsync(int claimId, int documentId, Guid userId)
    {
        var claim = await _claimRepository.GetClaimByIdAsync(claimId);
        if (claim == null) return Result.Failure("Claim not found.");
        
        // Admin or Owner validation would go here. Assuming Owner for now.
        if (claim.UserId != userId) return Result.Failure("Unauthorized access to claim.");
        
        var document = await _documentRepository.GetDocumentByIdAsync(documentId);
        if (document == null || document.ClaimId != claimId) return Result.Failure("Document not found.");

        await _documentRepository.DeleteDocumentAsync(document);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<IEnumerable<ClaimDocumentDto>> GetDocumentsAsync(int claimId)
    {
        var docs = await _documentRepository.GetDocumentsByClaimIdAsync(claimId);
        return docs.Select(d => new ClaimDocumentDto(d.Id, d.ClaimId, d.DocumentType, d.FileName, d.FileUrl, d.UploadedAt));
    }

    public async Task<Result<ClaimDocumentDto>> UploadDocumentAsync(int claimId, Guid userId, UploadClaimDocumentDto dto, Stream fileStream, string fileName)
    {
        var claim = await _claimRepository.GetClaimByIdAsync(claimId);
        if (claim == null) return Result<ClaimDocumentDto>.Failure("Claim not found.");

        if (claim.UserId != userId) return Result<ClaimDocumentDto>.Failure("Unauthorized access to claim.");

        if (claim.Status != "Draft" && claim.Status != "UnderReview" && claim.Status != "Submitted")
        {
            return Result<ClaimDocumentDto>.Failure("Documents can only be uploaded while Claim is Draft, Submitted, or Under Review.");
        }

        // Real MEGA Storage Upload
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var folderName = $"Claims/Claim_{claimId}";
        var megaUrl = await _megaStorageService.UploadFileAsync(fileStream, uniqueFileName, folderName);

        var document = new ClaimDocument
        {
            ClaimId = claimId,
            DocumentType = dto.DocumentType,
            FileName = fileName,
            FileUrl = megaUrl,
            UploadedAt = DateTime.UtcNow
        };

        await _documentRepository.AddDocumentAsync(document);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = new ClaimDocumentDto(document.Id, document.ClaimId, document.DocumentType, document.FileName, document.FileUrl, document.UploadedAt);
        return Result<ClaimDocumentDto>.Success(resultDto);
    }
}
