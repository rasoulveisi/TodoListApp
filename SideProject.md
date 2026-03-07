# TodoList Backend — Project Proposal

## Objective

Build a backend application inspired by Microsoft To Do, implementing core task-management features in a clean, layered .NET architecture. The project serves as both a production-grade deliverable and a structured learning path covering C#, ASP.NET Core, Entity Framework Core, and modern software design patterns.

---

## Scope

The application exposes a RESTful API that supports:

- **Task management** — create, update, delete, and complete tasks (one-time and recurring)
- **List management** — group tasks into named lists
- **Categorization** — assign categories to tasks
- **Smart filters** — surface tasks through built-in views:
  - **My Day** — tasks flagged for today
  - **Planned** — tasks with a due date
  - **Important** — tasks marked as important

Out of scope (initial release): authentication/authorization, real-time notifications, and a frontend client.

---

## Domain Model Overview

| Entity / Value Object | Key Properties | Relationships |
|---|---|---|
| **TodoItem** | Id, Title, Description, IsCompleted, DueDate, IsImportant, IsInMyDay, CreatedAt, CompletedAt, Recurrence | Belongs to one TodoList; has many Categories; owns one optional RecurrencePattern |
| **TodoList** | Id, Name, CreatedAt | Contains many TodoItems |
| **Category** | Id, Name, Color | Assigned to many TodoItems |
| **RecurrencePattern** (value object) | Type, Interval, EndDate | Owned by TodoItem |
| **RecurrenceType** (enum) | Daily, Weekly, Monthly, Yearly | Used by RecurrencePattern |

### Key Business Rules

- Completing a recurring task should generate the next occurrence based on its RecurrencePattern.
- A task can belong to exactly one list but may have multiple categories.
- "My Day" is a daily, user-toggled flag — not persisted across days automatically.

---

## Architecture

The solution follows a **layered architecture** with three main projects:

```
TodoList.slnx
├── src/
│   ├── TodoList.Domain          — Entities, value objects, enums, abstractions
│   ├── TodoList.Infrastructure  — EF Core DbContext, implementations of Domain abstractions, DTOs, migrations
│   └── TodoList.Api             — Controllers/endpoints, MediatR handlers, middleware, DI wiring
└── tests/
    └── TodoList.Domain.Tests    — Unit tests for domain logic
```

**Dependency flow:** Api → Infrastructure → Domain (the Domain project has zero external dependencies).

---

## Phases

### Phase 1 — Domain Layer (`TodoList.Domain`)

Implement the core domain model in pure C# with no external dependencies.

**Deliverables:**
- Entities: `TodoItem`, `TodoList`, `Category`
- Value objects: `RecurrencePattern`
- Enums: `RecurrenceType`
- Abstractions: `ITodoItemAbstraction`, `ITodoListAbstraction`, `ICategoryAbstraction`
- Unit tests covering entity business logic and value object equality

**Learning Outcomes:**
- Class, record, and interface definitions in C#
- Object-oriented modeling of real-world concepts
- Unit testing with xUnit
- Domain-Driven Design fundamentals (entities vs. value objects)

---

### Phase 2 — Infrastructure Layer (`TodoList.Infrastructure`)

Connect the domain to a relational database using Entity Framework Core.

**Deliverables:**
- `TodoListDbContext` with Fluent API entity configurations
- Implementations of Domain abstractions (e.g. TodoItemRepository implementing ITodoItemAbstraction) backed by EF Core
- DTOs for API responses and requests
- Generic pagination support (`PaginatedResult<T>`)
- Domain-specific exceptions (`NotFoundException`, `ValidationException`)
- CQRS preparation: `ICommand` / `IQuery<TResult>` interfaces
- Initial EF Core migration

**Learning Outcomes:**
- EF Core: DbContext, Fluent API, migrations, query creation
- `IQueryable` vs. `IEnumerable` (deferred execution)
- Implementing the data-access abstractions
- DTO projection and pagination patterns
- Exception handling strategies
- Command Query Responsibility Segregation (CQRS) concepts

---

### Phase 3 — API / Presenter Layer (`TodoList.Api`)

Expose the application's functionality through RESTful endpoints.

**Deliverables:**
- MediatR command and query handlers for all operations
- Controllers: `TodoItemsController`, `TodoListsController`, `CategoriesController`
- Global exception-handling middleware
- Dependency injection wiring via extension methods
- Request validation (FluentValidation or manual)
- Structured logging with `ILogger<T>`

**Learning Outcomes:**
- ASP.NET Core controllers and routing
- Dependency injection and service lifetimes
- Middleware pipeline
- Mediator pattern (CQRS with MediatR)
- Request validation
- Structured logging
- Consumer/integration event patterns

---

## Deliverables Summary

1. A fully functional, deployable .NET backend API
2. A clean, layered solution with clear separation of concerns
3. Unit tests for domain business logic
4. EF Core migrations ready for database provisioning
5. API documentation via Swagger/OpenAPI (auto-generated)

---

## Constraints & Conventions

- Target framework: **.NET 10**
- Database: SQL Server (or SQLite for local development)
- Testing framework: xUnit
- All domain logic must remain in `TodoList.Domain` with zero infrastructure dependencies
- Abstractions defined in the Domain; implementations in Infrastructure
