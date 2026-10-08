using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Models;

namespace Scheduler.Api.Data;

public class SchedulerDbContext(DbContextOptions<SchedulerDbContext> options) : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<SmsMessage> SmsMessages => Set<SmsMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(e =>
        {
            e.Property(c => c.FirstName).HasMaxLength(100);
            e.Property(c => c.LastName).HasMaxLength(100);
            e.Property(c => c.PhoneNumber).HasMaxLength(20);
            e.Property(c => c.Email).HasMaxLength(256);
            e.Property(c => c.Notes).HasMaxLength(2000);
            e.Ignore(c => c.FullName);
            e.HasIndex(c => c.PhoneNumber);
        });

        modelBuilder.Entity<Appointment>(e =>
        {
            e.Property(a => a.Title).HasMaxLength(200);
            e.Property(a => a.Description).HasMaxLength(2000);
            e.Property(a => a.Location).HasMaxLength(300);
            e.Property(a => a.Color).HasMaxLength(9);
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(a => a.StartUtc);
            e.HasOne(a => a.Client)
             .WithMany(c => c.Appointments)
             .HasForeignKey(a => a.ClientId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SmsMessage>(e =>
        {
            e.Property(s => s.ToPhoneNumber).HasMaxLength(20);
            e.Property(s => s.Body).HasMaxLength(1600);
            e.Property(s => s.ProviderMessageId).HasMaxLength(64);
            e.Property(s => s.Error).HasMaxLength(2000);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(s => s.Appointment)
             .WithMany(a => a.SmsMessages)
             .HasForeignKey(s => s.AppointmentId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
