using EducationalCompany.Api.Domain.Common;
using EducationalCompany.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;


namespace EducationalCompany.Api.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext (DbContextOptions<ApplicationDbContext> options) :base(options)
        { }

        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseOccasion> CourseOccasions { get; set; }
        public DbSet<CourseRegistration> CourseRegistrations { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Teacher> Teachers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Course>(entity => 
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
                entity.Property(e => e.DurationHours).IsRequired();
                entity.Property(e => e.Price).HasPrecision(18,2);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<CourseOccasion>(entity =>
            {
                entity.Property(e => e.StartDate).IsRequired();
                entity.Property(e => e.EndDate).IsRequired();
                entity.Property(e => e.MaxParticipants).IsRequired();
                entity.Property(e => e.CurrentParticipants).IsRequired();
                entity.HasOne(e => e.Course)
                      .WithMany(c => c.Occasions)
                      .HasForeignKey(e => e.CourseId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Teacher)
                      .WithMany(t => t.CourseOccasions)
                      .HasForeignKey(e => e.TeacherId)
                      .OnDelete(DeleteBehavior.SetNull);
            });  

            modelBuilder.Entity<Teacher>(entity =>
            {
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Specialization).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.Email).IsUnique();
            });

            modelBuilder.Entity<Participant>(entity =>
            {
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.Email).IsUnique();
            });

            modelBuilder.Entity<CourseRegistration>(entity =>
            {
                entity.Property(e => e.RegistrationDate).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.ConfirmedAt).IsRequired();
                entity.Property(e => e.CancelledAt).IsRequired();
                entity.HasOne(e => e.Participant)
                      .WithMany(p => p.Registrations)
                      .HasForeignKey(e => e.ParticipantId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.CourseOccasion)
                      .WithMany(co => co.Registrations)
                      .HasForeignKey(e => e.TeacherId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new {e.CourseOccasionId,e.ParticipantId}).IsUnique();
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken=default)
            {
                foreach (var entry in ChangeTracker.Entries<BaseEntity>()) 
                {
                if (entry.State == EntityState.Modified)
                    entry.Entity.UpdateModifiedDate();
                }
                return await base.SaveChangesAsync(cancellationToken);
            }
        

    }
}
