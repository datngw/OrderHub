using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;
using OrderHub.Application.Common;
using OrderHub.Application.Common.Persistence;
using OrderHub.Application.Common.Security;
using OrderHub.Application.Features.Auth;
using OrderHub.Application.Features.Auth.Register;
using OrderHub.Domain.Common;
using OrderHub.Domain.Users;
using OrderHub.UnitTests.Shared;

namespace OrderHub.UnitTests.Features.Auth.Register;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ITokenService> _tokenService;
    private readonly Mock<IPasswordHasher> _passwordHasher;
    private readonly IOptions<JwtOptions> _jwtOptions;
    private readonly Mock<IDateTimeProvider> _clock;
    private readonly RegisterCommandHandler _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public RegisterCommandHandlerTests()
    {
        TestHelper.EnsureMapsterInitialized();

        _userRepository = new Mock<IUserRepository>();
        _refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _tokenService = new Mock<ITokenService>();
        _passwordHasher = new Mock<IPasswordHasher>();
        _jwtOptions = MockHelpers.CreateJwtOptions();
        _clock = new Mock<IDateTimeProvider>();
        var logger = Mock.Of<ILogger<RegisterCommandHandler>>();

        _sut = new RegisterCommandHandler(
            _userRepository.Object,
            _refreshTokenRepository.Object,
            _unitOfWork.Object,
            _tokenService.Object,
            _passwordHasher.Object,
            _jwtOptions,
            _clock.Object,
            logger);
    }

    [Fact]
    public async Task Handle_NewUser_ReturnsSuccessWithTokens()
    {
        // Arrange
        var command = new RegisterCommand("test@example.com", "P@ssw0rd!", "Test User");

        _userRepository
            .Setup(r => r.ExistsByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasher
            .Setup(h => h.HashPassword("P@ssw0rd!"))
            .Returns("hashed-password");

        _tokenService
            .Setup(t => t.GenerateRefreshToken())
            .Returns("refresh-token");

        _clock
            .Setup(c => c.UtcNow)
            .Returns(FixedNow);

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _tokenService
            .Setup(t => t.GenerateAccessToken(It.IsAny<Guid>(), "test@example.com", "Customer"))
            .Returns("access-token");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.Email.Should().Be("test@example.com");
        result.Value.FullName.Should().Be("Test User");
        result.Value.Role.Should().Be("Customer");

        _userRepository.Verify(r => r.Add(It.IsAny<User>()), Times.Once);
        _refreshTokenRepository.Verify(r => r.Add(It.IsAny<RefreshToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ReturnsFailure()
    {
        // Arrange
        var command = new RegisterCommand("existing@example.com", "P@ssw0rd!", "Test User");

        _userRepository
            .Setup(r => r.ExistsByEmailAsync("existing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.EmailAlreadyExists);

        _userRepository.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateEmailOnSave_ReturnsFailure()
    {
        // Arrange
        var command = new RegisterCommand("test@example.com", "P@ssw0rd!", "Test User");

        _userRepository
            .Setup(r => r.ExistsByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasher
            .Setup(h => h.HashPassword("P@ssw0rd!"))
            .Returns("hashed-password");

        _tokenService
            .Setup(t => t.GenerateRefreshToken())
            .Returns("refresh-token");

        _clock
            .Setup(c => c.UtcNow)
            .Returns(FixedNow);

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.EmailAlreadyExists);
    }
}
