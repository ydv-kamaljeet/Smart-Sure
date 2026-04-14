using FluentAssertions;
using Moq;
using NUnit.Framework;
using SmartSure.Policy.Application.DTOs;
using SmartSure.Policy.Application.Interfaces;
using SmartSure.Policy.Application.Services;
using SmartSure.Policy.Domain.Entities;
using MassTransit;

namespace SmartSure.Policy.Tests;

[TestFixture]
public class PolicyManagementServiceTests
{
    private Mock<IPolicyRepository> _policyRepo;
    private Mock<IInsuranceCatalogRepository> _catalogRepo;
    private Mock<IUnitOfWork> _unitOfWork;
    private Mock<IPublishEndpoint> _publishEndpoint;
    private PolicyManagementService _sut;

    [SetUp]
    public void SetUp()
    {
        _policyRepo = new Mock<IPolicyRepository>();
        _catalogRepo = new Mock<IInsuranceCatalogRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _unitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
        _publishEndpoint = new Mock<IPublishEndpoint>();
        _publishEndpoint.Setup(p => p.Publish(It.IsAny<object>(), default)).Returns(Task.CompletedTask);

        _sut = new PolicyManagementService(
            _policyRepo.Object, _catalogRepo.Object,
            _unitOfWork.Object, _publishEndpoint.Object);
    }

    [Test]
    public async Task BuyPolicyAsync_WhenSubTypeNotFound_ReturnsFailure()
    {
        _catalogRepo.Setup(r => r.GetSubTypeByIdAsync(It.IsAny<int>())).ReturnsAsync((InsuranceSubType?)null);

        var result = await _sut.BuyPolicyAsync(Guid.NewGuid(), new BuyPolicyDto(99, null, null));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid");
    }

    [Test]
    public async Task BuyPolicyAsync_WhenSubTypeInactive_ReturnsFailure()
    {
        _catalogRepo.Setup(r => r.GetSubTypeByIdAsync(1))
                    .ReturnsAsync(new InsuranceSubType { Id = 1, IsActive = false });

        var result = await _sut.BuyPolicyAsync(Guid.NewGuid(), new BuyPolicyDto(1, null, null));

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task BuyPolicyAsync_WithVehicleDetails_CalculatesIdvAndCreatesPolicy()
    {
        var userId = Guid.NewGuid();
        _catalogRepo.Setup(r => r.GetSubTypeByIdAsync(1))
                    .ReturnsAsync(new InsuranceSubType { Id = 1, Name = "Light Vehicle", IsActive = true, BasePremium = 3500 });
        _policyRepo.Setup(r => r.GetPolicyHolderAsync(userId))
                   .ReturnsAsync(new PolicyHolder { UserId = userId, FullName = "Test User" });
        _policyRepo.Setup(r => r.AddPolicyAsync(It.IsAny<SmartSure.Policy.Domain.Entities.Policy>())).Returns(Task.CompletedTask);

        var vehicleDto = new CreateVehicleDetailsDto(
            "Toyota", "Corolla", DateTime.Now.Year - 1,
            800000, "VIN123", "MH01AB1234", 10000);

        var result = await _sut.BuyPolicyAsync(userId, new BuyPolicyDto(1, null, vehicleDto));

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);
        _policyRepo.Verify(r => r.AddPolicyAsync(It.IsAny<SmartSure.Policy.Domain.Entities.Policy>()), Times.Once);
    }

    [Test]
    public async Task CancelPolicyAsync_WhenPolicyNotFound_ReturnsFailure()
    {
        _policyRepo.Setup(r => r.GetPolicyByIdAsync(It.IsAny<Guid>()))
                   .ReturnsAsync((SmartSure.Policy.Domain.Entities.Policy?)null);

        var result = await _sut.CancelPolicyAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Test]
    public async Task CancelPolicyAsync_WhenAlreadyCancelled_ReturnsFailure()
    {
        _policyRepo.Setup(r => r.GetPolicyByIdAsync(It.IsAny<Guid>()))
                   .ReturnsAsync(new SmartSure.Policy.Domain.Entities.Policy { Status = "Cancelled" });

        var result = await _sut.CancelPolicyAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already cancelled");
    }

    [Test]
    public async Task CancelPolicyAsync_WithActivePolicy_CancelsAndPublishesEvent()
    {
        var policy = new SmartSure.Policy.Domain.Entities.Policy
        {
            Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = "Active"
        };
        _policyRepo.Setup(r => r.GetPolicyByIdAsync(policy.Id)).ReturnsAsync(policy);
        _policyRepo.Setup(r => r.UpdatePolicyAsync(It.IsAny<SmartSure.Policy.Domain.Entities.Policy>())).Returns(Task.CompletedTask);

        var result = await _sut.CancelPolicyAsync(policy.Id);

        result.IsSuccess.Should().BeTrue();
        policy.Status.Should().Be("Cancelled");
        _publishEndpoint.Verify(p => p.Publish(It.IsAny<SmartSure.Shared.Contracts.Events.PolicyCancelledEvent>(), default), Times.Once);
    }
}
