using FluentAssertions;
using Moq;
using NUnit.Framework;
using SmartSure.Identity.Application.Interfaces;
using SmartSure.Identity.Application.Services;
using SmartSure.Identity.Domain.Entities;

namespace SmartSure.Identity.Tests;

[TestFixture]
public class OtpServiceTests
{
    private Mock<IOtpRepository> _otpRepo;
    private Mock<IUnitOfWork> _unitOfWork;
    private OtpService _sut;

    [SetUp]
    public void SetUp()
    {
        _otpRepo = new Mock<IOtpRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _sut = new OtpService(_otpRepo.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task GenerateOtpAsync_DeletesExistingOtpBeforeCreatingNew()
    {
        // Arrange
        var existing = new OtpRecord { Email = "user@test.com" };
        _otpRepo.Setup(r => r.GetByEmailAsync("user@test.com")).ReturnsAsync(existing);
        _otpRepo.Setup(r => r.DeleteAsync(existing)).Returns(Task.CompletedTask);
        _otpRepo.Setup(r => r.AddAsync(It.IsAny<OtpRecord>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.GenerateOtpAsync("user@test.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveLength(6);
        _otpRepo.Verify(r => r.DeleteAsync(existing), Times.Once);
        _otpRepo.Verify(r => r.AddAsync(It.IsAny<OtpRecord>()), Times.Once);
    }

    [Test]
    public async Task ValidateOtpAsync_WhenOtpNotFound_ReturnsFailure()
    {
        // Arrange
        _otpRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((OtpRecord?)null);

        // Act
        var result = await _sut.ValidateOtpAsync("user@test.com", "123456");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Test]
    public async Task ValidateOtpAsync_WhenOtpExpired_DeletesAndReturnsFailure()
    {
        // Arrange
        var record = new OtpRecord
        {
            Email = "user@test.com",
            HashedOtp = BCrypt.Net.BCrypt.HashPassword("123456"),
            Expiry = DateTime.UtcNow.AddMinutes(-1) // expired
        };
        _otpRepo.Setup(r => r.GetByEmailAsync("user@test.com")).ReturnsAsync(record);
        _otpRepo.Setup(r => r.DeleteAsync(record)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.ValidateOtpAsync("user@test.com", "123456");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("expired");
        _otpRepo.Verify(r => r.DeleteAsync(record), Times.Once);
    }

    [Test]
    public async Task ValidateOtpAsync_WhenMaxAttemptsReached_DeletesAndReturnsFailure()
    {
        // Arrange
        var record = new OtpRecord
        {
            Email = "user@test.com",
            HashedOtp = BCrypt.Net.BCrypt.HashPassword("123456"),
            Expiry = DateTime.UtcNow.AddMinutes(10),
            Attempts = 3 // max reached
        };
        _otpRepo.Setup(r => r.GetByEmailAsync("user@test.com")).ReturnsAsync(record);
        _otpRepo.Setup(r => r.DeleteAsync(record)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.ValidateOtpAsync("user@test.com", "123456");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Too many");
    }

    [Test]
    public async Task ValidateOtpAsync_WhenOtpIsWrong_IncrementsAttempts()
    {
        // Arrange
        var record = new OtpRecord
        {
            Email = "user@test.com",
            HashedOtp = BCrypt.Net.BCrypt.HashPassword("123456"),
            Expiry = DateTime.UtcNow.AddMinutes(10),
            Attempts = 0
        };
        _otpRepo.Setup(r => r.GetByEmailAsync("user@test.com")).ReturnsAsync(record);
        _otpRepo.Setup(r => r.UpdateAsync(record)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.ValidateOtpAsync("user@test.com", "999999");

        // Assert
        result.IsSuccess.Should().BeFalse();
        record.Attempts.Should().Be(1);
    }

    [Test]
    public async Task ValidateOtpAsync_WhenOtpIsCorrect_DeletesRecordAndReturnsSuccess()
    {
        // Arrange
        var otpCode = "123456";
        var record = new OtpRecord
        {
            Email = "user@test.com",
            HashedOtp = BCrypt.Net.BCrypt.HashPassword(otpCode),
            Expiry = DateTime.UtcNow.AddMinutes(10),
            Attempts = 0
        };
        _otpRepo.Setup(r => r.GetByEmailAsync("user@test.com")).ReturnsAsync(record);
        _otpRepo.Setup(r => r.DeleteAsync(record)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.ValidateOtpAsync("user@test.com", otpCode);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _otpRepo.Verify(r => r.DeleteAsync(record), Times.Once);
    }
}
