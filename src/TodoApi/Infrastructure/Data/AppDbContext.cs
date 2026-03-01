using Microsoft.EntityFrameworkCore;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;

namespace TodoApi.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TodoItem> Todos => Set<TodoItem>();
    public DbSet<TodoAttachment> TodoAttachments => Set<TodoAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(100).IsRequired();
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(500).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.HasIndex(x => x.UserId).IsUnique();
        });

        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.ToTable("todos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserRefId).HasColumnName("user_ref_id").IsRequired();
            entity.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion(
                    x => ToDatabaseStatus(x),
                    x => FromDatabaseStatus(x))
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

            entity.HasOne(x => x.User)
                .WithMany(x => x.Todos)
                .HasForeignKey(x => x.UserRefId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.UserRefId, x.CreatedAt });
        });

        modelBuilder.Entity<TodoAttachment>(entity =>
        {
            entity.ToTable("todo_attachments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TodoRefId).HasColumnName("todo_ref_id").IsRequired();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(x => x.Size).HasColumnName("size").IsRequired();
            entity.Property(x => x.Type).HasColumnName("type").HasMaxLength(100).IsRequired();
            entity.Property(x => x.S3Key).HasColumnName("s3_key").IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasOne(x => x.Todo)
                .WithMany(x => x.Attachments)
                .HasForeignKey(x => x.TodoRefId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.TodoRefId);
        });
    }

    private static string ToDatabaseStatus(TodoStatus status)
    {
        return status switch
        {
            TodoStatus.Pending => "pending",
            TodoStatus.InProgress => "in_progress",
            TodoStatus.Done => "done",
            _ => "pending"
        };
    }

    private static TodoStatus FromDatabaseStatus(string value)
    {
        return value switch
        {
            "pending" => TodoStatus.Pending,
            "in_progress" => TodoStatus.InProgress,
            "done" => TodoStatus.Done,
            _ => TodoStatus.Pending
        };
    }
}
