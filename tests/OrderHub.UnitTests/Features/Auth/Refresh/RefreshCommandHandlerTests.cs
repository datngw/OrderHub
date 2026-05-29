using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Auth;
using OrderHub.Application.Features.Auth.Refresh;
using OrderHub.Domain.Common;
using OrderHub.Domain.Users;
using OrderHub.UnitTests.Helpers;

namespace OrderHub.UnitTests.Features.Auth.Refresh;

public class RefreshCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ITokenService> _tokenService;
    private readonly IOptions<JwtOptions> _jwtOptions;
    private readonly Mock<IDateTimeProvider> _clock;
    private readonly RefreshCommandHandler _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public RefreshCommandHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();

        _refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _tokenService = new Mock<ITokenService>();
        _jwtOptions = MockHelpers.CreateJwtOptions();
        _clock = new Mock<IDateTimeProvider>();
        var logger = Mock.Of<ILogger<RefreshCommandHandler>>();

        _sut = new RefreshCommandHandler(
            _refreshTokenRepository.Object,
            _unitOfWork.Object,
            _tokenService.Object,
            _jwtOptions,
            _clock.Object,
            logger);
    }

    private static User CreateTestUser()
    {
        return new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Email = "user@example.com",
            PasswordHash = "hashed",
            FullName = "Test User",
            Role = UserRoleEnum.Customer
        };
    }

    private static RefreshToken CreateValidRefreshToken(User user)
    {
        return RefreshToken.Create(
            "valid-refresh-token",
            user.Id,
            FixedNow.AddDays(7));
    }

    private static RefreshToken CreateExpiredRefreshToken(User user)
    {
        return RefreshToken.Create(
            "expired-refresh-token",
            user.Id,
            FixedNow.AddDays(-1));
    }

    [Fact]
    public async Task Handle_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var user = CreateTestUser();
        var existingToken = CreateValidRefreshToken(user);
        existingToken.User = user;

        var command = new RefreshCommand("valid-refresh-token");

        _refreshTokenRepository
            .Setup(r => r.GetByTokenWithUserAsync("valid-refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);

        _clock
            .Setup(c => c.UtcNow)
            .Returns(FixedNow);

        _tokenService
            .Setup(t => t.GenerateRefreshToken())
            .Returns("new-refresh-token");

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _tokenService
            .Setup(t => t.GenerateAccessToken(user.Id, user.Email, "Customer"))
            .Returns("new-access-token");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("new-refresh-token");
        result.Value.Email.Should().Be("user@example.com");
        result.Value.FullName.Should().Be("Test User");
        result.Value.Role.Should().Be("Customer");

        existingToken.IsRevoked.Should().BeTrue();
        existingToken.ReplacedByTokenId.Should().NotBeNull();

        _refreshTokenRepository.Verify(r => r.Add(It.IsAny<RefreshToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new RefreshCommand("nonexistent-token");

        _refreshTokenRepository
            .Setup(r => r.GetByTokenWithUserAsync("nonexistent-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.InvalidRefreshToken);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser();
        var expiredToken = CreateExpiredRefreshToken(user);
        expiredToken.User = user;

        var command = new RefreshCommand("expired-refresh-token");

        _refreshTokenRepository
            .Setup(r => r.GetByTokenWithUserAsync("expired-refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        _clock
            .Setup(c => c.UtcNow)
            .Returns(FixedNow);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.RefreshTokenExpired);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RevokedToken_RevokesEntireFamily()
    {
        // Arrange
        var user = CreateTestUser();
        var revokedToken = RefreshToken.Create(
            "revoked-refresh-token",
            user.Id,
            FixedNow.AddDays(7));
        revokedToken.User = user;
        revokedToken.Revoke(); // already revoked

        var activeToken1 = RefreshToken.Create("active-token-1", user.Id, FixedNow.AddDays(7));
        activeToken1.User = user;

        var activeToken2 = RefreshToken.Create("active-token-2", user.Id, FixedNow.AddDays(7));
        activeToken2.User = user;

        var command = new RefreshCommand("revoked-refresh-token");

        _refreshTokenRepository
            .Setup(r => r.GetByTokenWithUserAsync("revoked-refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedToken);

        _clock
            .Setup(c => c.UtcNow)
            .Returns(FixedNow);

        _refreshTokenRepository
            .Setup(r => r.GetActiveTokensByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([activeToken1, activeToken2]);

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.RefreshTokenRevoked);

        activeToken1.IsRevoked.Should().BeTrue();
        activeToken2.IsRevoked.Should().BeTrue();

        _refreshTokenRepository.Verify(
            r => r.GetActiveTokensByUserIdAsync(user.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
