using Moq;
using FluentAssertions;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Features.Auth.Logout;
using OrderHub.Domain.Common;
using OrderHub.Domain.Users;
using OrderHub.UnitTests.Helpers;

namespace OrderHub.UnitTests.Features.Auth.Logout;

public class LogoutCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly LogoutCommandHandler _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public LogoutCommandHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();

        _refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _sut = new LogoutCommandHandler(
            _refreshTokenRepository.Object,
            _unitOfWork.Object);
    }

    private static RefreshToken CreateActiveRefreshToken()
    {
        return RefreshToken.Create(
            "active-refresh-token",
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            FixedNow.AddDays(7));
    }

    private static RefreshToken CreateRevokedRefreshToken()
    {
        var token = RefreshToken.Create(
            "revoked-refresh-token",
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            FixedNow.AddDays(7));
        token.Revoke();
        return token;
    }

    [Fact]
    public async Task Handle_ValidToken_RevokesAndSucceeds()
    {
        // Arrange
        var token = CreateActiveRefreshToken();

        var command = new LogoutCommand("active-refresh-token");

        _refreshTokenRepository
            .Setup(r => r.GetByTokenAsync("active-refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyRevoked_ReturnsSuccessIdempotent()
    {
        // Arrange
        var token = CreateRevokedRefreshToken();

        var command = new LogoutCommand("revoked-refresh-token");

        _refreshTokenRepository
            .Setup(r => r.GetByTokenAsync("revoked-refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new LogoutCommand("nonexistent-token");

        _refreshTokenRepository
            .Setup(r => r.GetByTokenAsync("nonexistent-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.InvalidRefreshToken);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
