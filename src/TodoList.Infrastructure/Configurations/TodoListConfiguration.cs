using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TodoList.Infrastructure.Configurations;

public class TodoListConfiguration : IEntityTypeConfiguration<Domain.Entities.TodoList>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.TodoList> builder)
    {
        builder.ToTable("TodoLists");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.CreatedAt)
            .IsRequired();

        builder.HasMany(l => l.Items)
            .WithOne(i => i.TodoList)
            .HasForeignKey(i => i.TodoListId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
