/*
   RaceDay Event Management System
   Part 1, Section C

   Module:  Programming 2B (PROG6212)
   Student: Ulikhaya A. Mazibuko (ST10468748).
*/


/*
   We'll start by creating the database. This script will only create the database 
   if it is not already there, so that means that running this on a machine that
   already has RaceDayDb does not fail or wipe the whole database. The tables
   themselves are rebuilt further down.
 */

IF DB_ID('RaceDayDb') IS NULL
BEGIN
    CREATE DATABASE RaceDayDb;
END;
GO

USE RaceDayDb;
GO


/*
   Order matters here. A table cannot be dropped while another table still holds
   a foreign key pointing at it, so this works from the children back up to the
   parents. Results depends on Enrolments, Enrolments depends on EventCategories
   and Users and so on up to the two lookup tables.
*/

DROP TABLE IF EXISTS dbo.Results;
DROP TABLE IF EXISTS dbo.Enrolments;
DROP TABLE IF EXISTS dbo.EventCategories;
DROP TABLE IF EXISTS dbo.Events;
DROP TABLE IF EXISTS dbo.Users;
DROP TABLE IF EXISTS dbo.Disciplines;
DROP TABLE IF EXISTS dbo.Roles;
GO


/*
   Roles, a small lookup table holding the two roles the system supports. This keeps the
   roles in their own table rather than as a text column on Users means a role
   name can never be misspelt into existence and the API can rely on the id
   staying stable even if the display name changes later.
 */

CREATE TABLE dbo.Roles (
    RoleId      INT             IDENTITY(1, 1)  NOT NULL,
    RoleName    NVARCHAR(20)                    NOT NULL,
    Description NVARCHAR(100)                       NULL,

    CONSTRAINT PK_Roles        PRIMARY KEY (RoleId),
    CONSTRAINT UQ_Roles_Name   UNIQUE      (RoleName)
);
GO


/*
   These are the three event types the platform covers. This exists so that the event type
   is chosen from a fixed list rather than typed in freely, which keeps the data
   consistent and gives the Part 3 dropdown something to bind to.
*/

CREATE TABLE dbo.Disciplines (
    DisciplineId    INT             IDENTITY(1, 1)  NOT NULL,
    DisciplineName  NVARCHAR(30)                    NOT NULL,

    CONSTRAINT PK_Disciplines      PRIMARY KEY (DisciplineId),
    CONSTRAINT UQ_Disciplines_Name UNIQUE      (DisciplineName)
);
GO


/*
   This is for users, both organisers and participants live in this one table and are told apart by
   RoleId. Splitting them into two tables would mean duplicating every login and
   contact column, and would make the Results table awkward because it has to
   point at an organiser while Enrolments points at a participant.
*/

CREATE TABLE dbo.Users (
    UserId          INT             IDENTITY(1, 1)  NOT NULL,
    RoleId          INT                             NOT NULL,
    FirstName       NVARCHAR(50)                    NOT NULL,
    LastName        NVARCHAR(50)                    NOT NULL,
    Email           NVARCHAR(150)                   NOT NULL,
    PasswordHash    NVARCHAR(255)                   NOT NULL,
    PhoneNumber     NVARCHAR(20)                        NULL,
    DateOfBirth     DATE                            NOT NULL,
    Gender          NVARCHAR(10)                        NULL,
    City            NVARCHAR(60)                        NULL,
    CreatedAt       DATETIME2(0)                    NOT NULL    DEFAULT SYSUTCDATETIME(),
    IsActive        BIT                             NOT NULL    DEFAULT 1,

    CONSTRAINT PK_Users          PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email    UNIQUE      (Email),
    CONSTRAINT FK_Users_Roles    FOREIGN KEY (RoleId)
        REFERENCES dbo.Roles (RoleId),
    CONSTRAINT CK_Users_Dob      CHECK       (DateOfBirth < CAST(GETDATE() AS DATE))
);
GO


/*
   Events, owned by the organiser who created it, which is what OrganiserId records. The
   API takes that value from the session rather than the request body so a user
   cannot create an event in somebody else's name.
*/

CREATE TABLE dbo.Events (
    EventId                 INT             IDENTITY(1, 1)  NOT NULL,
    OrganiserId             INT                             NOT NULL,
    DisciplineId            INT                             NOT NULL,
    EventName               NVARCHAR(120)                   NOT NULL,
    Description             NVARCHAR(1000)                      NULL,
    EventDate               DATE                            NOT NULL,
    StartTime               TIME(0)                         NOT NULL,
    VenueName               NVARCHAR(150)                   NOT NULL,
    City                    NVARCHAR(60)                    NOT NULL,
    Province                NVARCHAR(50)                    NOT NULL,
    Latitude                DECIMAL(9, 6)                       NULL,
    Longitude               DECIMAL(9, 6)                       NULL,
    RegistrationCloseDate   DATE                            NOT NULL,
    Status                  NVARCHAR(20)                    NOT NULL    DEFAULT 'Draft',
    CreatedAt               DATETIME2(0)                    NOT NULL    DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Events                PRIMARY KEY (EventId),
    CONSTRAINT FK_Events_Organiser      FOREIGN KEY (OrganiserId)
        REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Events_Discipline     FOREIGN KEY (DisciplineId)
        REFERENCES dbo.Disciplines (DisciplineId),
    CONSTRAINT CK_Events_Status         CHECK       (Status IN ('Draft', 'Published', 'Completed', 'Cancelled')),
    CONSTRAINT CK_Events_CloseDate      CHECK       (RegistrationCloseDate <= EventDate)
);
GO


/*
   The distances or age groups on offer within one event, for example the 10 km
   and the 21.1 km at the same race. Categories belong to an event rather than
   being shared across all events, because the same distance has a different
   entry fee, start time and field limit at every race.
*/

CREATE TABLE dbo.EventCategories (
    CategoryId          INT             IDENTITY(1, 1)  NOT NULL,
    EventId             INT                             NOT NULL,
    CategoryName        NVARCHAR(60)                    NOT NULL,
    DistanceKm          DECIMAL(5, 2)                   NOT NULL,
    EntryFee            DECIMAL(8, 2)                   NOT NULL    DEFAULT 0,
    CategoryStartTime   TIME(0)                             NULL,
    MaxParticipants     INT                                 NULL,
    MinimumAge          INT                             NOT NULL    DEFAULT 0,

    CONSTRAINT PK_EventCategories           PRIMARY KEY (CategoryId),
    CONSTRAINT UQ_EventCategories_Name      UNIQUE      (EventId, CategoryName),
    CONSTRAINT FK_EventCategories_Event     FOREIGN KEY (EventId)
        REFERENCES dbo.Events (EventId) ON DELETE CASCADE,
    CONSTRAINT CK_EventCategories_Distance  CHECK       (DistanceKm > 0),
    CONSTRAINT CK_EventCategories_Fee       CHECK       (EntryFee >= 0),
    CONSTRAINT CK_EventCategories_Age       CHECK       (MinimumAge >= 0),
    CONSTRAINT CK_EventCategories_Max       CHECK       (MaxParticipants IS NULL OR MaxParticipants > 0)
);
GO


/*
   This is the table that resolves the many to many relationship between users
   and categories. One participant can enter many categories and one category
   holds many participants, and neither of those facts can be stored without a
   junction table sitting between them.
*/

CREATE TABLE dbo.Enrolments (
    EnrolmentId     INT             IDENTITY(1, 1)  NOT NULL,
    CategoryId      INT                             NOT NULL,
    ParticipantId   INT                             NOT NULL,
    RaceNumber      NVARCHAR(10)                    NOT NULL,
    EnrolmentDate   DATETIME2(0)                    NOT NULL    DEFAULT SYSUTCDATETIME(),
    EnrolmentStatus NVARCHAR(20)                    NOT NULL    DEFAULT 'Pending',

    CONSTRAINT PK_Enrolments                PRIMARY KEY (EnrolmentId),
    CONSTRAINT UQ_Enrolments_Participant    UNIQUE      (CategoryId, ParticipantId),
    CONSTRAINT UQ_Enrolments_RaceNumber     UNIQUE      (CategoryId, RaceNumber),
    CONSTRAINT FK_Enrolments_Category       FOREIGN KEY (CategoryId)
        REFERENCES dbo.EventCategories (CategoryId),
    CONSTRAINT FK_Enrolments_Participant    FOREIGN KEY (ParticipantId)
        REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Enrolments_Status         CHECK       (EnrolmentStatus IN ('Pending', 'Confirmed', 'Cancelled'))
);
GO


/* 
   This is captured by the organiser after the race. It hangs off Enrolments rather than
   off Users and Events separately, because the enrolment row already identifies
   who ran, which event it was and which category they were in.
*/

CREATE TABLE dbo.Results (
    ResultId            INT             IDENTITY(1, 1)  NOT NULL,
    EnrolmentId         INT                             NOT NULL,
    CapturedByUserId    INT                             NOT NULL,
    FinishTime          TIME(0)                             NULL,
    PositionOverall     INT                                 NULL,
    PositionInCategory  INT                                 NULL,
    ResultStatus        NVARCHAR(15)                    NOT NULL    DEFAULT 'Finished',
    CapturedAt          DATETIME2(0)                    NOT NULL    DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Results               PRIMARY KEY (ResultId),
    CONSTRAINT UQ_Results_Enrolment     UNIQUE      (EnrolmentId),
    CONSTRAINT FK_Results_Enrolment     FOREIGN KEY (EnrolmentId)
        REFERENCES dbo.Enrolments (EnrolmentId),
    CONSTRAINT FK_Results_CapturedBy    FOREIGN KEY (CapturedByUserId)
        REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Results_Status        CHECK       (ResultStatus IN ('Finished', 'DNF', 'DNS', 'DQ')),
    CONSTRAINT CK_Results_Overall       CHECK       (PositionOverall IS NULL OR PositionOverall > 0),
    CONSTRAINT CK_Results_Category      CHECK       (PositionInCategory IS NULL OR PositionInCategory > 0)
);
GO


/*
   --------------------------------------------------------------------------------------
   Everything below fills the database with realistic sample data so the API in
   Part 2 has something to work against from the first run.
   --------------------------------------------------------------------------------------
*/


/*
  Roles and disciplines
*/

INSERT INTO dbo.Roles (RoleName, Description)
VALUES
    ('Organiser',   'Creates and manages events, categories, enrolments and results.'),
    ('Participant', 'Browses events, enters events and tracks personal results.');

INSERT INTO dbo.Disciplines (DisciplineName)
VALUES
    ('Running'),
    ('Walking'),
    ('Cycling');
GO


/*
   11. Users including two organisers and four participants. The brief asks for a minimum of two of
   each and the extra participants make the enrolment and result data below
   look more like a real start list.
*/

INSERT INTO dbo.Users
    (RoleId, FirstName, LastName, Email, PasswordHash, PhoneNumber, DateOfBirth, Gender, City)
VALUES
    ((SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Organiser'),
     'Thandeka', 'Mokoena', 'thandeka.mokoena@racedaysa.co.za',
     'AQAAAAIAAYagAAAAEL9xK2mVqTt7pJd0sN4hRb1cXwYzF3vQ8kGmDpLr5aTnUj2WoBxSyE6HcMfZgA==',
     '0821234567', '1985-03-14', 'Female', 'Johannesburg'),

    ((SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Organiser'),
     'Riaan', 'van der Merwe', 'riaan.vdm@capeeventsco.co.za',
     'AQAAAAIAAYagAAAAEK3nHtY7wZqB2sVcXdRj9LmPfKgT4uNaEyWoQi8ZbCxSrDvH5MtJl0GkFpAyBw==',
     '0837654321', '1979-11-02', 'Male', 'Cape Town'),

    ((SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Participant'),
     'Sipho', 'Ndlovu', 'sipho.ndlovu@gmail.com',
     'AQAAAAIAAYagAAAAEJ7bR2xNvQmT8kWfYcHdZs4LpGtAe1UoiXBrJyM3nKqVzD6SgClF9WhPtEaXjQ==',
     '0713456789', '1996-07-21', 'Male', 'Soweto'),

    ((SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Participant'),
     'Aisha', 'Patel', 'aisha.patel@outlook.com',
     'AQAAAAIAAYagAAAAEM2vTqW8yHnBz5JdXsRkPfLcGtA7eUoiNbWrQyJ3mKxVzD1SgClF6WhPtEaYjR==',
     '0824567890', '2001-01-09', 'Female', 'Durban'),

    ((SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Participant'),
     'Lerato', 'Dlamini', 'lerato.dlamini@gmail.com',
     'AQAAAAIAAYagAAAAEN8cS3yOwRnU9lXgZdTmQgMdHuB2fVpjOcXsRzK4nLyWaE7ThDmG0XiQuFbZkS==',
     '0765678901', '1992-09-30', 'Female', 'Pretoria'),

    ((SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Participant'),
     'Johan', 'Botha', 'johan.botha@webmail.co.za',
     'AQAAAAIAAYagAAAAEP9dT4zPxSoV0mYhAeUnRhNeIvC3gWqkPdYtSaL5oMzXbF8UiEnH1YjRvGcAlT==',
     '0736789012', '1974-05-17', 'Male', 'Cape Town');
GO


/*
   Three events across the three disciplines. The first one has already taken
   place, which is deliberate, because results can only sensibly be captured for
   an event that has been run. The other two are still open for entries and give
   the Part 3 browse and enter screens something upcoming to display.
*/

INSERT INTO dbo.Events
    (OrganiserId, DisciplineId, EventName, Description, EventDate, StartTime,
     VenueName, City, Province, Latitude, Longitude, RegistrationCloseDate, Status)
VALUES
    ((SELECT UserId FROM dbo.Users WHERE Email = 'thandeka.mokoena@racedaysa.co.za'),
     (SELECT DisciplineId FROM dbo.Disciplines WHERE DisciplineName = 'Running'),
     'Johannesburg City Half Marathon',
     'A fast city route starting and finishing at Marks Park, taking runners through Emmarentia and the Westdene Dam before the final climb back into Judith Paarl.',
     '2026-05-24', '06:00:00',
     'Marks Park Sports Club', 'Johannesburg', 'Gauteng',
     -26.156300, 28.008900, '2026-05-17', 'Completed'),

    ((SELECT UserId FROM dbo.Users WHERE Email = 'riaan.vdm@capeeventsco.co.za'),
     (SELECT DisciplineId FROM dbo.Disciplines WHERE DisciplineName = 'Cycling'),
     'Cape Peninsula Cycle Challenge',
     'A coastal ride from Green Point along Chapman''s Peak Drive and back through Constantia Nek. Timed on chip, with a shorter family route for first time riders.',
     '2026-10-11', '06:30:00',
     'Green Point Athletics Stadium', 'Cape Town', 'Western Cape',
     -33.903700, 18.410400, '2026-10-01', 'Published'),

    ((SELECT UserId FROM dbo.Users WHERE Email = 'thandeka.mokoena@racedaysa.co.za'),
     (SELECT DisciplineId FROM dbo.Disciplines WHERE DisciplineName = 'Walking'),
     'Durban Beachfront Charity Walk',
     'A flat, family friendly walk along the Golden Mile promenade in aid of local youth sport development. Entries include a finisher medal and a refreshment voucher.',
     '2026-11-15', '07:00:00',
     'uShaka Marine World', 'Durban', 'KwaZulu-Natal',
     -29.867200, 31.044600, '2026-11-08', 'Published');
GO


/*
   Every event gets its own set of categories, as the brief requires. Note that
   both running events offer a 10 km, which is exactly the situation the unique
   constraint on (EventId, CategoryName) is designed to allow: duplicate names
   across different events are fine, duplicates within one event are not.
*/

INSERT INTO dbo.EventCategories
    (EventId, CategoryName, DistanceKm, EntryFee, CategoryStartTime, MaxParticipants, MinimumAge)
VALUES
    -- Johannesburg City Half Marathon
    ((SELECT EventId FROM dbo.Events WHERE EventName = 'Johannesburg City Half Marathon'),
     '21.1 km Half Marathon', 21.10, 250.00, '06:00:00', 2500, 16),
    ((SELECT EventId FROM dbo.Events WHERE EventName = 'Johannesburg City Half Marathon'),
     '10 km Road Race', 10.00, 150.00, '06:30:00', 1500, 12),
    ((SELECT EventId FROM dbo.Events WHERE EventName = 'Johannesburg City Half Marathon'),
     '5 km Fun Run', 5.00, 80.00, '07:00:00', 1000, 0),

    -- Cape Peninsula Cycle Challenge
    ((SELECT EventId FROM dbo.Events WHERE EventName = 'Cape Peninsula Cycle Challenge'),
     '109 km Full Peninsula', 109.00, 620.00, '06:30:00', 3000, 18),
    ((SELECT EventId FROM dbo.Events WHERE EventName = 'Cape Peninsula Cycle Challenge'),
     '45 km Coastal Route', 45.00, 380.00, '07:30:00', 1200, 14),

    -- Durban Beachfront Charity Walk
    ((SELECT EventId FROM dbo.Events WHERE EventName = 'Durban Beachfront Charity Walk'),
     '8 km Promenade Walk', 8.00, 100.00, '07:00:00', 800, 0),
    ((SELECT EventId FROM dbo.Events WHERE EventName = 'Durban Beachfront Charity Walk'),
     '3 km Family Walk', 3.00, 50.00, '07:30:00', 600, 0);
GO


/*
   A spread of entries across all three events and several statuses, so the
   colour coded status display required in Part 3 has real data to render.
*/

INSERT INTO dbo.Enrolments (CategoryId, ParticipantId, RaceNumber, EnrolmentDate, EnrolmentStatus)
VALUES
    -- Johannesburg City Half Marathon, already run
    ((SELECT c.CategoryId FROM dbo.EventCategories AS c
        INNER JOIN dbo.Events AS e ON e.EventId = c.EventId
        WHERE e.EventName = 'Johannesburg City Half Marathon' AND c.CategoryName = '21.1 km Half Marathon'),
     (SELECT UserId FROM dbo.Users WHERE Email = 'sipho.ndlovu@gmail.com'),
     'JHM1042', '2026-04-08T09:14:00', 'Confirmed'),

    ((SELECT c.CategoryId FROM dbo.EventCategories AS c
        INNER JOIN dbo.Events AS e ON e.EventId = c.EventId
        WHERE e.EventName = 'Johannesburg City Half Marathon' AND c.CategoryName = '21.1 km Half Marathon'),
     (SELECT UserId FROM dbo.Users WHERE Email = 'lerato.dlamini@gmail.com'),
     'JHM1087', '2026-04-11T17:32:00', 'Confirmed'),

    ((SELECT c.CategoryId FROM dbo.EventCategories AS c
        INNER JOIN dbo.Events AS e ON e.EventId = c.EventId
        WHERE e.EventName = 'Johannesburg City Half Marathon' AND c.CategoryName = '10 km Road Race'),
     (SELECT UserId FROM dbo.Users WHERE Email = 'aisha.patel@outlook.com'),
     'JHM2210', '2026-04-19T11:05:00', 'Confirmed'),

    ((SELECT c.CategoryId FROM dbo.EventCategories AS c
        INNER JOIN dbo.Events AS e ON e.EventId = c.EventId
        WHERE e.EventName = 'Johannesburg City Half Marathon' AND c.CategoryName = '5 km Fun Run'),
     (SELECT UserId FROM dbo.Users WHERE Email = 'johan.botha@webmail.co.za'),
     'JHM3018', '2026-05-02T08:47:00', 'Confirmed'),

    -- Cape Peninsula Cycle Challenge, entries still open
    ((SELECT c.CategoryId FROM dbo.EventCategories AS c
        INNER JOIN dbo.Events AS e ON e.EventId = c.EventId
        WHERE e.EventName = 'Cape Peninsula Cycle Challenge' AND c.CategoryName = '109 km Full Peninsula'),
     (SELECT UserId FROM dbo.Users WHERE Email = 'johan.botha@webmail.co.za'),
     'CPC0451', '2026-08-15T13:20:00', 'Confirmed'),

    ((SELECT c.CategoryId FROM dbo.EventCategories AS c
        INNER JOIN dbo.Events AS e ON e.EventId = c.EventId
        WHERE e.EventName = 'Cape Peninsula Cycle Challenge' AND c.CategoryName = '45 km Coastal Route'),
     (SELECT UserId FROM dbo.Users WHERE Email = 'sipho.ndlovu@gmail.com'),
     'CPC1203', '2026-08-22T19:58:00', 'Pending'),

    -- Durban Beachfront Charity Walk, entries still open
    ((SELECT c.CategoryId FROM dbo.EventCategories AS c
        INNER JOIN dbo.Events AS e ON e.EventId = c.EventId
        WHERE e.EventName = 'Durban Beachfront Charity Walk' AND c.CategoryName = '8 km Promenade Walk'),
     (SELECT UserId FROM dbo.Users WHERE Email = 'aisha.patel@outlook.com'),
     'DBW0112', '2026-08-30T10:11:00', 'Confirmed'),

    ((SELECT c.CategoryId FROM dbo.EventCategories AS c
        INNER JOIN dbo.Events AS e ON e.EventId = c.EventId
        WHERE e.EventName = 'Durban Beachfront Charity Walk' AND c.CategoryName = '3 km Family Walk'),
     (SELECT UserId FROM dbo.Users WHERE Email = 'lerato.dlamini@gmail.com'),
     'DBW0233', '2026-09-01T15:42:00', 'Pending');
GO


/*
   Only the Johannesburg event has results, because it is the only one that has
   actually been run. The two upcoming events have entries but no results, which
   is exactly the state the Part 3 My Results page has to cope with.
*/

INSERT INTO dbo.Results
    (EnrolmentId, CapturedByUserId, FinishTime, PositionOverall, PositionInCategory, ResultStatus, CapturedAt)
VALUES
    ((SELECT EnrolmentId FROM dbo.Enrolments WHERE RaceNumber = 'JHM1042'),
     (SELECT UserId FROM dbo.Users WHERE Email = 'thandeka.mokoena@racedaysa.co.za'),
     '01:34:52', 47, 12, 'Finished', '2026-05-24T14:20:00'),

    ((SELECT EnrolmentId FROM dbo.Enrolments WHERE RaceNumber = 'JHM1087'),
     (SELECT UserId FROM dbo.Users WHERE Email = 'thandeka.mokoena@racedaysa.co.za'),
     '01:48:19', 132, 8, 'Finished', '2026-05-24T14:22:00'),

    ((SELECT EnrolmentId FROM dbo.Enrolments WHERE RaceNumber = 'JHM2210'),
     (SELECT UserId FROM dbo.Users WHERE Email = 'thandeka.mokoena@racedaysa.co.za'),
     '00:52:07', 63, 5, 'Finished', '2026-05-24T13:05:00'),

    ((SELECT EnrolmentId FROM dbo.Enrolments WHERE RaceNumber = 'JHM3018'),
     (SELECT UserId FROM dbo.Users WHERE Email = 'thandeka.mokoena@racedaysa.co.za'),
     NULL, NULL, NULL, 'DNF', '2026-05-24T12:40:00');
GO


/*
   These queries are not part of the schema. They are here so that the script
   can be shown working live in SSMS: the first confirms every table was created
   and populated, and the second proves the relationships join correctly all the
   way from a user through to a result.
*/

SELECT 'Roles'            AS TableName, COUNT(*) AS TotalRows FROM dbo.Roles
UNION ALL
SELECT 'Disciplines',     COUNT(*) FROM dbo.Disciplines
UNION ALL
SELECT 'Users',           COUNT(*) FROM dbo.Users
UNION ALL
SELECT 'Events',          COUNT(*) FROM dbo.Events
UNION ALL
SELECT 'EventCategories', COUNT(*) FROM dbo.EventCategories
UNION ALL
SELECT 'Enrolments',      COUNT(*) FROM dbo.Enrolments
UNION ALL
SELECT 'Results',         COUNT(*) FROM dbo.Results;
GO


/* A participant's race history, which is the query behind the My Results page
   in Part 3. It walks Users to Enrolments to EventCategories to Events and then
   out to Results, touching five of the seven tables in one join. 
*/

SELECT
    u.FirstName + ' ' + u.LastName  AS Participant,
    e.EventName,
    e.EventDate,
    c.CategoryName,
    en.RaceNumber,
    en.EnrolmentStatus,
    r.FinishTime,
    r.PositionOverall,
    r.ResultStatus
FROM dbo.Users AS u
INNER JOIN dbo.Enrolments      AS en ON en.ParticipantId = u.UserId
INNER JOIN dbo.EventCategories AS c  ON c.CategoryId     = en.CategoryId
INNER JOIN dbo.Events          AS e  ON e.EventId        = c.EventId
LEFT  JOIN dbo.Results         AS r  ON r.EnrolmentId    = en.EnrolmentId
ORDER BY u.LastName, e.EventDate DESC;
GO
