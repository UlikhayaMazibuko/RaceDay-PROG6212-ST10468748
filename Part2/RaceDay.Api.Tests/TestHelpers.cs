using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Tests
{
    public static class TestHelpers
    {
        public static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

        public static RaceDayDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var db = new RaceDayDbContext(options);
            db.Database.EnsureCreated();
            return db;
        }

        public static T WithSession<T>(T controller, int? userId = null, string? role = null) where T : ControllerBase
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

            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            return controller;
        }

        public static User AddUser(RaceDayDbContext db, string role, string email, DateOnly? dateOfBirth = null)
        {
            var user = new User
            {
                RoleId = role == RoleNames.Organiser ? 1 : 2,
                FirstName = "Test",
                LastName = role,
                Email = email,
                PasswordHash = "not-a-real-hash",
                DateOfBirth = dateOfBirth ?? new DateOnly(1995, 1, 1),
                IsActive = true
            };

            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

       
        public static Event AddEvent(RaceDayDbContext db, int organiserId, DateOnly? eventDate = null, DateOnly? closeDate = null, string status = "Published")
        {
            var date = eventDate ?? Today.AddDays(30);

            var ev = new Event
            {
                OrganiserId = organiserId,
                DisciplineId = 1,
                EventName = "Test Road Race",
                EventDate = date,
                StartTime = new TimeOnly(6, 0),
                VenueName = "Test Venue",
                City = "Newcastle",
                Province = "KwaZulu-Natal",
                RegistrationCloseDate = closeDate ?? date.AddDays(-7),
                Status = status
            };

            db.Events.Add(ev);
            db.SaveChanges();
            return ev;
        }

        public static EventCategory AddCategory(RaceDayDbContext db, int eventId, int? maxParticipants = null, int minimumAge = 0, string name = "10 km Road Race")
        {
            var category = new EventCategory
            {
                EventId = eventId,
                CategoryName = name,
                DistanceKm = 10,
                EntryFee = 150,
                MaxParticipants = maxParticipants,
                MinimumAge = minimumAge
            };

            db.EventCategories.Add(category);
            db.SaveChanges();
            return category;
        }

        public static Enrolment AddEnrolment(RaceDayDbContext db, int categoryId, int participantId, string raceNumber)
        {
            var enrolment = new Enrolment
            {
                CategoryId = categoryId,
                ParticipantId = participantId,
                RaceNumber = raceNumber,
                EnrolmentStatus = "Pending"
            };

            db.Enrolments.Add(enrolment);
            db.SaveChanges();
            return enrolment;
        }
    }
}