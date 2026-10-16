using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
    }

    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<RaceEvent> Events => Set<RaceEvent>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventCategory> EventCategories => Set<EventCategory>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<RaceResult> Results => Set<RaceResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organiser>(e =>
        {
            e.ToTable("Organisers");
            e.HasKey(x => x.OrganiserID).HasName("PK_Organisers");
            e.Property(x => x.FirstName).HasColumnType("varchar(50)").IsRequired();
            e.Property(x => x.LastName).HasColumnType("varchar(50)").IsRequired();
            e.Property(x => x.Email).HasColumnType("varchar(100)").IsRequired();
            e.Property(x => x.Phone).HasColumnType("varchar(20)").IsRequired();
            e.Property(x => x.OrganisationName).HasColumnType("varchar(100)").IsRequired();
            e.Property(x => x.PasswordHash).HasColumnType("varchar(500)").IsRequired();
            e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UQ_Organisers_Email");
        });

        modelBuilder.Entity<Participant>(e =>
        {
            e.ToTable("Participants");
            e.HasKey(x => x.ParticipantID).HasName("PK_Participants");
            e.Property(x => x.FirstName).HasColumnType("varchar(50)").IsRequired();
            e.Property(x => x.LastName).HasColumnType("varchar(50)").IsRequired();
            e.Property(x => x.Email).HasColumnType("varchar(100)").IsRequired();
            e.Property(x => x.Phone).HasColumnType("varchar(20)").IsRequired();
            e.Property(x => x.DateOfBirth).HasColumnType("date");
            e.Property(x => x.Gender).HasColumnType("varchar(10)").IsRequired();
            e.Property(x => x.PasswordHash).HasColumnType("varchar(500)").IsRequired();
            e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UQ_Participants_Email");
        });

        modelBuilder.Entity<RaceEvent>(e =>
        {
            e.ToTable("Events", t =>
            {
                t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Run', 'Walk', 'Cycle')");
                t.HasCheckConstraint("CK_Events_DistanceKM", "[DistanceKM] > 0");
            });
            e.HasKey(x => x.EventID).HasName("PK_Events");
            e.Property(x => x.EventName).HasColumnType("varchar(100)").IsRequired();
            e.Property(x => x.Description).HasColumnType("varchar(500)").IsRequired();
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.Location).HasColumnType("varchar(100)").IsRequired();
            e.Property(x => x.DistanceKM).HasColumnType("decimal(5,2)");
            e.Property(x => x.EventType).HasConversion<string>().HasColumnType("varchar(10)").IsRequired();
            e.HasOne(x => x.Organiser)
                .WithMany(o => o.Events)
                .HasForeignKey(x => x.OrganiserID)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Events_Organisers");
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.CategoryID).HasName("PK_Categories");
            e.Property(x => x.CategoryName).HasColumnType("varchar(50)").IsRequired();
            e.Property(x => x.Description).HasColumnType("varchar(200)");
            e.HasIndex(x => x.CategoryName).IsUnique().HasDatabaseName("UQ_Categories_CategoryName");
        });

        modelBuilder.Entity<EventCategory>(e =>
        {
            e.ToTable("EventCategories");
            e.HasKey(x => new { x.EventID, x.CategoryID }).HasName("PK_EventCategories");
            e.HasOne(x => x.Event)
                .WithMany(ev => ev.EventCategories)
                .HasForeignKey(x => x.EventID)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_EventCategories_Events");
            e.HasOne(x => x.Category)
                .WithMany(c => c.EventCategories)
                .HasForeignKey(x => x.CategoryID)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_EventCategories_Categories");
        });

        modelBuilder.Entity<Enrollment>(e =>
        {
            e.ToTable("Enrollments");
            e.HasKey(x => x.EnrollmentID).HasName("PK_Enrollments");
            e.Property(x => x.EnrollmentDate).HasColumnType("date").HasDefaultValueSql("GETDATE()");
            e.Property(x => x.Status).HasColumnType("varchar(20)").IsRequired().HasDefaultValue("Registered");
            e.HasIndex(x => x.BibNumber).IsUnique().HasDatabaseName("UQ_Enrollments_BibNumber");
            e.HasIndex(x => new { x.ParticipantID, x.EventID }).IsUnique().HasDatabaseName("UQ_Enrollments_Participant_Event");
            e.HasOne(x => x.Participant)
                .WithMany(p => p.Enrollments)
                .HasForeignKey(x => x.ParticipantID)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Enrollments_Participants");
            e.HasOne(x => x.Event)
                .WithMany(ev => ev.Enrollments)
                .HasForeignKey(x => x.EventID)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Enrollments_Events");
            e.HasOne(x => x.Category)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(x => x.CategoryID)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Enrollments_Categories");
        });

        modelBuilder.Entity<RaceResult>(e =>
        {
            e.ToTable("Results", t => t.HasCheckConstraint("CK_Results_FinishPosition", "[FinishPosition] > 0"));
            e.HasKey(x => x.ResultID).HasName("PK_Results");
            e.Property(x => x.FinishTime).HasColumnType("time");
            e.Property(x => x.RecordedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.EnrollmentID).IsUnique().HasDatabaseName("UQ_Results_EnrollmentID");
            e.HasOne(x => x.Enrollment)
                .WithOne(en => en.Result)
                .HasForeignKey<RaceResult>(x => x.EnrollmentID)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Results_Enrollments");
        });
    }
}
