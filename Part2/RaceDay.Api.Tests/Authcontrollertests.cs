using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
using Xunit;

namespace RaceDay.Api.Tests
{
    public class AuthControllerTests
    {
        private static RegisterRequest ValidRegistration(string email = "thandeka@test.com", int roleId = 1)
        {
            return new RegisterRequest
            {
                FirstName = "Thandeka",
                LastName = "Mokoena",
                Email = email,
                Password = "Organiser123",
                DateOfBirth = new DateOnly(1985, 3, 14),
                RoleId = roleId
            };
        }

        [Fact]
        public async Task Register_WithValidDetails_Returns201AndStoresHashedPassword()
        {
            using var db = TestHelpers.CreateDb();
            var controller = TestHelpers.WithSession(new AuthController(db, new PasswordHasher<User>()));

            var result = await controller.Register(ValidRegistration());

            var created = Assert.IsType<CreatedResult>(result);
            var body = Assert.IsType<AuthResponse>(created.Value);
            Assert.Equal("Organiser", body.RoleName);

            var saved = db.Users.Single(u => u.Email == "thandeka@test.com");
            Assert.NotEqual("Organiser123", saved.PasswordHash);
            Assert.Equal(PasswordVerificationResult.Success,
                new PasswordHasher<User>().VerifyHashedPassword(saved, saved.PasswordHash, "Organiser123"));
        }

        [Fact]
        public async Task Register_AsParticipant_SavesParticipantRole()
        {
            using var db = TestHelpers.CreateDb();
            var controller = TestHelpers.WithSession(new AuthController(db, new PasswordHasher<User>()));

            var result = await controller.Register(ValidRegistration("sipho@test.com", roleId: 2));

            var body = Assert.IsType<AuthResponse>(Assert.IsType<CreatedResult>(result).Value);
            Assert.Equal("Participant", body.RoleName);
        }

        [Fact]
        public async Task Register_WithEmailAlreadyUsed_Returns409()
        {
            using var db = TestHelpers.CreateDb();
            var controller = TestHelpers.WithSession(new AuthController(db, new PasswordHasher<User>()));
            await controller.Register(ValidRegistration());

            var result = await controller.Register(ValidRegistration("THANDEKA@test.com"));

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(1, db.Users.Count());
        }

        [Fact]
        public async Task Register_WithRoleThatDoesNotExist_Returns400()
        {
            using var db = TestHelpers.CreateDb();
            var controller = TestHelpers.WithSession(new AuthController(db, new PasswordHasher<User>()));

            var result = await controller.Register(ValidRegistration(roleId: 99));

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Empty(db.Users);
        }

        [Fact]
        public async Task Login_WithCorrectPassword_Returns200AndStoresUserIdAndRoleInSession()
        {
            using var db = TestHelpers.CreateDb();
            var hasher = new PasswordHasher<User>();
            await TestHelpers.WithSession(new AuthController(db, hasher)).Register(ValidRegistration());
            var controller = TestHelpers.WithSession(new AuthController(db, hasher));

            var result = await controller.Login(new LoginRequest { Email = "thandeka@test.com", Password = "Organiser123" });

            Assert.IsType<OkObjectResult>(result);
            var session = controller.HttpContext.Session;
            Assert.Equal(db.Users.Single().UserId, session.GetInt32(SessionKeys.UserId));
            Assert.Equal(RoleNames.Organiser, session.GetString(SessionKeys.RoleName));
        }

        [Fact]
        public async Task Login_WithWrongPassword_Returns401AndCreatesNoSession()
        {
            using var db = TestHelpers.CreateDb();
            var hasher = new PasswordHasher<User>();
            await TestHelpers.WithSession(new AuthController(db, hasher)).Register(ValidRegistration());
            var controller = TestHelpers.WithSession(new AuthController(db, hasher));

            var result = await controller.Login(new LoginRequest { Email = "thandeka@test.com", Password = "WrongPassword1" });

            Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
        }

        [Fact]
        public async Task Login_WithUnknownEmail_Returns401()
        {
            using var db = TestHelpers.CreateDb();
            var controller = TestHelpers.WithSession(new AuthController(db, new PasswordHasher<User>()));

            var result = await controller.Login(new LoginRequest { Email = "nobody@test.com", Password = "Whatever123" });

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void Logout_WhenLoggedIn_Returns204AndClearsSession()
        {
            using var db = TestHelpers.CreateDb();
            var controller = TestHelpers.WithSession(new AuthController(db, new PasswordHasher<User>()), 1, RoleNames.Organiser);

            var result = controller.Logout();

            Assert.IsType<NoContentResult>(result);
            Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
            Assert.Null(controller.HttpContext.Session.GetString(SessionKeys.RoleName));
        }
    }
}