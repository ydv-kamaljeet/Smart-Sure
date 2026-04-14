using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using SmartSure.Identity.Application.DTOs;
using SmartSure.Identity.Application.Interfaces;
using SmartSure.Identity.Application.Services;
using SmartSure.Identity.Domain.Entities;
using SmartSure.Shared.Security.Jwt;
using MassTransit;

namespace SmartSure.Identity.Tests;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUserRepository> _userRepo;
    private Mock<IRoleRepository> _roleRepo;
    private Mock<IUnitOfWork> _unitOfWork;
    private Mock<IJwtTokenGenerator> _jwtGenerator;
    private Mock<IEmailService> _emailService;
    private Mock<IOtpService> _otpService;
    private Mock<ITokenBlacklistService> _tokenBlacklist;
    private Mock<IPublishEndpoint> _publishEndpoint;
    private Mock<IConfiguration> _configuration;
    private AuthService _sut;

    [SetUp]
    public void SetUp()
    {
        _userRepo = new Mock<IUserRepository>();
        _roleRepo = new Mock<IRoleRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _jwtGenerator = new Mock<IJwtTokenGenerator>();
        _emailService = new Mock<IEmailService>();
        _otpService = new Mock<IOtpService>();
        _tokenBlacklist = new Mock<ITokenBlacklistService>();
        _publishEndpoint = new Mock<IPublishEndpoint>();
        _configuration = new Mock<IConfiguration>();

        _configuration.Setup(c => c["AppSettings:BaseUrl"]).Returns("http://localhost:4200");

        _sut = new AuthService(
            _userRepo.Object, _roleRepo.Object, _unitOfWork.Object,
            _jwtGenerator.Object, _emailService.Object, _otpService.Object,
            _tokenBlacklist.Object, _publishEndpoint.Object, _configuration.Object);
    }

    // ── RegisterAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ReturnsFailure()
    {
        // Arrange
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com"))
                 .ReturnsAsync(new User { Email = "test@test.com" });

        var dto = new RegisterDto("test@test.com", "Test User", "Password1");

        // Act
        var result = await _sut.RegisterAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already registered");
    }

    [Test]
    public async Task RegisterAsync_WhenDefaultRoleNotFound_ReturnsFailure()
    {
        // Arrange
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _roleRepo.Setup(r => r.GetByNameAsync("Policyholder")).ReturnsAsync((Role?)null);

        var dto = new RegisterDto("new@test.com", "New User", "Password1");

        // Act
        var result = await _sut.RegisterAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Policyholder");
    }

    [Test]
    public async Task RegisterAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _roleRepo.Setup(r => r.GetByNameAsync("Policyholder"))
                 .ReturnsAsync(new Role { RoleId = 2, Name = "Policyholder" });
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailService.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

        var dto = new RegisterDto("new@test.com", "New User", "Password1");

        // Act
        var result = await _sut.RegisterAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    // ── LoginAsync ────────────────────────────────────────────────────────────

    [Test]
    public async Task LoginAsync_WhenUserNotFound_ReturnsFailure()
    {
        // Arrange
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.LoginAsync(new LoginDto("notfound@test.com", "pass"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid credentials");
    }

    [Test]
    public async Task LoginAsync_WhenEmailNotVerified_ReturnsFailure()
    {
        // Arrange
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                 .ReturnsAsync(new User { IsActive = true, IsEmailVerified = false });

        // Act
        var result = await _sut.LoginAsync(new LoginDto("user@test.com", "pass"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("verify your email");
    }

    [Test]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokens()
    {
        // Arrange
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "user@test.com",
            FullName = "Test User",
            IsActive = true,
            IsEmailVerified = true
        };
        user.Passwords.Add(new Password
        {
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1")
        });
        user.UserRoles.Add(new UserRole { Role = new Role { Name = "Policyholder" } });

        _userRepo.Setup(r => r.GetByEmailAsync("user@test.com")).ReturnsAsync(user);
        _jwtGenerator.Setup(j => j.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IList<string>>(), null, null))
                     .Returns("access-token");
        _jwtGenerator.Setup(j => j.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IList<string>>(), "refresh", It.IsAny<int?>()))
                     .Returns("refresh-token");

        // Act
        var result = await _sut.LoginAsync(new LoginDto("user@test.com", "Password1"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("access-token");
        result.Data!.RefreshToken.Should().Be("refresh-token");
    }

    // ── LogoutAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task LogoutAsync_BlacklistsToken()
    {
        // Arrange
        _tokenBlacklist.Setup(t => t.BlacklistTokenAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                       .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.LogoutAsync(Guid.NewGuid(), "some-token");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _tokenBlacklist.Verify(t => t.BlacklistTokenAsync("some-token", TimeSpan.FromHours(1)), Times.Once);
    }

    // ── ForgotPasswordAsync ───────────────────────────────────────────────────

    [Test]
    public async Task ForgotPasswordAsync_WhenUserNotFound_ReturnsSuccessToPreventEnumeration()
    {
        // Arrange — user doesn't exist but we still return success to prevent email enumeration
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.ForgotPasswordAsync("notexist@test.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _otpService.Verify(o => o.GenerateOtpAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ForgotPasswordAsync_WhenUserExists_GeneratesAndSendsOtp()
    {
        // Arrange
        _userRepo.Setup(r => r.GetByEmailAsync("user@test.com"))
                 .ReturnsAsync(new User { Email = "user@test.com" });
        _otpService.Setup(o => o.GenerateOtpAsync("user@test.com"))
                   .ReturnsAsync(SmartSure.Shared.Common.Models.Result<string>.Success("123456"));
        _emailService.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ForgotPasswordAsync("user@test.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _emailService.Verify(e => e.SendEmailAsync("user@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
