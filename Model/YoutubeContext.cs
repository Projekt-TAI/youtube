﻿using Microsoft.EntityFrameworkCore;

namespace TAIBackend.Model;

public partial class YoutubeContext : DbContext
{
    public YoutubeContext()
    {
    }

    public YoutubeContext(DbContextOptions<YoutubeContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<Like> Likes { get; set; }

    public virtual DbSet<Video> Videos { get; set; }
    
    public virtual DbSet<Subscription> Subscriptions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseInMemoryDatabase("youtube");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_1_accounts");

            entity.ToTable("accounts");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .HasColumnName("email");
            entity.Property(e => e.Firstname)
                .HasMaxLength(50)
                .HasColumnName("firstname");
            entity.Property(e => e.Fullname)
                .HasMaxLength(50)
                .HasColumnName("fullname");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_1_comments");

            entity.ToTable("comments");

            entity.HasIndex(e => e.Videoid, "fk_1_comments");

            entity.HasIndex(e => e.Commenterid, "fk_3_comments");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .ValueGeneratedOnAdd()
                .HasColumnName("created_at");
            entity.Property(e => e.Commenterid).HasColumnName("commenterid");
            entity.Property(e => e.Data)
                .HasMaxLength(2000)
                .HasColumnName("data");
            entity.Property(e => e.Videoid).HasColumnName("videoid");

            entity.HasOne(d => d.Commenter).WithMany(p => p.Comments)
                .HasForeignKey(d => d.Commenterid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_4_comments");

            entity.HasOne(d => d.Video).WithMany(p => p.Comments)
                .HasForeignKey(d => d.Videoid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_3_comments");
        });

        modelBuilder.Entity<Like>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_1_likes");

            entity.ToTable("likes");

            entity.HasIndex(e => e.Account, "fk_1_likes");

            entity.HasIndex(e => e.Video, "fk_2_likes");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Account).HasColumnName("account");
            entity.Property(e => e.Unlike).HasColumnName("unlike");
            entity.Property(e => e.Video).HasColumnName("video");

            entity.HasOne(d => d.AccountNavigation).WithMany(p => p.Likes)
                .HasForeignKey(d => d.Account)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_6_likes");

            entity.HasOne(d => d.VideoNavigation).WithMany(p => p.Likes)
                .HasForeignKey(d => d.Video)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_7_likes");
        });

        modelBuilder.Entity<Video>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_1_videos");

            entity.ToTable("videos");

            entity.HasIndex(e => e.Owneraccountid, "fk_1_videos");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.Description)
                .HasMaxLength(50)
                .HasColumnName("description");
            entity.Property(e => e.Owneraccountid).HasColumnName("owneraccountid");
            entity.Property(e => e.Title)
                .HasMaxLength(50)
                .HasColumnName("title");
            entity.Property(e => e.Views).HasColumnName("views");

            entity.HasOne(d => d.Owneraccount).WithMany(p => p.Videos)
                .HasForeignKey(d => d.Owneraccountid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_1_videos");
        });
        
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_1_subscriptions");

            entity.ToTable("subscriptions");

            entity.HasIndex(e => e.Owneraccountid, "fk_1_subscriptions");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            
            entity.Property(e => e.Owneraccountid).HasColumnName("owneraccountid");

            entity.HasOne(d => d.Owneraccount).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.Owneraccountid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_1_subscriptions");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
