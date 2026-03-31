using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartSure.Claims.Application.DTOs;
using SmartSure.Claims.Application.Interfaces;

namespace SmartSure.Claims.API.Controllers;

[ApiController]
[Route("api/claims/{claimId}/documents")]
[Authorize]
public class ClaimDocumentsController : ControllerBase
{
    private readonly IClaimDocumentService _documentService;

    public ClaimDocumentsController(IClaimDocumentService documentService)
    {
        _documentService = documentService;
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return idClaim != null ? Guid.Parse(idClaim) : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetDocuments(int claimId)
    {
        // Ideally we would verify claim ownership first. For now, we rely on the client or subsequent service logic.
        var docs = await _documentService.GetDocumentsAsync(claimId);
        return Ok(docs);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocument(int claimId, [FromForm] UploadClaimDocumentDto dto, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { ErrorMessage = "No file uploaded." });

        var userId = GetUserId();
        using var stream = file.OpenReadStream();
        
        var result = await _documentService.UploadDocumentAsync(claimId, userId, dto, stream, file.FileName);
        
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(result.Data);
    }

    [HttpDelete("{docId}")]
    public async Task<IActionResult> DeleteDocument(int claimId, int docId)
    {
        var userId = GetUserId();
        var result = await _documentService.DeleteDocumentAsync(claimId, docId, userId);
        
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return NoContent();
    }
}
