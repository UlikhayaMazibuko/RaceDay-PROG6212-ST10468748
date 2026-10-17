using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Security;
using Xunit;

namespace RaceDay.Api.Tests
{
    public class EventsControllerTests
    {
        private static CreateEventRequest ValidEvent()
        {
            return new CreateEventRequest
            {
                DisciplineId = 1,
                EventName = "Newcastle Winter 10K",
                EventDate = TestHelpers.Today.AddDays(30),
                StartTime = new TimeOnly(6, 30),
                VenueName = "Arbor Park Sports Ground",
                City = "Newcastle",
                Province = "KwaZulu-Natal",
                RegistrationCloseDate = TestHelpers.Today.AddDays(23)
            };
        }

        private static UpdateEventRequest ValidUpdate(string newName)
        {
            return new UpdateEventRequest
            {
                DisciplineId = 1,
                EventName = newName,
                EventDate = TestHelpers.Today.AddDays(30),
                StartTime = new TimeOnly(7, 0),
                VenueName = "Arbor Park Sports Ground",
                City = "Newcastle",
                Province = "KwaZulu-Natal",
                RegistrationCloseDate = TestHelpers.Today.AddDays(23),
                Status = "Published"
            };
        }

        [Fact]
        public async Task CreateEvent_AsOrganiser_Returns201AndOwnerComesFromSession()
        {
            using var db = TestHelpers.CreateDb();
            var organiser = TestHelpers.AddUser(db, RoleNames.Organiser, "org@test.com");
            var controller = TestHelpers.WithSession(new EventsController(db), organiser.UserId, RoleNames.Organiser);

            var result = await controller.CreateEvent(ValidEvent());

            Assert.IsType<CreatedAtActionResult>(result);
            var saved = Assert.Single(db.Events);
            Assert.Equal(organiser.UserId, saved.OrganiserId);
            Assert.Equal("Published", saved.Status);
        }

        [Fact]
        public async Task CreateEvent_WithDateInThePast_Returns400()
        {
            using var db = TestHelpers.CreateDb();
            var organiser = TestHelpers.AddUser(db, RoleNames.Organiser, "org@test.com");
            var controller = TestHelpers.WithSession(new EventsController(db), organiser.UserId, RoleNames.Organiser);
            var request = ValidEvent();
            request.EventDate = TestHelpers.Today.AddDays(-1);
            request.RegistrationCloseDate = TestHelpers.Today.AddDays(-2);

            var result = await controller.CreateEvent(request);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Empty(db.Events);
        }

        [Fact]
        public async Task CreateEvent_WhenEntriesCloseAfterRaceDay_Returns400()
        {
            using var db = TestHelpers.CreateDb();
            var organiser = TestHelpers.AddUser(db, RoleNames.Organiser, "org@test.com");
            var controller = TestHelpers.WithSession(new EventsController(db), organiser.UserId, RoleNames.Organiser);
            var request = ValidEvent();
            request.RegistrationCloseDate = request.EventDate!.Value.AddDays(1);

            var result = await controller.CreateEvent(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateEvent_ByTheOrganiserWhoCreatedIt_Returns200AndSavesChanges()
        {
            using var db = TestHelpers.CreateDb();
            var organiser = TestHelpers.AddUser(db, RoleNames.Organiser, "org@test.com");
            var ev = TestHelpers.AddEvent(db, organiser.UserId);
            var controller = TestHelpers.WithSession(new EventsController(db), organiser.UserId, RoleNames.Organiser);

            var result = await controller.UpdateEvent(ev.EventId, ValidUpdate("Renamed Race"));

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Renamed Race", db.Events.Single().EventName);
        }

        [Fact]
        public async Task UpdateEvent_ByADifferentOrganiser_Returns403AndChangesNothing()
        {
            using var db = TestHelpers.CreateDb();
            var owner = TestHelpers.AddUser(db, RoleNames.Organiser, "owner@test.com");
            var otherOrganiser = TestHelpers.AddUser(db, RoleNames.Organiser, "other@test.com");
            var ev = TestHelpers.AddEvent(db, owner.UserId);
            var controller = TestHelpers.WithSession(new EventsController(db), otherOrganiser.UserId, RoleNames.Organiser);

            var result = await controller.UpdateEvent(ev.EventId, ValidUpdate("Hijacked Race"));

            var forbidden = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
            Assert.Equal("Test Road Race", db.Events.Single().EventName);
        }

        [Fact]
        public async Task UpdateEvent_ThatDoesNotExist_Returns404()
        {
            using var db = TestHelpers.CreateDb();
            var organiser = TestHelpers.AddUser(db, RoleNames.Organiser, "org@test.com");
            var controller = TestHelpers.WithSession(new EventsController(db), organiser.UserId, RoleNames.Organiser);

            var result = await controller.UpdateEvent(999, ValidUpdate("Anything"));

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteEvent_WithNoEnrolments_Returns204AndRemovesIt()
        {
            using var db = TestHelpers.CreateDb();
            var organiser = TestHelpers.AddUser(db, RoleNames.Organiser, "org@test.com");
            var ev = TestHelpers.AddEvent(db, organiser.UserId);
            var controller = TestHelpers.WithSession(new EventsController(db), organiser.UserId, RoleNames.Organiser);

            var result = await controller.DeleteEvent(ev.EventId);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(db.Events);
        }

        [Fact]
        public async Task DeleteEvent_WithEnrolments_Returns409AndKeepsTheEvent()
        {
            using var db = TestHelpers.CreateDb();
            var organiser = TestHelpers.AddUser(db, RoleNames.Organiser, "org@test.com");
            var participant = TestHelpers.AddUser(db, RoleNames.Participant, "runner@test.com");
            var ev = TestHelpers.AddEvent(db, organiser.UserId);
            var category = TestHelpers.AddCategory(db, ev.EventId);
            TestHelpers.AddEnrolment(db, category.CategoryId, participant.UserId, "C1-0001");
            var controller = TestHelpers.WithSession(new EventsController(db), organiser.UserId, RoleNames.Organiser);

            var result = await controller.DeleteEvent(ev.EventId);

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Single(db.Events);
        }

        [Fact]
        public async Task GetEvents_HidesDraftEventsFromThePublicList()
        {
            using var db = TestHelpers.CreateDb();
            var organiser = TestHelpers.AddUser(db, RoleNames.Organiser, "org@test.com");
            TestHelpers.AddEvent(db, organiser.UserId, status: "Published");
            TestHelpers.AddEvent(db, organiser.UserId, status: "Draft");
            var controller = TestHelpers.WithSession(new EventsController(db));

            var result = await controller.GetEvents(null, null, null);

            var list = Assert.IsAssignableFrom<IEnumerable<EventSummaryResponse>>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.Single(list);
        }
    }
}