using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDay.Api.Controllers;
using RaceDay.Api.Security;
using Xunit;

namespace RaceDay.Api.Tests
{
    public class RoleAccessTests
    {
       
        private static RequireRoleAttribute GetFilter(Type controllerType, string methodName)
        {
            var method = controllerType.GetMethod(methodName)!;

            return method.GetCustomAttribute<RequireRoleAttribute>()
                ?? controllerType.GetCustomAttribute<RequireRoleAttribute>()
                ?? throw new InvalidOperationException($"{controllerType.Name}.{methodName} has no [RequireRole] attribute.");
        }

        private static IActionResult? Run(RequireRoleAttribute filter, int? userId, string? role)
        {
            var httpContext = new DefaultHttpContext { Session = new TestSession() };

            if (userId.HasValue)
            {
                httpContext.Session.SetInt32(SessionKeys.UserId, userId.Value);
            }

            if (role is not null)
            {
                httpContext.Session.SetString(SessionKeys.RoleName, role);
            }

            var context = new AuthorizationFilterContext(
                new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>());

            filter.OnAuthorization(context);
            return context.Result;
        }

        private static void AssertForbidden(IActionResult? result)
        {
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        }

        [Fact]
        public void CreateEvent_WhenNotLoggedIn_IsRejectedWith401()
        {
            var filter = GetFilter(typeof(EventsController), nameof(EventsController.CreateEvent));

            var result = Run(filter, null, null);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void CreateEvent_AsParticipant_IsRejectedWith403()
        {
            var filter = GetFilter(typeof(EventsController), nameof(EventsController.CreateEvent));

            var result = Run(filter, 2, RoleNames.Participant);

            AssertForbidden(result);
        }

        [Fact]
        public void CreateEvent_AsOrganiser_IsAllowed()
        {
            var filter = GetFilter(typeof(EventsController), nameof(EventsController.CreateEvent));

            var result = Run(filter, 1, RoleNames.Organiser);

            Assert.Null(result);
        }

        [Fact]
        public void AddCategory_AsParticipant_IsRejectedWith403()
        {
            var filter = GetFilter(typeof(CategoriesController), nameof(CategoriesController.CreateCategory));

            AssertForbidden(Run(filter, 2, RoleNames.Participant));
        }

        [Fact]
        public void Enrol_AsOrganiser_IsRejectedWith403()
        {
            var filter = GetFilter(typeof(EnrolmentsController), nameof(EnrolmentsController.Enrol));

            AssertForbidden(Run(filter, 1, RoleNames.Organiser));
        }

        [Fact]
        public void Enrol_AsParticipant_IsAllowed()
        {
            var filter = GetFilter(typeof(EnrolmentsController), nameof(EnrolmentsController.Enrol));

            Assert.Null(Run(filter, 2, RoleNames.Participant));
        }

        [Fact]
        public void CaptureResult_AsParticipant_IsRejectedWith403()
        {
            var filter = GetFilter(typeof(ResultsController), nameof(ResultsController.CaptureResult));

            AssertForbidden(Run(filter, 2, RoleNames.Participant));
        }

        [Fact]
        public void ViewMyResults_AsOrganiser_IsRejectedWith403()
        {
            var filter = GetFilter(typeof(ResultsController), nameof(ResultsController.GetMyResults));

            AssertForbidden(Run(filter, 1, RoleNames.Organiser));
        }

        [Theory]
        [InlineData(RoleNames.Organiser)]
        [InlineData(RoleNames.Participant)]
        public void ViewOwnProfile_AsEitherRole_IsAllowed(string role)
        {
            var filter = GetFilter(typeof(UsersController), nameof(UsersController.GetMyProfile));

            Assert.Null(Run(filter, 1, role));
        }

        [Fact]
        public void ViewOwnProfile_WhenNotLoggedIn_IsRejectedWith401()
        {
            var filter = GetFilter(typeof(UsersController), nameof(UsersController.GetMyProfile));

            Assert.IsType<UnauthorizedObjectResult>(Run(filter, null, null));
        }
    }
}