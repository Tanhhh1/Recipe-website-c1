using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace EProject.NETCore.Models;

public partial class JamesThewContext : DbContext
{
    public JamesThewContext()
    {
    }

    public JamesThewContext(DbContextOptions<JamesThewContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Announcement> Announcements { get; set; }

    public virtual DbSet<Competition> Competitions { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Guidance> Guidances { get; set; }

    public virtual DbSet<Submission> Submissions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=DESKTOP-D1TJ1DB\\SQLEXPRESS;Initial Catalog=JamesThew;User ID=sa;Password=Aptech@2024;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Announce__3213E83FA255B644");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompetitionId).HasColumnName("competition_id");
            entity.Property(e => e.Date).HasColumnName("date");

            entity.HasOne(d => d.Competition).WithMany(p => p.Announcements)
                .HasForeignKey(d => d.CompetitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Announcements_Competitions");
        });

        modelBuilder.Entity<Competition>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Competit__3213E83F01E77E0C");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Feedback__3213E83F5326DE2E");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Fullname)
                .HasMaxLength(100)
                .HasColumnName("fullname");
            entity.Property(e => e.GuidanceId).HasColumnName("guidance_id");

            entity.HasOne(d => d.Guidance).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.GuidanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Feedbacks_Guidance");
        });

        modelBuilder.Entity<Guidance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Guidance__3213E83FA4C37FF8");

            entity.ToTable("Guidance");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Img)
                .HasMaxLength(255)
                .HasColumnName("img");
            entity.Property(e => e.IsFree).HasColumnName("is_free");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.UpdatedDate).HasColumnName("updated_date");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Guidances)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Guidance_Users");
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Submissi__3213E83F97C61159");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompetitionId).HasColumnName("competition_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Fullname)
                .HasMaxLength(100)
                .HasColumnName("fullname");
            entity.Property(e => e.IsWinner).HasColumnName("is_winner");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");

            entity.HasOne(d => d.Competition).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.CompetitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Submissions_Competitions");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3213E83F22EA6022");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.ExpirationDate).HasColumnName("expiration_date");
            entity.Property(e => e.Fullname)
                .HasMaxLength(100)
                .HasColumnName("fullname");
            entity.Property(e => e.MembershipType).HasColumnName("membership_type");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
