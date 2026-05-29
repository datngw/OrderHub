using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Auth;
using OrderHub.Application.Features.Auth.Login;
using OrderHub.Domain.Common;
using OrderHub.Domain.Users;
using OrderHub.UnitTests.Shared;

namespace OrderHub.UnitTests.Features.Auth.Login;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ITokenService> _tokenService;
    private readonly Mock<IPasswordHasher> _passwordHasher;
    private readonly IOptions<JwtOptions> _jwtOptions;
    private readonly Mock<IDateTimeProvider> _clock;
    private readonly LoginCommandHandler _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public LoginCommandHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();

        _userRepository = new Mock<IUserRepository>();
        _refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _tokenService = new Mock<ITokenService>();
        _passwordHasher = new Mock<IPasswordHasher>();
        _jwtOptions = MockHelpers.CreateJwtOptions();
        _clock = new Mock<IDateTimeProvider>();
        var logger = Mock.Of<ILogger<LoginCommandHandler>>();

        _sut = new LoginCommandHandler(
            _userRepository.Object,
            _refreshTokenRepository.Object,
            _unitOfWork.Object,
            _tokenService.Object,
            _passwordHasher.Object,
            _jwtOptions,
            _clock.Object,
            logger);
    }

    private static User CreateTestUser()
    {
        return new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = "user@example.com",
            PasswordHash = "hashed-password",
            FullName = "Test User",
            Role = UserRoleEnum.Customer
        };
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsSuccessWithTokens()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new LoginCommand("user@example.com", "P@ssw0rd!");

        _userRepository
            .Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasher
            .Setup(h => h.VerifyPassword("P@ssw0rd!", "hashed-password"))
            .Returns(true);

        _tokenService
            .Setup(t => t.GenerateAccessToken(user.Id, user.Email, "Customer"))
            .Returns("access-token");

        _tokenService
            .Setup(t => t.GenerateRefreshToken())
            .Returns("new-refresh-token");

        _clock
            .Setup(c => c.UtcNow)
            .Returns(FixedNow);

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("new-refresh-token");
        result.Value.Email.Should().Be("user@example.com");
        result.Value.FullName.Should().Be("Test User");
        result.Value.Role.Should().Be("Customer");

        _refreshTokenRepository.Verify(r => r.Add(It.IsAny<RefreshToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new LoginCommand("unknown@example.com", "P@ssw0rd!");

        _userRepository
            .Setup(r => r.GetByEmailAsync("unknown@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.InvalidCredentials);

        _passwordHasher.Verify(
            h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _refreshTokenRepository.Verify(r => r.Add(It.IsAny<RefreshToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new LoginCommand("user@example.com", "wrong-password");

        _userRepository
            .Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasher
            .Setup(h => h.VerifyPassword("wrong-password", "hashed-password"))
            .Returns(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.InvalidCredentials);

        _refreshTokenRepository.Verify(r => r.Add(It.IsAny<RefreshToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
