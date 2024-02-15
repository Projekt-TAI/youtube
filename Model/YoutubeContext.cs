using Microsoft.EntityFrameworkCore;

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

            entity.HasIndex(e => e.Commenterid, "fk_2_comments");

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
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_3_comments");

            entity.HasOne(d => d.Video).WithMany(p => p.Comments)
                .HasForeignKey(d => d.Videoid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_4_comments");
        });

        modelBuilder.Entity<Like>(entity =>
        {
            entity.HasKey(e => new { e.AccountId, e.VideoId }).HasName("pk_1_likes");

            entity.ToTable("likes");

            entity.HasIndex(e => e.AccountId, "fk_1_likes");

            entity.HasIndex(e => e.VideoId, "fk_2_likes");

            entity.Property(e => e.AccountId).HasColumnName("accountId");
            entity.Property(e => e.Unlike).HasColumnName("unlike");
            entity.Property(e => e.VideoId).HasColumnName("videoId");

            entity.HasOne(d => d.Account).WithMany(p => p.Likes)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_6_likes");

            entity.HasOne(d => d.Video).WithMany(p => p.Likes)
                .HasForeignKey(d => d.VideoId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_7_likes");
        });

        modelBuilder.Entity<Video>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_1_videos");

            entity.ToTable("videos");

            entity.HasIndex(e => e.OwneraccountId, "fk_1_videos");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.Description)
                .HasMaxLength(50)
                .HasColumnName("description");
            entity.Property(e => e.OwneraccountId).HasColumnName("owneraccountid");
            entity.Property(e => e.Title)
                .HasMaxLength(50)
                .HasColumnName("title");
            entity.Property(e => e.Views).HasColumnName("views");

            entity.HasOne(d => d.Owneraccount).WithMany(p => p.Videos)
                .HasForeignKey(d => d.OwneraccountId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_2_videos");
        });
        
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => new { e.OwneraccountId, e.SubscribedaccountId }).HasName("pk_1_subscriptions");

            entity.ToTable("subscriptions");

            entity.HasIndex(e => e.OwneraccountId, "fk_1_subscriptions");

            entity.HasIndex(e => e.SubscribedaccountId, "fk_2_subscriptions");
            
            entity.Property(e => e.OwneraccountId).HasColumnName("owneraccountid");

            entity.HasOne(d => d.Owneraccount).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.OwneraccountId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_1_subscriptions");

            entity.HasOne(d => d.Subscribedaccount).WithMany(p => p.Subscribers)
                .HasForeignKey(d => d.SubscribedaccountId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_2_subscriptions");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
