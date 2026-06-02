using Xunit;
using Moq;
using FluentAssertions;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using AutoMapper;
using Application.DTOS;
using Application.Interface;
using Application.Services;
using AuthMicroservice.Application.Dtos;
using AuthMicroservice.Domain.Entities;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _mapperMock = new Mock<IMapper>();
        _configMock = new Mock<IConfiguration>();

        _configMock.Setup(x => x["Jwt:Key"]).Returns("AssetNode-2024-Super-Secret-JWT-Key-With-Special-Characters-@#$%^&*123456789");
        _configMock.Setup(x => x["Jwt:Issuer"]).Returns("test_issuer");
        _configMock.Setup(x => x["Jwt:Audience"]).Returns("test_audience");

        _service = new UserService(
            _userRepoMock.Object,
            _mapperMock.Object,
            _configMock.Object
        );
    }

    // ---------------- GET BY ID ----------------

    [Fact]
    public async Task GetByIdAsync_ShouldThrow_WhenUserNotFound()
    {
        _userRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync((User?)null);

        var act = async () => await _service.GetByIdAsync(1);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUserDto()
    {
        var user = new User {
            Username = "testuser",
            Email = "test@test.com",
            PasswordHash = "hash",
            UserId = 1,
            Role="admin"

        };
        var dto = new UserDto();

        _userRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(user);
        _mapperMock.Setup(x => x.Map<UserDto>(user)).Returns(dto);

        var result = await _service.GetByIdAsync(1);

        result.Should().Be(dto);
    }

    // ---------------- CREATE ----------------

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenUsernameMissing()
    {
        var dto = new CreateUserDto
        { 
            Email = "a@test.com",
            Password = "123"
        };

        var act = async () => await _service.CreateAsync(dto);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenUsernameExists()
    {
        var dto = new CreateUserDto
        {
            Username = "test",
            Email = "a@test.com",
            Password = "123"
        };

        _userRepoMock.Setup(x => x.UsernameExistsAsync(dto.Username,null))
                     .ReturnsAsync(true);

        var act = async () => await _service.CreateAsync(dto);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenEmailExists()
    {
        var dto = new CreateUserDto
        {
            Username = "test",
            Email = "a@test.com",
            Password = "123"
        };

        _userRepoMock.Setup(x => x.EmailExistsAsync(dto.Email, null))
                   .ReturnsAsync(true);

        var act = async () => await _service.CreateAsync(dto);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateUser()
    {
        var dto = new CreateUserDto
        {
            Username = "test",
            Email = "a@test.com",
            Password = "123"
        };

        var user = new User
        {
            Username = "testuser",
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = "admin"

        };
        var userDto = new UserDto();

        _userRepoMock.Setup(x => x.UsernameExistsAsync(dto.Username,null)).ReturnsAsync(false);
        _userRepoMock.Setup(x => x.EmailExistsAsync(dto.Email,null)).ReturnsAsync(false);
        _mapperMock.Setup(x => x.Map<User>(dto)).Returns(user);
        _mapperMock.Setup(x => x.Map<UserDto>(user)).Returns(userDto);

        var result = await _service.CreateAsync(dto);

        result.Should().Be(userDto);
        _userRepoMock.Verify(x => x.AddAsync(user), Times.Once);
    }

    // ---------------- LOGIN ----------------

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenInvalidCredentials()
    {
        var dto = new LoginDto { Email = "test@test.com", Password = "wrong" };

        _userRepoMock.Setup(x => x.GetByEmailAsync(dto.Email))
                     .ReturnsAsync((User?)null);

        var act = async () => await _service.LoginAsync(dto);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTokens()
    {
        var password = "123";
        var hashed = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Email = "test@test.com",
            PasswordHash = hashed,
            Username = "test",
            Role = "User",
            UserId = 1
        };

        var dto = new LoginDto { Email = user.Email, Password = password };

        _userRepoMock.Setup(x => x.GetByEmailAsync(dto.Email))
                     .ReturnsAsync(user);

        var result = await _service.LoginAsync(dto);

        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    // ---------------- LOGOUT ----------------

    [Fact]
    public async Task LogoutAsync_ShouldClearRefreshToken()
    {
        
        var user = new User
        {
            Username = "testuser",
            Email = "test@test.com",
            PasswordHash = "hash",
            RefreshToken = "abc",
            Role = "admin"

        };

        _userRepoMock.Setup(x => x.GetByRefreshTokenAsync("abc"))
                     .ReturnsAsync(user);

        await _service.LogoutAsync("abc");

        user.RefreshToken.Should().BeNull();
        _userRepoMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    // ---------------- GET CURRENT USER ----------------

    [Fact]
    public async Task GetCurrentUserAsync_ShouldThrow_WhenNotAuthenticated()
    {
        var identity = new ClaimsIdentity();

        var act = async () => await _service.GetCurrentUserAsync(identity);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnUser()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, "test@test.com")
        }, "mock");

        var user = new User
        {
            Username = "testuser",
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = "admin"

        };
        var dto = new UserDto();

        _userRepoMock.Setup(x => x.GetByEmailAsync("test@test.com"))
                     .ReturnsAsync(user);

        _mapperMock.Setup(x => x.Map<UserDto>(user)).Returns(dto);

        var result = await _service.GetCurrentUserAsync(identity);

        result.Should().Be(dto);
    }
}