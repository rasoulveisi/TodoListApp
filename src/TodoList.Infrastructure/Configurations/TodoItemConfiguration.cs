using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoList.Domain.Entities;

namespace TodoList.Infrastructure.Configurations;

public class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.ToTable("TodoItems");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        builder.Property(t => t.IsCompleted)
            .HasDefaultValue(false);

        builder.Property(t => t.IsImportant)
            .HasDefaultValue(false);

        builder.Property(t => t.IsInMyDay)
            .HasDefaultValue(false);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.OwnsOne(t => t.Recurrence, recurrence =>
        {
            recurrence.Property(r => r.Type)
                .HasColumnName("RecurrenceType");

            recurrence.Property(r => r.Interval)
                .HasColumnName("RecurrenceInterval");

            recurrence.Property(r => r.EndDate)
                .HasColumnName("RecurrenceEndDate");
        });

        builder.HasOne(t => t.TodoList)
            .WithMany(l => l.Items)
            .HasForeignKey(t => t.TodoListId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(t => t.Categories)
            .WithMany()
            .UsingEntity("TodoItemCategory");

        builder.HasIndex(t => t.TodoListId);
        builder.HasIndex(t => t.IsCompleted);
        builder.HasIndex(t => t.DueDate);
        builder.HasIndex(t => t.IsImportant);
        builder.HasIndex(t => t.IsInMyDay);
    }
}
