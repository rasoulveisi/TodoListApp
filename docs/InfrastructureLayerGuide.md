# Infrastructure Layer — EF Core DbContext & Configurations Guide

This document explains the Entity Framework Core setup in the `TodoList.Infrastructure` project: the DbContext, Fluent API configurations, and migrations. It teaches the concepts you need to map domain entities to a SQL Server database.

---

## Project Structure

```
src/TodoList.Infrastructure/
├── Configurations/
│   ├── TodoItemConfiguration.cs
│   ├── TodoListConfiguration.cs
│   └── CategoryConfiguration.cs
├── Migrations/
│   ├── 20260307152725_InitialCreate.cs
│   ├── 20260307152725_InitialCreate.Designer.cs
│   └── TodoListDbContextModelSnapshot.cs
├── TodoListDbContext.cs
└── TodoList.Infrastructure.csproj
```

| Folder / File | Purpose |
|---|---|
| `TodoListDbContext.cs` | Central class that represents a session with the database. Exposes `DbSet`s and applies configurations. |
| `Configurations/` | Fluent API classes that define table names, column constraints, relationships, and indexes for each entity. |
| `Migrations/` | Generated SQL-like code that creates or updates the database schema. Each migration has an `Up` (apply) and `Down` (revert) method. |

---

## Why EF Core Here?

The **Domain** layer defines entities and abstractions (e.g. `ITodoItemAbstraction`) but has no knowledge of SQL or databases. The **Infrastructure** layer is where we:

1. **Implement** those abstractions using EF Core (repositories come in a later phase).
2. **Map** domain entities to database tables via the DbContext and configurations.
3. **Evolve** the schema over time using migrations.

EF Core is an **ORM** (Object-Relational Mapper): it lets you work with C# objects (`TodoItem`, `TodoList`, etc.) while it translates your code into SQL and manages connections.

---

## DbContext — The Central Session

```csharp
public class TodoListDbContext(DbContextOptions<TodoListDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<Domain.Entities.TodoList> TodoLists => Set<Domain.Entities.TodoList>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TodoListDbContext).Assembly);
    }
}
```

### What is a DbContext?

A **DbContext** represents a **unit of work** with the database. It:

- Tracks loaded entities (change tracking).
- Exposes **DbSet&lt;T&gt;** for each entity type — these are the “tables” you query and save to.
- Builds the **model** (tables, columns, relationships) in `OnModelCreating`.

You typically create one DbContext per request in a web app and dispose it when the request ends.

### Primary constructor

```csharp
TodoListDbContext(DbContextOptions<TodoListDbContext> options) : DbContext(options)
```

This is C# **primary constructor** syntax (C# 12): the parameters are stored and passed to the base class. The API will call `new TodoListDbContext(options)` when resolving the DbContext from dependency injection. No need for a separate field and constructor body.

### DbSet&lt;T&gt; and Set&lt;T&gt;()

- **DbSet&lt;TodoItem&gt;** is the type used for `TodoItems` — it supports LINQ queries and `Add`/`Remove` for persistence.
- **Set&lt;TodoItem&gt;()** is the EF Core method that returns the DbSet for that entity type. Using `Set<T>()` keeps the code DRY and ensures every entity is registered the same way.

### ApplyConfigurationsFromAssembly

```csharp
modelBuilder.ApplyConfigurationsFromAssembly(typeof(TodoListDbContext).Assembly);
```

This scans the **Infrastructure assembly** for all classes that implement **IEntityTypeConfiguration&lt;T&gt;** and runs their `Configure` method. You add a new configuration class and it is picked up automatically — no need to register it in the DbContext by hand.

---

## Entity Configurations — Fluent API

Instead of decorating entities with attributes (e.g. `[MaxLength(200)]`), we use **Fluent API** in separate configuration classes. Benefits:

- Domain entities stay free of persistence concerns.
- All mapping logic lives in one place per entity.
- Complex mappings (owned types, many-to-many) are easier to express.

Each configuration class implements **IEntityTypeConfiguration&lt;TEntity&gt;** and receives an **EntityTypeBuilder&lt;T&gt;** in `Configure`.

---

### CategoryConfiguration — Simple Entity

```csharp
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Color)
            .HasMaxLength(7);
    }
}
```

| Method | Purpose |
|---|---|
| **ToTable("Categories")** | Table name in the database. Without it, EF would use the class name (e.g. `Category`). |
| **HasKey(c => c.Id)** | Declares the primary key. Usually optional if the property is named `Id`. |
| **Property(...).IsRequired()** | Column is NOT NULL. |
| **Property(...).HasMaxLength(n)** | String column max length; in SQL Server this becomes `nvarchar(n)`. |

`Color` is optional (nullable in the domain), so we only set `HasMaxLength(7)` for hex codes like `#FF5733`.

---

### TodoListConfiguration — One-to-Many

```csharp
builder.HasMany(l => l.Items)
    .WithOne(i => i.TodoList)
    .HasForeignKey(i => i.TodoListId)
    .OnDelete(DeleteBehavior.Cascade);
```

- **HasMany(l => l.Items)** — A `TodoList` has many `TodoItem`s.
- **WithOne(i => i.TodoList)** — Each `TodoItem` has one `TodoList`.
- **HasForeignKey(i => i.TodoListId)** — The foreign key column on `TodoItem` is `TodoListId`.
- **OnDelete(DeleteBehavior.Cascade)** — When a list is deleted, all its items are deleted in the database.

This relationship could be inferred by EF Core conventions, but stating it explicitly in configuration makes delete behavior and intent clear.

---

### TodoItemConfiguration — Rich Mapping

This is the most detailed configuration. Key parts:

#### Table and properties

```csharp
builder.ToTable("TodoItems");
builder.HasKey(t => t.Id);

builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
builder.Property(t => t.Description).HasMaxLength(2000);
builder.Property(t => t.IsCompleted).HasDefaultValue(false);
builder.Property(t => t.IsImportant).HasDefaultValue(false);
builder.Property(t => t.IsInMyDay).HasDefaultValue(false);
```

**HasDefaultValue(false)** means the database column gets a default of `0` (false). If you insert a row without specifying these columns, the DB fills them in.

#### Owned type — RecurrencePattern

`RecurrencePattern` is a **value object** (a `record` in the domain). We don’t want a separate table for it; we want its properties stored as columns on `TodoItems`. EF Core calls this an **owned entity type**:

```csharp
builder.OwnsOne(t => t.Recurrence, recurrence =>
{
    recurrence.Property(r => r.Type).HasColumnName("RecurrenceType");
    recurrence.Property(r => r.Interval).HasColumnName("RecurrenceInterval");
    recurrence.Property(r => r.EndDate).HasColumnName("RecurrenceEndDate");
});
```

- **OwnsOne(t => t.Recurrence, ...)** — `Recurrence` is owned by `TodoItem`; its columns live in (or are grouped with) the `TodoItems` table.
- **HasColumnName(...)** — Override the column name so the database has clear names like `RecurrenceType` instead of a default like `Recurrence_Type`.

In the database you get columns: `RecurrenceType`, `RecurrenceInterval`, `RecurrenceEndDate` on `TodoItems`.

#### Many-to-many — TodoItem ↔ Category

```csharp
builder.HasMany(t => t.Categories)
    .WithMany()
    .UsingEntity("TodoItemCategory");
```

- **HasMany(t => t.Categories).WithMany()** — TodoItem has many Categories, Category has many TodoItems; no navigation collection on `Category` in the domain, so `WithMany()` has no argument.
- **UsingEntity("TodoItemCategory")** — Names the join table. EF Core creates a table `TodoItemCategory` with two foreign key columns (e.g. `TodoItemId`, `CategoriesId`).

#### Indexes

```csharp
builder.HasIndex(t => t.TodoListId);
builder.HasIndex(t => t.IsCompleted);
builder.HasIndex(t => t.DueDate);
builder.HasIndex(t => t.IsImportant);
builder.HasIndex(t => t.IsInMyDay);
```

**Indexes** speed up queries filtered or ordered by these columns. For example, “tasks in My Day” or “tasks by list” will use these indexes. We don’t put an index on every column — only those used in filters or joins (as in the plan’s API filters: My Day, Planned, Important).

---

## Migrations — Schema as Code

A **migration** is a snapshot of a schema change. It has:

- **Up()** — Apply the change (create table, add column, etc.).
- **Down()** — Undo the change (drop table, remove column, etc.).

### How migrations are generated

1. You change the model (entity or configuration).
2. You run:  
   `dotnet ef migrations add <MigrationName> --project src/TodoList.Infrastructure --startup-project src/TodoList.Api`
3. EF Core compares the current model to the last snapshot (`TodoListDbContextModelSnapshot.cs`) and generates a new migration class.

The **startup project** (Api) must reference **Microsoft.EntityFrameworkCore.Design** and have a way to create the DbContext (e.g. connection string in `appsettings.json` and DbContext registered in `Program.cs`). The **project** is where the DbContext and entities live (Infrastructure); migrations are generated there (e.g. in `Migrations/`).

### Applying migrations

To update the database to the latest model:

```bash
dotnet ef database update --project src/TodoList.Infrastructure --startup-project src/TodoList.Api
```

To revert the last migration:

```bash
dotnet ef migrations remove --project src/TodoList.Infrastructure --startup-project src/TodoList.Api
```

---

## Connection String & Registration

The API project needs to know which database to use and must register the DbContext.

**appsettings.json:**

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TodoListDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

- **Server=(localdb)\\mssqllocaldb** — LocalDB instance (lightweight SQL Server for development).
- **Database=TodoListDb** — Database name. Created automatically when you run `database update` if it doesn’t exist.
- **Trusted_Connection=True** — Windows authentication.
- **MultipleActiveResultSets=true** — Allows multiple result sets on one connection (useful for some scenarios).

**Program.cs:**

```csharp
builder.Services.AddDbContext<TodoListDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

- **AddDbContext&lt;TodoListDbContext&gt;** — Registers the DbContext with the DI container. Default lifetime is **Scoped** (one instance per HTTP request).
- **UseSqlServer(...)** — Uses the SQL Server provider and the connection string from configuration.

---

## Packages Used

| Package | Project | Purpose |
|---|---|---|
| **Microsoft.EntityFrameworkCore.SqlServer** | Infrastructure | SQL Server provider for EF Core. |
| **Microsoft.EntityFrameworkCore.Design** | Infrastructure, Api | Design-time support: migrations, scaffolding. Required in the startup project for `dotnet ef` commands. |

---

## Summary Table

| Concept | EF Core / C# Feature | Example in This Project |
|---|---|---|
| Database session | `DbContext` | `TodoListDbContext` |
| Table access | `DbSet<T>` | `TodoItems`, `TodoLists`, `Categories` |
| Model building | `OnModelCreating` + `ApplyConfigurationsFromAssembly` | Loads all `IEntityTypeConfiguration<T>` |
| Table name | `ToTable("...")` | `"TodoItems"`, `"TodoLists"`, `"Categories"` |
| Primary key | `HasKey(e => e.Id)` | All three entities |
| Required / length | `IsRequired()`, `HasMaxLength(n)` | Title, Name, Description, Color |
| Default value | `HasDefaultValue(...)` | IsCompleted, IsImportant, IsInMyDay |
| Value object in same table | `OwnsOne(...)` | RecurrencePattern → RecurrenceType, RecurrenceInterval, RecurrenceEndDate |
| One-to-many | `HasMany().WithOne().HasForeignKey().OnDelete()` | TodoList → TodoItem, cascade delete |
| Many-to-many | `HasMany().WithMany().UsingEntity("...")` | TodoItem ↔ Category, table `TodoItemCategory` |
| Query performance | `HasIndex(...)` | TodoListId, IsCompleted, DueDate, IsImportant, IsInMyDay |
| Schema versioning | Migrations | `InitialCreate` — creates all tables and indexes |
| DI registration | `AddDbContext<T>(...)` | In Api `Program.cs` with `UseSqlServer` |

---

## Next Steps

After this guide, the plan continues with:

- **Repository implementations** — Classes that implement `ITodoItemAbstraction`, `ITodoListAbstraction`, and `ICategoryAbstraction` using `TodoListDbContext`.
- **DTOs and pagination** — Response/request DTOs and generic `PaginatedResult<T>`.
- **Exception handling** — Domain exceptions (e.g. `NotFoundException`) and mapping them to HTTP status codes.

The DbContext and configurations you learned here are the foundation for all of that.
