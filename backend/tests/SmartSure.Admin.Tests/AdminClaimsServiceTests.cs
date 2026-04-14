using FluentAssertions;
using Moq;
using NUnit.Framework;
using SmartSure.Admin.Application.Interfaces;
using SmartSure.Admin.Application.Services;
using SmartSure.Admin.Domain.Entities;
using SmartSure.Shared.Contracts.Events;
using MassTransit;

namespace SmartSure.Admin.Tests;

[TestFixture]
public class AdminClaimsServiceTests
{
    private Mock<IAdminRepository<AdminClaim>> _claimRepo;
    private Mock<IUnitOfWork> _unitOfWork;
    private Mock<IAdminAuditLogService> _auditLogService;
    private Mock<IBus> _bus;
    private AdminClaimsService _sut;

    [SetUp]
    public void SetUp()
    {
        _claimRepo = new Mock<IAdminRepository<AdminClaim>>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _auditLogService = new Mock<IAdminAuditLogService>();
        _bus = new Mock<IBus>();

        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _auditLogService.Setup(a => a.LogActionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                        .Returns(Task.CompletedTask);

        _sut = new AdminClaimsService(_claimRepo.Object, _unitOfWork.Object, _auditLogService.Object, _bus.Object);
    }

    [Test]
    public async Task ApproveClaimAsync_WhenClaimNotFound_ReturnsFalse()
    {
        _claimRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AdminClaim>());

        var result = await _sut.ApproveClaimAsync(999, "Approved");

        result.Should().BeFalse();
    }

    [Test]
    public async Task ApproveClaimAsync_WithValidClaim_ApprovesAndPublishesEvent()
    {
        var claim = new AdminClaim
        {
            Id = 1, ClaimId = 100, PolicyId = Guid.NewGuid(),
            UserId = Guid.NewGuid(), ClaimAmount = 5000,
            Status = "Submitted", PolicyNumber = "POL-001"
        };
        _claimRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AdminClaim> { claim });
        _claimRepo.Setup(r => r.UpdateAsync(claim)).Returns(Task.CompletedTask);
        _bus.Setup(b => b.Publish(It.IsAny<ClaimApprovedEvent>(), default)).Returns(Task.CompletedTask);

        var result = await _sut.ApproveClaimAsync(100, "Looks good");

        result.Should().BeTrue();
        claim.Status.Should().Be("Approved");
        _bus.Verify(b => b.Publish(It.IsAny<ClaimApprovedEvent>(), default), Times.Once);
    }

    [Test]
    public async Task RejectClaimAsync_WithValidClaim_RejectsAndPublishesEvent()
    {
        var claim = new AdminClaim
        {
            Id = 1, ClaimId = 100, PolicyId = Guid.NewGuid(),
            UserId = Guid.NewGuid(), ClaimAmount = 5000,
            Status = "Submitted", PolicyNumber = "POL-001"
        };
        _claimRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AdminClaim> { claim });
        _claimRepo.Setup(r => r.UpdateAsync(claim)).Returns(Task.CompletedTask);
        _bus.Setup(b => b.Publish(It.IsAny<ClaimRejectedEvent>(), default)).Returns(Task.CompletedTask);

        var result = await _sut.RejectClaimAsync(100, "Insufficient evidence");

        result.Should().BeTrue();
        claim.Status.Should().Be("Rejected");
        _bus.Verify(b => b.Publish(It.IsAny<ClaimRejectedEvent>(), default), Times.Once);
    }

    [Test]
    public async Task MarkAsUnderReviewAsync_WithValidClaim_UpdatesStatusAndPublishesEvent()
    {
        var claim = new AdminClaim
        {
            Id = 1, ClaimId = 100, PolicyId = Guid.NewGuid(),
            UserId = Guid.NewGuid(), Status = "Submitted", PolicyNumber = "POL-001"
        };
        _claimRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AdminClaim> { claim });
        _claimRepo.Setup(r => r.UpdateAsync(claim)).Returns(Task.CompletedTask);
        _bus.Setup(b => b.Publish(It.IsAny<ClaimStatusChangedEvent>(), default)).Returns(Task.CompletedTask);

        var result = await _sut.MarkAsUnderReviewAsync(100, "Under investigation");

        result.Should().BeTrue();
        claim.Status.Should().Be("Under Review");
    }
}
