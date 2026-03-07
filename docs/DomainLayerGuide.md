# Domain Layer — Structure & Model Design Guide

This document explains the design decisions behind the `TodoList.Domain` project and teaches the C# concepts used throughout.

---

## Project Structure

```
src/TodoList.Domain/
├── Entities/
│   ├── TodoItem.cs
│   ├── TodoList.cs
│   └── Category.cs
├── ValueObjects/
│   └── RecurrencePattern.cs
├── Enums/
│   └── RecurrenceType.cs
├── Abstractions/
│   ├── ITodoItemAbstraction.cs
│   ├── ITodoListAbstraction.cs
│   └── ICategoryAbstraction.cs
└── TodoList.Domain.csproj
```

Each folder has a clear responsibility:

| Folder | Purpose |
|---|---|
| `Entities/` | Core data models that get stored in the database. Each has an `Id` primary key. |
| `ValueObjects/` | Small objects that describe something but have no identity of their own. |
| `Enums/` | Named constants — a fixed set of allowed values. |
| `Abstractions/` | Data-access abstractions (interfaces) that define *what* operations exist, without specifying *how* they are implemented. |

---

## Why This Structure?

This follows **layered architecture** (also called Clean Architecture):

```
Domain  ←  Infrastructure  ←  Api
```

- **Domain** knows nothing about databases or HTTP. It only defines *what the data looks like* and *what operations are available* (via abstractions).
- **Infrastructure** implements those abstractions using EF Core and a real database.
- **Api** handles HTTP requests, calls the infrastructure, and returns responses.

The benefit: you can change the database (e.g. switch from SQL Server to PostgreSQL) without touching the domain layer at all.

---

## Entities — Simple POCO Models

POCO stands for **Plain Old CLR Object**. It means a class with just properties — no special base class, no framework dependencies, no constructor logic.

### TodoItem

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

    public int TodoListId { get; set; }          // foreign key
    public TodoList TodoList { get; set; } = null!; // navigation property
    public List<Category> Categories { get; set; } = []; // many-to-many
}
```

Key concepts:

- **`int Id`** — primary key, auto-incremented by the database.
- **`string?` and `DateTime?`** — the `?` means nullable. `Description` can be `null` (optional), while `Title` cannot.
- **`= string.Empty`** — default value, so `Title` is never `null` even without a constructor.
- **`= null!`** — tells the compiler "I know this looks null, but EF Core will populate it." The `!` suppresses the nullable warning.
- **`= []`** — shorthand for `new List<>()`. Initializes an empty list.

### TodoList

```csharp
public class TodoList
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<TodoItem> Items { get; set; } = [];
}
```

`Items` is a **navigation property** — EF Core uses it to represent the one-to-many relationship between `TodoList` and `TodoItem`.

### Category

```csharp
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}
```

The simplest entity. `Color` is optional (nullable string), meant for UI hex colors like `"#FF5733"`.

---

## Relationships

```
TodoList  ──1:many──>  TodoItem  ──many:many──>  Category
                       TodoItem  ──0..1──>  RecurrencePattern
```

### One-to-Many: TodoList → TodoItem

Each `TodoItem` belongs to exactly one `TodoList`. This is expressed with:

```csharp
// On TodoItem:
public int TodoListId { get; set; }        // FK column in the database
public TodoList TodoList { get; set; }      // navigation to the parent
```

```csharp
// On TodoList:
public List<TodoItem> Items { get; set; }   // navigation to children
```

EF Core automatically detects this convention: a property named `TodoListId` + a property of type `TodoList` = foreign key relationship.

### Many-to-Many: TodoItem ↔ Category

A todo item can have multiple categories, and a category can apply to multiple items. In EF Core 5+, you can model this without a join entity:

```csharp
// On TodoItem:
public List<Category> Categories { get; set; } = [];
```

EF Core creates the join table in the database automatically.

### Owned Type: TodoItem → RecurrencePattern

`RecurrencePattern` is a **value object** — it will be stored as columns *within* the `TodoItems` table (not a separate table). EF Core calls this an "owned type," configured in Phase 2.

---

## Value Objects — `record` vs `class`

```csharp
public record RecurrencePattern
{
    public RecurrenceType Type { get; set; }
    public int Interval { get; set; }
    public DateTime? EndDate { get; set; }
}
```

### What is a `record`?

A `record` is a C# type that gets **value-based equality** for free:

```csharp
var a = new RecurrencePattern { Type = RecurrenceType.Daily, Interval = 1 };
var b = new RecurrencePattern { Type = RecurrenceType.Daily, Interval = 1 };

a == b  // true — records compare by property values
```

With a regular `class`, `a == b` would be `false` because classes compare by reference (are they the same object in memory?).

### When to use `record`

Use `record` for types that:
- Have no identity (`Id`) of their own
- Are defined entirely by their property values
- Could be swapped out for another instance with the same values

`RecurrencePattern` fits perfectly — "repeat daily every 1 day" is the same concept no matter which object represents it.

---

## Enums

```csharp
public enum RecurrenceType
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Yearly = 4
}
```

An `enum` is a set of named integer constants. Using explicit values (`= 1, = 2...`) is good practice because:
- The database stores the integer, not the name
- Adding new values later won't shift existing ones
- Starting at 1 (not 0) makes `0` an implicit "unset/invalid" value you can detect

---

## Abstractions

```csharp
public interface ITodoItemAbstraction
{
    Task<TodoItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<TodoItem>> GetByListIdAsync(int todoListId, CancellationToken cancellationToken = default);
    Task AddAsync(TodoItem item, CancellationToken cancellationToken = default);
    Task UpdateAsync(TodoItem item, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
```

### What is an abstraction?

An **abstraction** (here, a C# interface) is a **contract**. It says *what* operations exist, but not *how* they work:

- `ITodoItemAbstraction` is declared in the **Domain** layer (no database dependency)
- `TodoItemRepository` (the concrete implementation using EF Core) will live in the **Infrastructure** layer and implement `ITodoItemAbstraction`

The Domain layer defines abstractions; Infrastructure implements them. This decouples business logic from data access technology.

### Key patterns in the abstraction

| Pattern | Explanation |
|---|---|
| `Task<...>` return type | All methods are **async** — they return a `Task` because database calls are I/O operations that shouldn't block the thread. |
| `CancellationToken` parameter | Allows the caller to cancel a long-running operation (e.g. if the HTTP request is aborted). `= default` makes it optional. |
| `TodoItem?` (nullable return) | `GetByIdAsync` returns `null` if the item doesn't exist, instead of throwing an exception. |

---

## Where Does Validation Live?

**Not in the entities.** The entities are intentionally "dumb" data containers. Validation is handled in Phase 3 at the **command handler level** (MediatR handlers), using either:

- **FluentValidation** — a library for building validation rules as separate classes
- **Manual checks** — `if` statements in the handler before saving

This keeps entities simple and testable, and keeps validation rules in one place close to the API boundary.

---

## Summary Table

| Concept | C# Feature | Example in This Project |
|---|---|---|
| Data model | `class` with properties | `TodoItem`, `TodoList`, `Category` |
| Value object | `record` | `RecurrencePattern` |
| Named constants | `enum` | `RecurrenceType` |
| Data access abstraction | `interface` | `ITodoItemAbstraction` |
| Optional value | `?` (nullable) | `string? Description`, `DateTime? DueDate` |
| Default value | `= expression` | `string.Empty`, `[]`, `null!` |
| Async operation | `Task<T>` | All abstraction methods |
| Cancellation support | `CancellationToken` | All abstraction method parameters |
