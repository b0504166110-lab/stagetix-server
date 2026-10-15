using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StageTix.Core.DTOs.Auth;
using StageTix.Core.Entities;
using StageTix.Core.Enums;
using StageTix.Core.Exceptions;
using StageTix.Core.Interfaces.Repositories;
using StageTix.Core.Interfaces.Security;
using StageTix.Core.Models;
using StageTix.Service.Services;
using StageTix.Tests.TestHelpers;

namespace StageTix.Tests.Services;

public class AuthServiceTests
{
    private const string Password = "Passw0rd!";

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = UnitOfWorkMock.Create();
    private readonly Mock<IPasswordService> _passwords = new();
    private readonly Mock<IJwtTokenGenerator> _tokens = new();
    private readonly AuthService _sut;
    private readonly User _dana = TestData.UserOf(TestData.Dana, passwordHash: "hashed-password");

    public AuthServiceTests()
    {
        _sut = new AuthService(_users.Object, _unitOfWork.Object, _passwords.Object, _tokens.Object,
            TestMapper.Instance, new FixedTimeProvider(), NullLogger<AuthService>.Instance);

        _users.Setup(r => r.GetByEmailAsync(_dana.Email, It.IsAny<CancellationToken>())).ReturnsAsync(_dana);
        _users.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => id == _dana.Id ? _dana : null);
        _passwords.Setup(p => p.Verify("hashed-password", Password)).Returns(true);
        _passwords.Setup(p => p.Hash(It.IsAny<string>())).Returns((string pw) => $"hash-of-{pw}");
        _tokens.Setup(t => t.Generate(It.IsAny<User>()))
            .Returns(new AccessToken("signed.jwt.token", FixedTimeProvider.DefaultNow.AddHours(1)));
    }

    private Task<LoginResponse> Login(string email, string password) =>
        _sut.LoginAsync(new LoginRequest { Email = email, Password = password }, CancellationToken.None);

    // ---------- Login ----------

    [Fact]
    public async Task Valid_credentials_return_a_token_and_the_user()
    {
        var response = await Login(_dana.Email, Password);

        Assert.Equal("signed.jwt.token", response.Token);
        Assert.Equal(_dana.Id, response.User.Id);
        Assert.Equal(UserRole.Customer, response.User.Role);
    }

    [Fact]
    public async Task Email_is_matched_case_insensitively_and_trimmed()
    {
        var response = await Login($"  {_dana.Email.ToUpperInvariant()} ", Password);
        Assert.Equal(_dana.Id, response.User.Id);
    }

    [Fact]
    public async Task Wrong_password_is_rejected_without_issuing_a_token()
    {
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => Login(_dana.Email, "Wrong123!"));
        _tokens.Verify(t => t.Generate(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Unknown_email_gets_exactly_the_same_error_as_a_wrong_password()
    {
        var unknown = await Assert.ThrowsAsync<AuthenticationFailedException>(() => Login("nobody@example.com", Password));
        var wrongPassword = await Assert.ThrowsAsync<AuthenticationFailedException>(() => Login(_dana.Email, "Wrong123!"));

        Assert.Equal(wrongPassword.Message, unknown.Message);
    }

    // ---------- Registration ----------

    [Fact]
    public async Task Registration_creates_a_customer_with_a_hashed_password_and_normalized_email()
    {
        User? saved = null;
        _users.Setup(r => r.Add(It.IsAny<User>())).Callback<User>(u => saved = u);

        var response = await _sut.RegisterAsync(
            new RegisterRequest { FullName = " New User ", Email = " New@Example.COM ", Password = Password }, CancellationToken.None);

        Assert.NotNull(saved);
        Assert.Equal(UserRole.Customer, saved.Role);
        Assert.Equal("new@example.com", saved.Email);
        Assert.Equal("New User", saved.FullName);
        Assert.Equal($"hash-of-{Password}", saved.PasswordHash);
        Assert.NotEqual(Password, saved.PasswordHash);
        Assert.Equal(UserRole.Customer, response.Role);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Registering_an_existing_email_is_a_conflict()
    {
        _users.Setup(r => r.EmailExistsAsync(_dana.Email, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.RegisterAsync(
            new RegisterRequest { FullName = "Dana", Email = _dana.Email, Password = Password }, CancellationToken.None));

        _users.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }

    // ---------- Permissions by role ----------

    [Fact]
    public async Task A_customer_can_read_herself()
    {
        var user = await _sut.GetUserAsync(_dana.Id, TestData.Dana, CancellationToken.None);
        Assert.Equal(_dana.Id, user.Id);
    }

    [Fact]
    public async Task A_customer_cannot_read_another_user()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetUserAsync(_dana.Id, TestData.Noa, CancellationToken.None));
        _users.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task The_admin_can_read_any_user()
    {
        var user = await _sut.GetUserAsync(_dana.Id, TestData.Admin, CancellationToken.None);
        Assert.Equal(_dana.Id, user.Id);
    }
}
