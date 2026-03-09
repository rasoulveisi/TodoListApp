# TodoList API — Full-Stack Architecture & Learning Guide

This document walks through every layer of the TodoList API project. It explains what each piece does, why it exists, and the C# / ASP.NET Core concepts behind it.

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Project Structure](#2-project-structure)
3. [Domain Layer](#3-domain-layer)
4. [Infrastructure Layer](#4-infrastructure-layer)
5. [API Layer](#5-api-layer)
6. [Request Lifecycle — End to End](#6-request-lifecycle--end-to-end)
7. [Key Patterns & Concepts](#7-key-patterns--concepts)
8. [API Endpoints Reference](#8-api-endpoints-reference)

---

## 1. Architecture Overview

The project follows **Clean Architecture** (also called layered or onion architecture). Dependencies point inward:

```
┌─────────────────────────────────────────────────┐
│                  API Layer                       │
│  Controllers → MediatR → Command/Query Handlers │
├─────────────────────────────────────────────────┤
│             Infrastructure Layer                 │
│  EF Core DbContext, Repositories, DTOs, Config   │
├─────────────────────────────────────────────────┤
│               Domain Layer                       │
│  Entities, Value Objects, Enums, Abstractions    │
└─────────────────────────────────────────────────┘
```

| Rule | Meaning |
|------|---------|
| Domain depends on nothing | It has zero NuGet packages — pure C# only. |
| Infrastructure depends on Domain | It implements the interfaces that Domain defines. |
| API depends on both | It wires everything together and exposes HTTP endpoints. |

This separation means you can swap the database, change the web framework, or replace any layer without rewriting the others.

---

## 2. Project Structure

```
src/
├── TodoList.Domain/              ← Pure business models, no dependencies
│   ├── Entities/
│   │   ├── TodoItem.cs
│   │   ├── TodoList.cs
│   │   └── Category.cs
│   ├── ValueObjects/
│   │   └── RecurrencePattern.cs
│   ├── Enums/
│   │   └── RecurrenceType.cs
│   ├── Abstractions/
│   │   ├── ITodoItemAbstraction.cs
│   │   ├── ITodoListAbstraction.cs
│   │   └── ICategoryAbstraction.cs
│   └── PaginatedResult.cs
│
├── TodoList.Infrastructure/      ← Data access, EF Core, PostgreSQL
│   ├── Configurations/           Entity type configurations (Fluent API)
│   ├── Repositories/             Concrete implementations of abstractions
│   ├── Dtos/                     Request/Response data transfer objects
│   ├── Exceptions/               Custom exception types
│   ├── Extensions/               DI registration helpers
│   ├── Pagination/               Reusable pagination extension
│   ├── Migrations/               EF Core migration files
│   └── TodoListDbContext.cs      The EF Core database context
│
├── TodoList.Api/                 ← HTTP layer, wiring, middleware
│   ├── Controllers/              HTTP endpoints (thin — delegate to MediatR)
│   ├── Features/                 MediatR commands, queries, handlers
│   │   ├── TodoItems/
│   │   ├── TodoLists/
│   │   └── Categories/
│   ├── Middleware/                Global exception handling
│   ├── Extensions/               App builder helpers
│   └── Program.cs                Application entry point & DI setup
```

---

## 3. Domain Layer

The domain layer is the heart of the application. It defines *what the data looks like* and *what operations are possible* — without knowing anything about databases, HTTP, or frameworks.

### 3.1 Entities

Entities are POCO (Plain Old CLR Object) classes. They have an `Id` primary key and represent database rows.

**TodoItem** — the core task:

```csharp
public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsImportant { get; set; }
    public bool IsInMyDay { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public RecurrencePattern? Recurrence { get; set; }

    public int? TodoListId { get; set; }
    public TodoList? TodoList { get; set; }
    public List<Category> Categories { get; set; } = [];
}
```

**TodoList** — a container that groups tasks:

```csharp
public class TodoList
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<TodoItem> Items { get; set; } = [];
}
```

**Category** — a tag/label for tasks:

```csharp
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}
```

### 3.2 Relationships

```
TodoList  ──1:many──▸  TodoItem  ──many:many──▸  Category
                       TodoItem  ──0..1──▸  RecurrencePattern (owned)
```

| Relationship | How it works |
|---|---|
| **TodoList → TodoItem** (one-to-many, optional) | `TodoItem.TodoListId` is a nullable FK. A task can exist without belonging to any list. If a list is deleted, the FK is set to `null` (ON DELETE SET NULL). |
| **TodoItem ↔ Category** (many-to-many) | EF Core creates a `TodoItemCategory` join table automatically. No join entity needed in code. |
| **TodoItem → RecurrencePattern** (owned type) | The value object's properties are stored as columns *inside* the `TodoItems` table, not in a separate table. |

### 3.3 Value Objects

A value object has no identity — it is defined entirely by its property values.

```csharp
public record RecurrencePattern
{
    public RecurrenceType Type { get; set; }
    public int Interval { get; set; }
    public DateTime? EndDate { get; set; }
}
```

Using `record` instead of `class` gives value-based equality:

```csharp
var a = new RecurrencePattern { Type = RecurrenceType.Daily, Interval = 1 };
var b = new RecurrencePattern { Type = RecurrenceType.Daily, Interval = 1 };
a == b  // true — records compare by values, classes compare by reference
```

### 3.4 Enums

```csharp
public enum RecurrenceType
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Yearly = 4
}
```

Explicit numbering starting at 1 means `0` is always "unset/invalid" — useful for detecting missing values.

### 3.5 Abstractions (Repository Interfaces)

Abstractions define *what* data operations exist without saying *how* they are implemented:

```csharp
public interface ITodoItemAbstraction
{
    Task<TodoItem?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<TodoItem>> GetAllAsync(CancellationToken ct = default);
    Task<List<TodoItem>> GetByListIdAsync(int todoListId, CancellationToken ct = default);
    Task<PaginatedResult<TodoItem>> GetFilteredAsync(
        bool? isInMyDay, bool? hasDueDate, bool? isImportant,
        int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(TodoItem item, CancellationToken ct = default);
    Task UpdateAsync(TodoItem item, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
```

| Pattern | Why |
|---|---|
| `Task<...>` return | Database calls are async I/O — returning `Task` avoids blocking the thread. |
| `CancellationToken` | Lets the caller cancel (e.g. if the HTTP request is aborted). `= default` makes it optional. |
| `TodoItem?` | Returns `null` instead of throwing when not found — the caller decides how to handle it. |

### 3.6 PaginatedResult

A generic wrapper for paginated data, independent of any framework:

```csharp
public class PaginatedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public PaginatedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }
}
```

`TotalPages` is a **computed property** — it has no backing field and is calculated every time it is accessed. The `=> expression` syntax is called an expression-bodied member.

---

## 4. Infrastructure Layer

The infrastructure layer implements the abstractions from Domain using Entity Framework Core and PostgreSQL.

### 4.1 DbContext

The `DbContext` is EF Core's gateway to the database. It represents a session and provides `DbSet<T>` properties for each table:

```csharp
public class TodoListDbContext(DbContextOptions<TodoListDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<TodoListEntity> TodoLists => Set<TodoListEntity>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TodoListDbContext).Assembly);
    }
}
```

| Concept | Explanation |
|---|---|
| **Primary constructor** `(DbContextOptions<...> options)` | C# 12 feature — constructor parameters become available to the entire class body. No need for a separate field. |
| **`DbSet<T>` via `Set<T>()`** | Expression-bodied properties that return the EF set for querying and saving. |
| **`ApplyConfigurationsFromAssembly`** | Scans the assembly for all classes implementing `IEntityTypeConfiguration<T>` and applies them. No need to register each one manually. |

### 4.2 Entity Configurations (Fluent API)

Instead of scattering data annotations (`[Required]`, `[MaxLength]`) across entities, all database schema details live in dedicated configuration classes. This keeps entities clean.

```csharp
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

        builder.Property(t => t.IsCompleted).HasDefaultValue(false);
        builder.Property(t => t.IsImportant).HasDefaultValue(false);
        builder.Property(t => t.IsInMyDay).HasDefaultValue(false);

        // Owned value object — stored as columns in TodoItems table
        builder.OwnsOne(t => t.Recurrence, recurrence =>
        {
            recurrence.Property(r => r.Type).HasColumnName("RecurrenceType");
            recurrence.Property(r => r.Interval).HasColumnName("RecurrenceInterval");
            recurrence.Property(r => r.EndDate).HasColumnName("RecurrenceEndDate");
        });

        // Optional relationship: a task may belong to no list
        builder.HasOne(t => t.TodoList)
            .WithMany(l => l.Items)
            .HasForeignKey(t => t.TodoListId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Many-to-many: EF Core creates the join table automatically
        builder.HasMany(t => t.Categories)
            .WithMany()
            .UsingEntity("TodoItemCategory");

        // Indexes for common query patterns
        builder.HasIndex(t => t.TodoListId);
        builder.HasIndex(t => t.IsCompleted);
        builder.HasIndex(t => t.DueDate);
        builder.HasIndex(t => t.IsImportant);
        builder.HasIndex(t => t.IsInMyDay);
    }
}
```

| Fluent API call | What it does |
|---|---|
| `ToTable("TodoItems")` | Explicitly names the database table. |
| `HasKey(t => t.Id)` | Declares the primary key. |
| `.IsRequired().HasMaxLength(200)` | Column is NOT NULL with a max of 200 characters. |
| `.HasDefaultValue(false)` | Database-side default. New rows get `false` without the app sending a value. |
| `OwnsOne(...)` | Maps a value object's properties as columns in the *same table* (no separate table). |
| `.IsRequired(false)` | The FK column is nullable — the relationship is optional. |
| `.OnDelete(DeleteBehavior.SetNull)` | When the parent list is deleted, set `TodoListId` to NULL instead of deleting the task. |
| `.UsingEntity("TodoItemCategory")` | Names the auto-generated join table for the many-to-many. |
| `HasIndex(...)` | Creates a database index for faster filtering/sorting on that column. |

### 4.3 Repositories

Repositories implement the domain abstractions using EF Core's `DbContext`. Here is `TodoItemRepository` as an example:

```csharp
public class TodoItemRepository(TodoListDbContext context) : ITodoItemAbstraction
{
    public async Task<TodoItem?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await context.TodoItems
            .Include(t => t.Categories)
            .Include(t => t.TodoList)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<PaginatedResult<TodoItem>> GetFilteredAsync(
        bool? isInMyDay, bool? hasDueDate, bool? isImportant,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.TodoItems
            .Include(t => t.Categories)
            .Include(t => t.TodoList)
            .AsQueryable();

        if (isInMyDay.HasValue)
            query = query.Where(t => t.IsInMyDay == isInMyDay.Value);
        if (hasDueDate.HasValue)
            query = hasDueDate.Value
                ? query.Where(t => t.DueDate != null)
                : query.Where(t => t.DueDate == null);
        if (isImportant.HasValue)
            query = query.Where(t => t.IsImportant == isImportant.Value);

        query = query.OrderBy(t => t.Id);
        return await query.ToPaginatedResultAsync(page, pageSize, ct);
    }

    public async Task AddAsync(TodoItem item, CancellationToken ct = default)
    {
        await context.TodoItems.AddAsync(item, ct);
        await context.SaveChangesAsync(ct);
    }

    // ... Update, Delete follow the same pattern
}
```

Key EF Core concepts used:

| Concept | Explanation |
|---|---|
| **`Include()`** | Eager loading — tells EF to JOIN related data in the same query. Without it, `item.Categories` would be `null`. |
| **`FirstOrDefaultAsync()`** | Returns the first match, or `null` if none found. The `Async` suffix means it returns `Task<T?>`. |
| **`AsQueryable()`** | Keeps the query as an `IQueryable` so additional `.Where()` filters are added to the SQL, not run in memory. |
| **`OrderBy()` before pagination** | SQL requires an ORDER BY when using OFFSET/FETCH (Skip/Take). Without it, PostgreSQL warns that results are non-deterministic. |
| **`SaveChangesAsync()`** | Flushes all tracked changes to the database in a single transaction. |

### 4.4 Pagination Extension

A reusable extension method that turns any `IQueryable<T>` into a `PaginatedResult<T>`:

```csharp
public static class QueryablePaginationExtensions
{
    public static async Task<PaginatedResult<T>> ToPaginatedResultAsync<T>(
        this IQueryable<T> source, int page, int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await source.CountAsync(ct);
        var items = await source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResult<T>(items, totalCount, page, pageSize);
    }
}
```

| Detail | Why |
|---|---|
| **`this IQueryable<T>`** | The `this` keyword makes it an **extension method** — you call it as `query.ToPaginatedResultAsync(...)`. |
| **`Math.Max(1, page)`** | Guards against page 0 or negative. |
| **`Math.Clamp(pageSize, 1, 100)`** | Prevents requesting 0 items or an unreasonably large page. |
| **Two queries** | One `COUNT(*)` for total, one `SELECT ... OFFSET ... LIMIT` for the page. Both are translated to SQL by EF Core. |

### 4.5 DTOs (Data Transfer Objects)

DTOs define the shape of data that crosses the API boundary. They are `record` types — immutable, concise, and provide value equality.

```csharp
// What the client sends to create a task
public record CreateTodoItemRequest(
    string Title,
    string? Description,
    int? TodoListId,
    DateTime? DueDate,
    bool IsImportant,
    bool IsInMyDay,
    RecurrenceDto? Recurrence,
    IReadOnlyList<int> CategoryIds);

// What the API sends back
public record TodoItemResponse(
    int Id, string Title, string? Description, bool IsCompleted,
    DateTime? DueDate, bool IsImportant, bool IsInMyDay,
    DateTime CreatedAt, DateTime? CompletedAt,
    RecurrenceDto? Recurrence, int? TodoListId,
    IReadOnlyList<CategorySummaryDto> Categories);
```

**Why separate DTOs from entities?**

| Without DTOs | With DTOs |
|---|---|
| Entity changes break the API contract | API shape is independent of the database schema |
| Sensitive/internal fields get exposed | You control exactly what the client sees |
| Circular references cause JSON issues | DTOs are flat and serialization-safe |

### 4.6 Custom Exceptions

Two custom exceptions express domain-level error conditions:

```csharp
public class NotFoundException : Exception
{
    public string ResourceName { get; }
    public object ResourceId { get; }

    public NotFoundException(string resourceName, object resourceId)
        : base($"{resourceName} with id '{resourceId}' was not found.")
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }
}
```

```csharp
public class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}
```

These are thrown by command handlers and caught by the middleware (see section 5.3).

### 4.7 Dependency Injection Registration

A single extension method registers all infrastructure services:

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TodoListDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ITodoItemAbstraction, TodoItemRepository>();
        services.AddScoped<ITodoListAbstraction, TodoListRepository>();
        services.AddScoped<ICategoryAbstraction, CategoryRepository>();

        return services;
    }
}
```

| Lifetime | Meaning |
|---|---|
| **`AddScoped`** | One instance per HTTP request. All classes that inject `ITodoItemAbstraction` within the same request get the same `TodoItemRepository` instance — and therefore the same `DbContext` and change tracker. |
| **`AddDbContext`** | Scoped by default. Creates one `TodoListDbContext` per request and disposes it at the end. |

---

## 5. API Layer

### 5.1 Program.cs — The Entry Point

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Middleware pipeline (order matters!)
app.UseExceptionHandling();
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Development-only: Swagger UI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapGet("/swagger", () => Results.Content(/* Swagger UI HTML */, "text/html"));
}

app.Run();
```

**Middleware pipeline order matters.** The request flows top-to-bottom through each middleware. Exception handling is first so it catches errors from everything below it.

### 5.2 Controllers — Thin HTTP Layer

Controllers receive HTTP requests, create a MediatR command or query, and return the result. They contain no business logic.

```csharp
[ApiController]
[Route("api/[controller]")]
public class TodoItemsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetFiltered(
        [FromQuery] bool? myDay,
        [FromQuery] bool? planned,
        [FromQuery] bool? important,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetTodoItemsQuery(myDay, planned, important, page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTodoItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new CreateTodoItemCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
```

| Attribute / Pattern | Explanation |
|---|---|
| `[ApiController]` | Enables automatic model validation, `[FromBody]` inference, and problem details for 400 responses. |
| `[Route("api/[controller]")]` | `[controller]` is replaced by the class name minus "Controller" → `/api/todoitems`. |
| `[FromQuery]` | Binds from URL query string: `/api/todoitems?myDay=true&page=2`. |
| `[FromBody]` | Binds from the JSON request body. |
| **Primary constructor** `(IMediator mediator)` | Dependency injection — ASP.NET Core resolves `IMediator` and passes it in. |
| `CreatedAtAction(...)` | Returns HTTP 201 with a `Location` header pointing to the new resource's GET endpoint. |

### 5.3 MediatR — Commands and Queries (CQRS)

MediatR implements the **mediator pattern**: instead of controllers calling repositories directly, they send a message (command or query) and a handler processes it.

**Why?** It decouples the "what" (HTTP endpoint) from the "how" (business logic). Each handler is a small, focused class.

#### Command Example: Create a TodoItem

```csharp
// The message (what to do)
public record CreateTodoItemCommand(CreateTodoItemRequest Request)
    : IRequest<TodoItemResponse>;

// The handler (how to do it)
public class CreateTodoItemCommandHandler(
    ITodoItemAbstraction todoItems,
    ITodoListAbstraction todoLists,
    ICategoryAbstraction categories)
    : IRequestHandler<CreateTodoItemCommand, TodoItemResponse>
{
    public async Task<TodoItemResponse> Handle(
        CreateTodoItemCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate
        if (string.IsNullOrWhiteSpace(request.Request.Title))
            throw new ValidationException(new Dictionary<string, string[]>
                { ["Title"] = ["Title is required."] });

        // 2. Resolve optional TodoList
        int? todoListId = request.Request.TodoListId;
        if (todoListId.HasValue && todoListId.Value != 0)
        {
            var list = await todoLists.GetByIdAsync(todoListId.Value, cancellationToken);
            if (list == null)
                throw new NotFoundException("TodoList", todoListId.Value);
        }
        else
            todoListId = null;

        // 3. Resolve categories (silently skip unknown IDs)
        var categoryEntities = new List<Category>();
        var categoryIds = request.Request.CategoryIds ?? [];
        if (categoryIds.Count != 0)
        {
            var allCategories = await categories.GetAllAsync(cancellationToken);
            var lookup = allCategories.ToDictionary(c => c.Id);
            foreach (var id in categoryIds)
                if (lookup.TryGetValue(id, out var cat))
                    categoryEntities.Add(cat);
        }

        // 4. Create and save
        var item = new TodoItem
        {
            Title = request.Request.Title,
            Description = request.Request.Description,
            TodoListId = todoListId,
            DueDate = request.Request.DueDate,
            IsImportant = request.Request.IsImportant,
            IsInMyDay = request.Request.IsInMyDay,
            CreatedAt = DateTime.UtcNow,
            Recurrence = /* map from DTO if present */,
            Categories = categoryEntities
        };

        await todoItems.AddAsync(item, cancellationToken);
        return item.ToResponse();
    }
}
```

#### Query Example: Get Filtered TodoItems (with pagination)

```csharp
public record GetTodoItemsQuery(
    bool? IsInMyDay, bool? HasDueDate, bool? IsImportant,
    int Page = 1, int PageSize = 20)
    : IRequest<PaginatedResult<TodoItemResponse>>;

public class GetTodoItemsQueryHandler(ITodoItemAbstraction todoItems)
    : IRequestHandler<GetTodoItemsQuery, PaginatedResult<TodoItemResponse>>
{
    public async Task<PaginatedResult<TodoItemResponse>> Handle(
        GetTodoItemsQuery request, CancellationToken cancellationToken)
    {
        var result = await todoItems.GetFilteredAsync(
            request.IsInMyDay, request.HasDueDate, request.IsImportant,
            request.Page, request.PageSize, cancellationToken);

        return new PaginatedResult<TodoItemResponse>(
            result.Items.Select(i => i.ToResponse()).ToList(),
            result.TotalCount, result.Page, result.PageSize);
    }
}
```

#### MediatR Registration

```csharp
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
```

This scans the API assembly and registers every `IRequestHandler<,>` implementation automatically. No manual registration per handler needed.

### 5.4 Mapping — Entity to DTO

Extension methods convert domain entities to response DTOs:

```csharp
internal static class TodoItemMapping
{
    public static TodoItemResponse ToResponse(this TodoItem item) =>
        new(
            item.Id, item.Title, item.Description, item.IsCompleted,
            item.DueDate, item.IsImportant, item.IsInMyDay,
            item.CreatedAt, item.CompletedAt,
            item.Recurrence is null ? null :
                new RecurrenceDto(item.Recurrence.Type,
                    item.Recurrence.Interval, item.Recurrence.EndDate),
            item.TodoListId,
            item.Categories.Select(c =>
                new CategorySummaryDto(c.Id, c.Name, c.Color)).ToList()
        );
}
```

`internal static` means:
- **`internal`** — only visible within the same project (API). Infrastructure and Domain cannot see it.
- **`static`** — no instance needed. Extension methods must be in a static class.

### 5.5 Exception Handling Middleware

A single middleware catches all unhandled exceptions and returns consistent JSON error responses:

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);       // call the rest of the pipeline
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            var (statusCode, json) = GetErrorResponse(ex);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
    }

    private static (int statusCode, string json) GetErrorResponse(Exception ex)
    {
        var name = ex.GetType().Name;
        if (name.EndsWith("NotFoundException"))
            return (404, /* { type: "NotFound", message: "..." } */);
        if (name.EndsWith("ValidationException"))
            return (422, /* { type: "Validation", message: "...", errors: {...} } */);
        return (500, /* { type: "Error", message: "An unexpected error occurred." } */);
    }
}
```

**How middleware works:**

1. ASP.NET Core calls `InvokeAsync` for every request.
2. `await _next(context)` passes the request to the next middleware (and eventually to the controller).
3. If anything downstream throws, the `catch` block handles it.
4. The middleware is registered first in the pipeline (`app.UseExceptionHandling()`), so it wraps *everything*.

| Exception | HTTP Status | When |
|---|---|---|
| `NotFoundException` | 404 | Entity not found by ID |
| `ValidationException` | 422 | Invalid input data |
| Any other | 500 | Unexpected server error (details hidden from client) |

### 5.6 Extension Method for Middleware Registration

```csharp
public static class WebApplicationExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
```

This keeps `Program.cs` clean — `app.UseExceptionHandling()` reads better than `app.UseMiddleware<ExceptionHandlingMiddleware>()`.

---

## 6. Request Lifecycle — End to End

Here is what happens when a client sends `POST /api/todoitems`:

```
Client
  │
  ▼
┌─────────────────────────────────┐
│ 1. ExceptionHandlingMiddleware  │  Wraps everything in try/catch
├─────────────────────────────────┤
│ 2. Routing + Model Binding      │  Matches route, deserializes JSON → CreateTodoItemRequest
├─────────────────────────────────┤
│ 3. TodoItemsController.Create() │  Creates CreateTodoItemCommand, sends via MediatR
├─────────────────────────────────┤
│ 4. CreateTodoItemCommandHandler │  Validates, resolves relations, builds entity
├─────────────────────────────────┤
│ 5. TodoItemRepository.AddAsync()│  Adds to DbContext, calls SaveChangesAsync
├─────────────────────────────────┤
│ 6. PostgreSQL                   │  INSERT INTO "TodoItems" ...
├─────────────────────────────────┤
│ 7. Response flows back up       │  Entity → ToResponse() → DTO → JSON
└─────────────────────────────────┘
  │
  ▼
Client receives 201 Created + JSON body
```

If the handler throws `NotFoundException`, the middleware catches it and returns:

```json
{ "type": "NotFound", "message": "TodoList with id '99' was not found." }
```

with HTTP 404 — the controller never sees the exception.

---

## 7. Key Patterns & Concepts

### 7.1 Dependency Injection (DI)

Every class declares its dependencies in its constructor. ASP.NET Core resolves them automatically:

```csharp
// Controller asks for IMediator
public class TodoItemsController(IMediator mediator) : ControllerBase

// Handler asks for repository abstractions
public class CreateTodoItemCommandHandler(
    ITodoItemAbstraction todoItems,
    ITodoListAbstraction todoLists,
    ICategoryAbstraction categories) : IRequestHandler<...>

// Repository asks for DbContext
public class TodoItemRepository(TodoListDbContext context) : ITodoItemAbstraction
```

The DI container resolves the entire chain: Controller → MediatR → Handler → Repository → DbContext.

### 7.2 CQRS (Command Query Responsibility Segregation)

Commands change state (Create, Update, Delete). Queries read state (Get, GetAll, GetFiltered). Each is a separate class with a single handler. Benefits:

- Each handler does one thing and is easy to test.
- Commands and queries can evolve independently.
- Adding cross-cutting concerns (logging, caching) is easy via MediatR pipeline behaviors.

### 7.3 Repository Pattern

Repositories hide data access details behind an interface. The domain defines the contract; infrastructure fulfills it. The API layer never touches `DbContext` directly.

### 7.4 Extension Methods

Used throughout for cleaner APIs:

```csharp
// Called as: services.AddInfrastructure(config)
public static IServiceCollection AddInfrastructure(this IServiceCollection services, ...)

// Called as: query.ToPaginatedResultAsync(page, pageSize)
public static Task<PaginatedResult<T>> ToPaginatedResultAsync<T>(this IQueryable<T> source, ...)

// Called as: item.ToResponse()
public static TodoItemResponse ToResponse(this TodoItem item)
```

The `this` keyword on the first parameter turns a regular static method into something that looks like an instance method on that type.

### 7.5 Primary Constructors (C# 12)

```csharp
// Old way
public class TodoItemRepository : ITodoItemAbstraction
{
    private readonly TodoListDbContext _context;
    public TodoItemRepository(TodoListDbContext context) { _context = context; }
}

// C# 12 primary constructor
public class TodoItemRepository(TodoListDbContext context) : ITodoItemAbstraction
{
    // 'context' is available directly — no field needed
}
```

### 7.6 Records

```csharp
// Positional record — constructor parameters become read-only properties
public record CreateTodoItemRequest(string Title, string? Description, ...);

// This one line generates: properties, constructor, Equals, GetHashCode, ToString, Deconstruct
```

### 7.7 Nullable Reference Types

C# uses `?` to express optionality:

| Syntax | Meaning |
|---|---|
| `string Title` | Never null — compiler warns if you try to assign null |
| `string? Description` | Explicitly nullable — may be null |
| `int? TodoListId` | Nullable value type — either has an int value or is null |
| `TodoList? TodoList` | Navigation may be null (no list assigned) |
| `= null!` | "Trust me, this won't be null at runtime" (used for EF-populated navigations) |

---

## 8. API Endpoints Reference

### TodoItems — `/api/todoitems`

| Method | Route | Description | Request | Response |
|---|---|---|---|---|
| GET | `/` | List with filters & pagination | `?myDay=true&page=1&pageSize=20` | `PaginatedResult<TodoItemResponse>` |
| GET | `/{id}` | Get by ID | — | `TodoItemResponse` or 404 |
| POST | `/` | Create | `CreateTodoItemRequest` body | 201 + `TodoItemResponse` |
| PUT | `/{id}` | Update | `UpdateTodoItemRequest` body | `TodoItemResponse` |
| DELETE | `/{id}` | Delete | — | 204 No Content |
| POST | `/{id}/complete` | Mark complete/incomplete | `{ "complete": true }` | `TodoItemResponse` |
| POST | `/{id}/myday` | Toggle My Day | — | `TodoItemResponse` |

### TodoLists — `/api/todolists`

| Method | Route | Description |
|---|---|---|
| GET | `/` | Get all lists (with item count) |
| GET | `/{id}` | Get list with its items |
| POST | `/` | Create list |
| PUT | `/{id}` | Update list name |
| DELETE | `/{id}` | Delete list (tasks' `TodoListId` set to null) |

### Categories — `/api/categories`

| Method | Route | Description |
|---|---|---|
| GET | `/` | Get all categories |
| GET | `/{id}` | Get by ID |
| POST | `/` | Create category |
| PUT | `/{id}` | Update category |
| DELETE | `/{id}` | Delete category |

---

## Summary

| Layer | Responsibility | Key Technologies |
|---|---|---|
| **Domain** | Defines entities, relationships, value objects, and repository contracts | Pure C# (no packages) |
| **Infrastructure** | Implements data access, schema configuration, pagination, exceptions | EF Core, Npgsql (PostgreSQL) |
| **API** | HTTP endpoints, request handling, validation, error mapping | ASP.NET Core, MediatR |

| Pattern | Where Used |
|---|---|
| Clean Architecture | Project structure and dependency direction |
| Repository Pattern | Domain abstractions → Infrastructure implementations |
| CQRS via MediatR | Commands/Queries in Features folder |
| DTOs | Infrastructure Dtos folder — separate API shape from database shape |
| Middleware Pipeline | Exception handling wraps the entire request |
| Extension Methods | DI registration, pagination, entity-to-DTO mapping |
| Fluent API Configuration | EF Core entity configurations (no data annotations on entities) |
