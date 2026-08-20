using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Controllers;
using RaceDay.API.DTOs;
using RaceDay.API.Services;

namespace RaceDay.Tests;

public class AuthTests
{
    private readonly PasswordHasher _passwordHasher = new();

    [Fact]
    public async Task Register_ValidParticipant_ReturnsCreatedAtActionWithSessionToken()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var sessionService = new SessionService(context);
        var controller = new AuthController(context, _passwordHasher, sessionService);

        var dto = new RegisterRequestDto
        {
            FullName = "Jarred Super Runner",
            Email = "jarred.runner@raceday.co.za",
            Password = "Password123!",
            Role = "Participant",
            PhoneNumber = "+27821234567"
        };

        // Act
        var result = await controller.Register(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<AuthResponseDto>(createdResult.Value);
        Assert.Equal("Jarred Super Runner", response.FullName);
        Assert.Equal("jarred.runner@raceday.co.za", response.Email);
        Assert.Equal("Participant", response.Role);
        Assert.False(string.IsNullOrWhiteSpace(response.SessionToken));
    }

    [Fact]
    public async Task Register_ValidOrganiser_ReturnsCreatedAtActionWithOrganiserRole()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var sessionService = new SessionService(context);
        var controller = new AuthController(context, _passwordHasher, sessionService);

        var dto = new RegisterRequestDto
        {
            FullName = "Jarred Chief Organiser",
            Email = "jarred.org@raceday.co.za",
            Password = "SecurePassword123!",
            Role = "Organiser"
        };

        // Act
        var result = await controller.Register(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<AuthResponseDto>(createdResult.Value);
        Assert.Equal("Organiser", response.Role);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflictStatusCode409()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var sessionService = new SessionService(context);
        var controller = new AuthController(context, _passwordHasher, sessionService);

        var email = "duplicate.user@raceday.co.za";
        await controller.Register(new RegisterRequestDto
        {
            FullName = "First Athlete",
            Email = email,
            Password = "Password123!",
            Role = "Participant"
        });

        // Act: attempt duplicate registration with same email
        var duplicateResult = await controller.Register(new RegisterRequestDto
        {
            FullName = "Second Athlete",
            Email = email,
            Password = "Password123!",
            Role = "Participant"
        });

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(duplicateResult);
        Assert.Equal(409, conflictResult.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithSessionToken()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var sessionService = new SessionService(context);
        var controller = new AuthController(context, _passwordHasher, sessionService);

        var email = "login.test@raceday.co.za";
        var password = "CorrectPassword123!";
        await controller.Register(new RegisterRequestDto
        {
            FullName = "Login Athlete",
            Email = email,
            Password = password,
            Role = "Participant"
        });

        // Act
        var loginResult = await controller.Login(new LoginRequestDto
        {
            Email = email,
            Password = password
        });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(loginResult);
        var response = Assert.IsType<AuthResponseDto>(okResult.Value);
        Assert.Equal(email, response.Email);
        Assert.False(string.IsNullOrWhiteSpace(response.SessionToken));
    }

    [Fact]
    public async Task Login_IncorrectPassword_ReturnsUnauthorizedStatusCode401()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var sessionService = new SessionService(context);
        var controller = new AuthController(context, _passwordHasher, sessionService);

        var email = "auth.fail@raceday.co.za";
        await controller.Register(new RegisterRequestDto
        {
            FullName = "Test Athlete",
            Email = email,
            Password = "RealPassword123!",
            Role = "Participant"
        });

        // Act: try logging in with incorrect password
        var loginResult = await controller.Login(new LoginRequestDto
        {
            Email = email,
            Password = "WrongPassword999!"
        });

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(loginResult);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task Login_NonExistentEmail_ReturnsUnauthorizedStatusCode401()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var sessionService = new SessionService(context);
        var controller = new AuthController(context, _passwordHasher, sessionService);

        // Act
        var loginResult = await controller.Login(new LoginRequestDto
        {
            Email = "nobody@nowhere.com",
            Password = "Password123!"
        });

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(loginResult);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task Logout_ValidSession_InvalidatesSession()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var sessionService = new SessionService(context);
        var controller = new AuthController(context, _passwordHasher, sessionService);

        var regResult = await controller.Register(new RegisterRequestDto
        {
            FullName = "Logout User",
            Email = "logout@raceday.co.za",
            Password = "Password123!",
            Role = "Participant"
        });

        var created = Assert.IsType<CreatedAtActionResult>(regResult);
        var authResponse = Assert.IsType<AuthResponseDto>(created.Value);

        controller.ControllerContext = TestDatabaseFixture.CreateControllerContext(
            authResponse.UserId, "Participant", "Logout User", "logout@raceday.co.za", authResponse.SessionToken);

        // Act
        var logoutResult = await controller.Logout();

        // Assert
        Assert.IsType<OkObjectResult>(logoutResult);
        var validatedSession = await sessionService.ValidateSessionAsync(authResponse.SessionToken);
        Assert.Null(validatedSession); // Session must now be invalid!
    }

    [Fact]
    public async Task GetMe_UnauthenticatedCaller_ReturnsUnauthorized()
    {
        // Arrange
        using var context = TestDatabaseFixture.CreateInMemoryContext();
        var sessionService = new SessionService(context);
        var controller = new AuthController(context, _passwordHasher, sessionService);

        // No ControllerContext / claims set (unauthenticated)

        // Act
        var result = await controller.GetMe();

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorized.StatusCode);
    }
}
