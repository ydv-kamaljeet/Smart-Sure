using FluentAssertions;
using Moq;
using NUnit.Framework;
using SmartSure.Claims.Application.DTOs;
using SmartSure.Claims.Application.Interfaces;
using SmartSure.Claims.Application.Services;
using SmartSure.Claims.Domain.Entities;
using MassTransit;

namespace SmartSure.Claims.Tests;

[TestFixture]
public class ClaimManagementServiceTests
{
    private Mock<IClaimRepository> _claimRepo;
    private Mock<IClaimHistoryRepository> _historyRepo;
    private Mock<IPublishEndpoint> _publishEndpoint;
    private Mock<IUnitOfWork> _unitOfWork;
    private ClaimManagementService _sut;

    [SetUp]
    public void SetUp()
    {
        _claimRepo = new Mock<IClaimRepository>();
        _historyRepo = new Mock<IClaimHistoryRepository>();
        _publishEndpoint = new Mock<IPublishEndpoint>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _unitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
        _historyRepo.Setup(r => r.AddHistoryTokenAsync(It.IsAny<ClaimHistory>())).Returns(Task.CompletedTask);

        _sut = new ClaimManagementService(
            _claimRepo.Object, _historyRepo.Object,
            _publishEndpoint.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task InitiateClaimAsync_WhenPolicyNotFound_ReturnsFailure()
    {
        _claimRepo.Setup(r => r.GetValidPolicyAsync(It.IsAny<Guid>())).ReturnsAsync((ValidPolicy?)null);

        var result = await _sut.InitiateClaimAsync(Guid.NewGuid(),
            new CreateClaimDto(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), "Test", 5000));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("does not exist");
    }

    [Test]
    public async Task InitiateClaimAsync_WhenUserDoesNotOwnPolicy_ReturnsFailure()
    {
        var policyId = Guid.NewGuid();
        _claimRepo.Setup(r => r.GetValidPolicyAsync(policyId))
                  .ReturnsAsync(new ValidPolicy { PolicyId = policyId, UserId = Guid.NewGuid(), Status = "Active" });

        var result = await _sut.InitiateClaimAsync(Guid.NewGuid(),
            new CreateClaimDto(policyId, DateTime.UtcNow.AddDays(-1), "Test", 5000));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unauthorized");
    }

    [Test]
    public async Task InitiateClaimAsync_WhenPolicyCancelled_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        _claimRepo.Setup(r => r.GetValidPolicyAsync(policyId))
                  .ReturnsAsync(new ValidPolicy { PolicyId = policyId, UserId = userId, Status = "Cancelled" });

        var result = await _sut.InitiateClaimAsync(userId,
            new CreateClaimDto(policyId, DateTime.UtcNow.AddDays(-1), "Test", 5000));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cancelled");
    }

    [Test]
    public async Task InitiateClaimAsync_WhenAmountExceedsIdv_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        _claimRepo.Setup(r => r.GetValidPolicyAsync(policyId))
                  .ReturnsAsync(new ValidPolicy { PolicyId = policyId, UserId = userId, Status = "Active", InsuredDeclaredValue = 10000 });

        var result = await _sut.InitiateClaimAsync(userId,
            new CreateClaimDto(policyId, DateTime.UtcNow.AddDays(-1), "Test", 50000));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("IDV");
    }

    [Test]
    public async Task InitiateClaimAsync_WhenIncidentDateInFuture_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        _claimRepo.Setup(r => r.GetValidPolicyAsync(policyId))
                  .ReturnsAsync(new ValidPolicy { PolicyId = policyId, UserId = userId, Status = "Active" });

        var result = await _sut.InitiateClaimAsync(userId,
            new CreateClaimDto(policyId, DateTime.UtcNow.AddDays(1), "Test", 5000));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("future");
    }

    [Test]
    public async Task InitiateClaimAsync_WithValidData_CreatesClaimAndPublishesEvent()
    {
        var userId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        _claimRepo.Setup(r => r.GetValidPolicyAsync(policyId))
                  .ReturnsAsync(new ValidPolicy
                  {
                      PolicyId = policyId, UserId = userId, Status = "Active",
                      PolicyNumber = "POL-001", CustomerName = "Test User",
                      InsuredDeclaredValue = 100000,
                      StartDate = DateTime.UtcNow.AddMonths(-1),
                      EndDate = DateTime.UtcNow.AddYears(1)
                  });
        _claimRepo.Setup(r => r.AddClaimAsync(It.IsAny<Claim>())).Returns(Task.CompletedTask);
        _publishEndpoint.Setup(p => p.Publish(It.IsAny<object>(), default)).Returns(Task.CompletedTask);

        var result = await _sut.InitiateClaimAsync(userId,
            new CreateClaimDto(policyId, DateTime.UtcNow.AddDays(-1), "Accident", 5000));

        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be("Submitted");
        result.Data!.ClaimNumber.Should().StartWith("CLM-");
        _claimRepo.Verify(r => r.AddClaimAsync(It.IsAny<Claim>()), Times.Once);
    }
}
