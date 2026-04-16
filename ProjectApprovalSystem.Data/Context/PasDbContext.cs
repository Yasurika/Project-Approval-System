using Microsoft.EntityFrameworkCore;
using ProjectApprovalSystem.Core.Entities;

namespace ProjectApprovalSystem.Data.Context;

/// <summary>
/// Entity Framework Core DbContext for Project Approval System
/// Manages database interactions and entity relationships
/// </summary>
public class PasDbContext : DbContext
{
    public PasDbContext(DbContextOptions<PasDbContext> options) : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<ResearchArea> ResearchAreas { get; set; } = null!;
    public DbSet<Proposal> Proposals { get; set; } = null!;
    public DbSet<SupervisorExpertise> SupervisorExpertises { get; set; } = null!;
    public DbSet<Match> Matches { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(256);
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(256);
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(256);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);

            // Relationships
            entity.HasMany(e => e.StudentProposals)
                .WithOne(p => p.Student)
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Expertises)
                .WithOne(se => se.Supervisor)
                .HasForeignKey(se => se.SupervisorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Matches)
                .WithOne(m => m.Supervisor)
                .HasForeignKey(m => m.SupervisorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ResearchArea entity
        modelBuilder.Entity<ResearchArea>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasIndex(e => e.Name).IsUnique();

            // Relationships
            entity.HasMany(e => e.Proposals)
                .WithOne(p => p.ResearchArea)
                .HasForeignKey(p => p.ResearchAreaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.SupervisorExpertises)
                .WithOne(se => se.ResearchArea)
                .HasForeignKey(se => se.ResearchAreaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Proposal entity - CRITICAL FOR BLIND MATCHING
        modelBuilder.Entity<Proposal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProposalId)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(500);
            entity.Property(e => e.Abstract)
                .IsRequired()
                .HasMaxLength(2000);
            entity.Property(e => e.Description).HasMaxLength(5000);
            entity.Property(e => e.TechStack)
                .IsRequired()
                .HasMaxLength(500);
            entity.HasIndex(e => e.ProposalId).IsUnique();

            // Relationships
            entity.HasOne(e => e.Student)
                .WithMany(u => u.StudentProposals)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ResearchArea)
                .WithMany(r => r.Proposals)
                .HasForeignKey(e => e.ResearchAreaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Matches)
                .WithOne(m => m.Proposal)
                .HasForeignKey(m => m.ProposalId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SupervisorExpertise entity
        modelBuilder.Entity<SupervisorExpertise>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Composite index for unique constraint
            entity.HasIndex(e => new { e.SupervisorId, e.ResearchAreaId })
                .IsUnique();

            // Relationships
            entity.HasOne(e => e.Supervisor)
                .WithMany(u => u.Expertises)
                .HasForeignKey(e => e.SupervisorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ResearchArea)
                .WithMany(r => r.SupervisorExpertises)
                .HasForeignKey(e => e.ResearchAreaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Match entity - CRITICAL FOR BLIND MATCHING
        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SupervisorComments).HasMaxLength(1000);

            // Relationships
            entity.HasOne(e => e.Proposal)
                .WithMany(p => p.Matches)
                .HasForeignKey(e => e.ProposalId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Supervisor)
                .WithMany(u => u.Matches)
                .HasForeignKey(e => e.SupervisorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure one supervisor can only express interest in a proposal once
            entity.HasIndex(e => new { e.ProposalId, e.SupervisorId })
                .IsUnique();
        });
    }
}
