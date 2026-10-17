using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Security;
using Xunit;

namespace RaceDay.Api.Tests
{

    public class EnrolmentsControllerTests
    {
        [Fact]
        public async Task Enrol_InOpenCategory_Returns201AndRecordsParticipantEventAndCategory()
        {
   
            using var db = TestHelpers.CreateDb();
            var organiser = TestHelpers.AddUser(db, RoleNames.Organiser, "org@test.com");
            var participant = TestHelpers.AddUser(db, RoleNames.Participant, "runner@test.com");
            var ev = TestHelpers.AddEvent(db, organiser.UserId);
            var category = TestHelpers.AddCategory(db, ev.EventId);
            var controller = TestHelpers.WithSession(new EnrolmentsController(db), participant.UserId, RoleNames.Participant);


            var result = await controller.Enrol(category.CategoryId);

            Assert.IsType<CreatedAtActionResult>(result);
            var enrolment = Assert.Single(db.Enrolments);
            Assert.Equal(participant.UserId, enrolment.ParticipantId);
            Assert.Equal(category.CategoryId, enrolment.CategoryId);
            Assert.Equal(ev.EventId, db.EventCategories.Single(c => c.CategoryId == enrolment.CategoryId).EventId);
            Assert.Equal("Pending", enrolment.EnrolmentStatus);
            Assert.Equal($"C{category.CategoryId}-0001", enrolment.RaceNumber);
        }

        [Fact]
        public async Task Enrol_InSameCategoryTwice_Returns409AndKeepsOneEnrolment()
        {
            using var db = TestHelpers.CreateDb();
            var organiser = TestHelpers.AddUser(db, RoleNames.Organiser, "org@test.com");
            var participant = TestHelpers.AddUser(db, RoleNames.Participant, "runner@test.com");
            var category = TestHelpers.AddCategory(db, TestHelpers.AddEvent(db, organiser.UserId).EventId);
            var controller = TestHelpers.WithSession(new EnrolmentsController(db), participant.UserId, RoleNames.Participant);
            await controller.Enrol(category.CategoryId);

            var result = await controller.Enrol(category.CategoryId);

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Single(db.Enrolments);
        }

        [Fact]
        public async Task Enrol_WhenCategoryIsFull_Returns409()
        {
            using var db = TestHelpers.CreateDb();
            var organiser = TestHelpers.AddUser(db, RoleNames.Organiser, "org@test.com");
            var firstRunner = TestHelpers.AddUser(db, RoleNames.Participant, "first@test.com");
            var secondRunner = TestHelpers.AddUser(db, RoleNames.Participant, "second@test.com");
            var category = TestHelpers.AddCategory(db, TestHelpers.AddEvent(db, organiser.UserId).EventId, maxParticipants: 1);
            TestHelpers.AddEnrolment(db, category.CategoryId, firstRunner.UserId, "C1-0001");
            var controller = TestHelpers.WithSession(new EnrolmentsController(db), secondRunner.UserId, RoleNames.Participant);

            var result = await controller.Enrol(category.CategoryId);

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Single(db.Enrolments);
        }

        [Fact]
        public async Task Enrol_AfterEntriesHaveClosed_Returns409()
        {
            using var db = TestHelpers.CreateDb();
            var organiser = TestHelpers.AddUser(db, RoleNames.Organiser, "org@test.com");
            var participant = TestHelpers.AddUser(db, RoleNames.Participant, "runner@test.com");
            var ev = TestHelpers.AddEvent(db, organiser.UserId,
                eventDate: TestHelpers.Today.AddDays(5),
                closeDate: TestHelpers.Today.AddDays(-1));
            var category = TestHelpers.AddCategory(db, ev.EventId);
            var controller = TestHelpers.WithSession(new EnrolmentsController(db), participant.UserId, RoleNames.Participant);

            var result = await controller.Enrol(category.CategoryId);

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Empty(db.Enrolments);
        }

        [Fact]
        public async Task Enrol_WhenUnderMinimumAge_Returns409()
        {
            using var db = TestHelpers.CreateDb();
            var organiser = TestHelpers.AddUser(db, RoleNames.Organiser, "org@test.com");
            var tenYearOld = TestHelpers.AddUser(db, RoleNames.Participant, "young@test.com", TestHelpers.Today.AddYears(-10));
            var category = TestHelpers.AddCategory(db, TestHelpers.AddEvent(db, organiser.UserId).EventId, minimumAge: 16);
            var controller = TestHelpers.WithSession(new EnrolmentsController(db), tenYearOld.UserId, RoleNames.Participant);

            var result = await controller.Enrol(category.CategoryId);

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Empty(db.Enrolments);
        }

        [Fact]
        public async Task Enrol_InCategoryThatDoesNotExist_Returns404()
        {
            using var db = TestHelpers.CreateDb();
            var participant = TestHelpers.AddUser(db, RoleNames.Participant, "runner@test.com");
            var controller = TestHelpers.WithSession(new EnrolmentsController(db), participant.UserId, RoleNames.Participant);

            var result = await controller.Enrol(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetMyEnrolments_ReturnsOnlyTheLoggedInParticipantsEntries()
        {
            using var db = TestHelpers.CreateDb();
            var organiser = TestHelpers.AddUser(db, RoleNames.Organiser, "org@test.com");
            var me = TestHelpers.AddUser(db, RoleNames.Participant, "me@test.com");
            var someoneElse = TestHelpers.AddUser(db, RoleNames.Participant, "else@test.com");
            var category = TestHelpers.AddCategory(db, TestHelpers.AddEvent(db, organiser.UserId).EventId);
            TestHelpers.AddEnrolment(db, category.CategoryId, me.UserId, "C1-0001");
            TestHelpers.AddEnrolment(db, category.CategoryId, someoneElse.UserId, "C1-0002");
            var controller = TestHelpers.WithSession(new EnrolmentsController(db), me.UserId, RoleNames.Participant);

            var result = await controller.GetMyEnrolments();

            var list = Assert.IsAssignableFrom<IEnumerable<EnrolmentResponse>>(Assert.IsType<OkObjectResult>(result).Value);
            var only = Assert.Single(list);
            Assert.Equal(me.UserId, only.ParticipantId);
        }

        [Fact]
        public async Task GetEnrolmentsForEvent_ByOrganiserWhoDoesNotOwnIt_Returns403()
        {
            using var db = TestHelpers.CreateDb();
            var owner = TestHelpers.AddUser(db, RoleNames.Organiser, "owner@test.com");
            var otherOrganiser = TestHelpers.AddUser(db, RoleNames.Organiser, "other@test.com");
            var ev = TestHelpers.AddEvent(db, owner.UserId);
            var controller = TestHelpers.WithSession(new EnrolmentsController(db), otherOrganiser.UserId, RoleNames.Organiser);

            var result = await controller.GetEnrolmentsForEvent(ev.EventId);

            var forbidden = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        }
    }
}