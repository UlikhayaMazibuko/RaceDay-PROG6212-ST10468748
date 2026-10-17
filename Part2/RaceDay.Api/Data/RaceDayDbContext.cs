using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data
{
    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
        {
        }

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Discipline> Disciplines => Set<Discipline>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<EventCategory> EventCategories => Set<EventCategory>();
        public DbSet<Enrolment> Enrolments => Set<Enrolment>();
        public DbSet<Result> Results => Set<Result>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureRoles(modelBuilder);
            ConfigureDisciplines(modelBuilder);
            ConfigureUsers(modelBuilder);
            ConfigureEvents(modelBuilder);
            ConfigureEventCategories(modelBuilder);
            ConfigureEnrolments(modelBuilder);
            ConfigureResults(modelBuilder);
        }

        private static void ConfigureRoles(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(r => r.RoleId);

                entity.Property(r => r.RoleName).HasMaxLength(20).IsRequired();
                entity.Property(r => r.Description).HasMaxLength(100);

                entity.HasIndex(r => r.RoleName).IsUnique().HasDatabaseName("UQ_Roles_Name");

                entity.HasData(
                    new Role { RoleId = 1, RoleName = "Organiser", Description = "Creates and manages events, categories, enrolments and results." },
                    new Role { RoleId = 2, RoleName = "Participant", Description = "Browses events, enters events and tracks personal results." });
            });
        }

        private static void ConfigureDisciplines(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Discipline>(entity =>
            {
                entity.HasKey(d => d.DisciplineId);

                entity.Property(d => d.DisciplineName).HasMaxLength(30).IsRequired();

                entity.HasIndex(d => d.DisciplineName).IsUnique().HasDatabaseName("UQ_Disciplines_Name");

                entity.HasData(
                    new Discipline { DisciplineId = 1, DisciplineName = "Running" },
                    new Discipline { DisciplineId = 2, DisciplineName = "Walking" },
                    new Discipline { DisciplineId = 3, DisciplineName = "Cycling" });
            });
        }

        private static void ConfigureUsers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.UserId);

                entity.Property(u => u.FirstName).HasMaxLength(50).IsRequired();
                entity.Property(u => u.LastName).HasMaxLength(50).IsRequired();
                entity.Property(u => u.Email).HasMaxLength(150).IsRequired();
                entity.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
                entity.Property(u => u.PhoneNumber).HasMaxLength(20);
                entity.Property(u => u.DateOfBirth).HasColumnType("date");
                entity.Property(u => u.Gender).HasMaxLength(10);
                entity.Property(u => u.City).HasMaxLength(60);

                entity.Property(u => u.CreatedAt)
                      .HasColumnType("datetime2(0)")
                      .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.Property(u => u.IsActive)
                      .HasDefaultValue(true)
                      .HasSentinel(true);

                entity.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UQ_Users_Email");

                entity.HasOne(u => u.Role)
                      .WithMany(r => r.Users)
                      .HasForeignKey(u => u.RoleId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .HasConstraintName("FK_Users_Roles");

                entity.ToTable(t => t.HasCheckConstraint("CK_Users_Dob", "[DateOfBirth] < CAST(GETDATE() AS DATE)"));
            });
        }

        private static void ConfigureEvents(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.EventId);

                entity.Property(e => e.EventName).HasMaxLength(120).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.EventDate).HasColumnType("date");
                entity.Property(e => e.StartTime).HasColumnType("time(0)");
                entity.Property(e => e.VenueName).HasMaxLength(150).IsRequired();
                entity.Property(e => e.City).HasMaxLength(60).IsRequired();
                entity.Property(e => e.Province).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Latitude).HasColumnType("decimal(9, 6)");
                entity.Property(e => e.Longitude).HasColumnType("decimal(9, 6)");
                entity.Property(e => e.RegistrationCloseDate).HasColumnType("date");

                entity.Property(e => e.Status)
                      .HasMaxLength(20)
                      .IsRequired()
                      .HasDefaultValue("Draft");

                entity.Property(e => e.CreatedAt)
                      .HasColumnType("datetime2(0)")
                      .HasDefaultValueSql("SYSUTCDATETIME()");
                entity.HasOne(e => e.Organiser)
                      .WithMany(u => u.OrganisedEvents)
                      .HasForeignKey(e => e.OrganiserId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .HasConstraintName("FK_Events_Organiser");

                entity.HasOne(e => e.Discipline)
                      .WithMany(d => d.Events)
                      .HasForeignKey(e => e.DisciplineId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .HasConstraintName("FK_Events_Discipline");

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Events_Status", "[Status] IN ('Draft', 'Published', 'Completed', 'Cancelled')");
                    t.HasCheckConstraint("CK_Events_CloseDate", "[RegistrationCloseDate] <= [EventDate]");
                });
            });
        }

        private static void ConfigureEventCategories(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EventCategory>(entity =>
            {
                entity.HasKey(c => c.CategoryId);

                entity.Property(c => c.CategoryName).HasMaxLength(60).IsRequired();
                entity.Property(c => c.DistanceKm).HasColumnType("decimal(5, 2)");
                entity.Property(c => c.EntryFee).HasColumnType("decimal(8, 2)").HasDefaultValue(0m);
                entity.Property(c => c.CategoryStartTime).HasColumnType("time(0)");
                entity.Property(c => c.MinimumAge).HasDefaultValue(0);

                entity.HasIndex(c => new { c.EventId, c.CategoryName })
                      .IsUnique()
                      .HasDatabaseName("UQ_EventCategories_Name");

                entity.HasOne(c => c.Event)
                      .WithMany(e => e.Categories)
                      .HasForeignKey(c => c.EventId)
                      .OnDelete(DeleteBehavior.Cascade)
                      .HasConstraintName("FK_EventCategories_Event");

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_EventCategories_Distance", "[DistanceKm] > 0");
                    t.HasCheckConstraint("CK_EventCategories_Fee", "[EntryFee] >= 0");
                    t.HasCheckConstraint("CK_EventCategories_Age", "[MinimumAge] >= 0");
                    t.HasCheckConstraint("CK_EventCategories_Max", "[MaxParticipants] IS NULL OR [MaxParticipants] > 0");
                });
            });
        }

        private static void ConfigureEnrolments(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Enrolment>(entity =>
            {
                entity.HasKey(en => en.EnrolmentId);

                entity.Property(en => en.RaceNumber).HasMaxLength(10).IsRequired();

                entity.Property(en => en.EnrolmentDate)
                      .HasColumnType("datetime2(0)")
                      .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.Property(en => en.EnrolmentStatus)
                      .HasMaxLength(20)
                      .IsRequired()
                      .HasDefaultValue("Pending");

                entity.HasIndex(en => new { en.CategoryId, en.ParticipantId })
                      .IsUnique()
                      .HasDatabaseName("UQ_Enrolments_Participant");

                entity.HasIndex(en => new { en.CategoryId, en.RaceNumber })
                      .IsUnique()
                      .HasDatabaseName("UQ_Enrolments_RaceNumber");

                entity.HasOne(en => en.Category)
                      .WithMany(c => c.Enrolments)
                      .HasForeignKey(en => en.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .HasConstraintName("FK_Enrolments_Category");

                entity.HasOne(en => en.Participant)
                      .WithMany(u => u.Enrolments)
                      .HasForeignKey(en => en.ParticipantId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .HasConstraintName("FK_Enrolments_Participant");

                entity.ToTable(t => t.HasCheckConstraint("CK_Enrolments_Status", "[EnrolmentStatus] IN ('Pending', 'Confirmed', 'Cancelled')"));
            });
        }

        private static void ConfigureResults(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Result>(entity =>
            {
                entity.HasKey(r => r.ResultId);

                entity.Property(r => r.FinishTime).HasColumnType("time(0)");

                entity.Property(r => r.ResultStatus)
                      .HasMaxLength(15)
                      .IsRequired()
                      .HasDefaultValue("Finished");

                entity.Property(r => r.CapturedAt)
                      .HasColumnType("datetime2(0)")
                      .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasIndex(r => r.EnrolmentId).IsUnique().HasDatabaseName("UQ_Results_Enrolment");

                entity.HasOne(r => r.Enrolment)
                      .WithOne(en => en.Result)
                      .HasForeignKey<Result>(r => r.EnrolmentId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .HasConstraintName("FK_Results_Enrolment");

                entity.HasOne(r => r.CapturedBy)
                      .WithMany(u => u.CapturedResults)
                      .HasForeignKey(r => r.CapturedByUserId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .HasConstraintName("FK_Results_CapturedBy");

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Results_Status", "[ResultStatus] IN ('Finished', 'DNF', 'DNS', 'DQ')");
                    t.HasCheckConstraint("CK_Results_Overall", "[PositionOverall] IS NULL OR [PositionOverall] > 0");
                    t.HasCheckConstraint("CK_Results_Category", "[PositionInCategory] IS NULL OR [PositionInCategory] > 0");
                });
            });
        }
    }
}