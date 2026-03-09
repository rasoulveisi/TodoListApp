# Building a Production-Ready REST API with C# and Clean Architecture

## What This Document Covers

This is a deep-dive into the TodoList API, a real-world task management backend built with ASP.NET Core, Entity Framework Core, and PostgreSQL. The project demonstrates how professional C# developers organize a backend application using Clean Architecture, the repository pattern, CQRS with MediatR, and modern C# language features.

The application manages three core resources: todo items (tasks), todo lists (containers that group tasks), and categories (labels/tags on tasks). It exposes a RESTful HTTP API with full CRUD operations, pagination, filtering, and structured error handling.

This document is written for learners who want to understand not just what each piece of the codebase does, but why it was designed that way and what C# concepts make it work.

---

## Part 1: The Big Picture — Why Clean Architecture?

Imagine you are building a house. You would not wire the electricity into the walls in a way that means replacing a light switch requires tearing down an entire wall. You would design the wiring so that each component is replaceable without disrupting the rest of the structure.

Clean Architecture applies this same idea to software. The TodoList API is split into three separate projects, each with a clear role and a strict rule about what it is allowed to depend on.

The innermost layer is called the Domain. It contains the pure business models — what a task looks like, what a list looks like, what a category looks like. The domain has absolutely zero external dependencies. It does not know about databases, does not know about HTTP, and does not reference any NuGet packages at all. It is pure C# and nothing else.

The middle layer is called Infrastructure. It implements the actual data access using Entity Framework Core and PostgreSQL. It depends on the domain layer because it needs to know what a TodoItem or a Category looks like. But the domain never depends on infrastructure — the dependency only points inward.

The outermost layer is the API. It handles HTTP requests, wires everything together using dependency injection, and exposes the endpoints that clients call. It depends on both the domain and infrastructure layers.

The key insight is that dependencies always point inward, toward the domain. This means you could swap PostgreSQL for SQL Server, or swap Entity Framework for Dapper, and you would only change the infrastructure layer. The domain and API layers would not need to change at all. You could even replace ASP.NET Core with a different web framework, and the domain and infrastructure layers would remain untouched.

This is not theoretical — it is a practical benefit that makes the codebase easier to test, easier to maintain, and easier to extend.

---

## Part 2: The Domain Layer — The Heart of the Application

The domain layer is intentionally the simplest part of the codebase. It defines three things: entities, value objects, and abstractions.

### Entities: What Gets Stored in the Database

An entity is a C# class that represents a row in a database table. The term "POCO" — Plain Old CLR Object — means these classes have no special base class, no framework dependencies, and no complex constructor logic. They are just data containers with properties.

The central entity is TodoItem. It represents a single task. A TodoItem has a title (which is required), an optional description, boolean flags for whether it is completed, important, or in the user's "My Day" view, optional due dates, and timestamps for when it was created and completed.

What makes TodoItem interesting is its relationships. A task can optionally belong to a TodoList — the word "optionally" is important here. The foreign key TodoListId is nullable, meaning a task can exist independently without belonging to any list. This was a deliberate design decision: not every task needs to be organized into a list.

A task can also have multiple categories, and a category can be applied to multiple tasks. This is a many-to-many relationship. In modern Entity Framework Core, you can model this simply by having a list of categories on the TodoItem entity. EF Core automatically creates the join table in the database — you never have to write a join entity class yourself.

Finally, a task can have a recurrence pattern — like "repeat daily" or "repeat every two weeks." This is modeled as a value object, which brings us to the next important concept.

### Value Objects: Identity Does Not Matter

A value object is something that is defined entirely by its properties, not by an identity. Consider the concept "repeat daily every one day." If you have two objects that both represent "repeat daily every one day," they are the same thing — it does not matter which object instance you are holding.

In C#, the way to express this is with a record instead of a class. When you declare something as a record, the compiler automatically generates value-based equality. Two record instances with the same property values are considered equal. Two class instances with the same property values are not — classes compare by reference, meaning "are these the exact same object in memory?"

The RecurrencePattern is declared as a record with three properties: the type of recurrence (daily, weekly, monthly, or yearly), the interval (every how many days, weeks, etc.), and an optional end date.

In the database, this value object does not get its own table. Instead, its properties are stored as columns directly inside the TodoItems table — columns named RecurrenceType, RecurrenceInterval, and RecurrenceEndDate. Entity Framework calls this an "owned type." It is a powerful way to keep related data together without creating unnecessary tables.

### Enums: A Fixed Set of Allowed Values

The recurrence types are modeled as an enum — Daily equals 1, Weekly equals 2, Monthly equals 3, Yearly equals 4. The explicit numbering starting at 1 is a deliberate choice. It means the value 0 is always "unset" or "invalid," which is useful for detecting when someone forgot to specify a value. The database stores the integer, not the text name, so having stable numbers prevents issues if you reorder or add new values later.

### Abstractions: Contracts Without Implementation

This is where the domain layer gets architecturally interesting. It defines interfaces — contracts that say "these operations must exist" without specifying how they work.

For example, the ITodoItemAbstraction interface declares methods like GetByIdAsync, GetAllAsync, AddAsync, UpdateAsync, and DeleteAsync. But it contains no implementation code at all. It is just a list of method signatures.

The crucial architectural point is that this interface lives in the domain layer, but the class that implements it lives in the infrastructure layer. This is called the Dependency Inversion Principle — the domain defines the contract, and the infrastructure fulfills it. The domain never needs to know that Entity Framework Core or PostgreSQL exists.

Every method in these interfaces returns a Task, which means they are asynchronous. Database calls involve I/O — sending a query over the network and waiting for results. Asynchronous methods let the application handle other requests while waiting, rather than blocking a thread. Every method also accepts a CancellationToken, which lets the system cancel an operation if, for example, the client disconnects before the query finishes.

### PaginatedResult: A Reusable Wrapper

When you have thousands of tasks, you do not want to return all of them at once. The PaginatedResult is a generic class that wraps a page of results along with metadata: the items on the current page, the total count of all matching items, the current page number, the page size, and a computed total number of pages. Because it lives in the domain layer with no framework dependencies, it can be reused anywhere in the application.

---

## Part 3: The Infrastructure Layer — Where Data Access Lives

The infrastructure layer is where abstract concepts meet concrete technology. It implements all the repository interfaces defined by the domain, using Entity Framework Core to talk to a PostgreSQL database.

### The DbContext: Your Gateway to the Database

Entity Framework Core uses a class called DbContext as the central point for all database operations. Think of it as a session — it tracks which entities have been loaded, which ones have been modified, and it knows how to translate C# operations into SQL queries.

The TodoListDbContext class declares three DbSet properties — one for TodoItems, one for TodoLists, and one for Categories. A DbSet represents a database table and provides methods for querying and saving data.

A noteworthy detail is the OnModelCreating method. Instead of applying configuration to each entity one at a time, it calls ApplyConfigurationsFromAssembly. This scans the entire assembly and automatically finds and applies every configuration class. When you add a new entity in the future, you just create a new configuration class and it gets picked up automatically — no registration step needed.

### Entity Configuration with the Fluent API

The project uses the Fluent API instead of data annotations to configure how entities map to database tables. This means the entities themselves stay clean — no attributes like Required or MaxLength cluttering up the property declarations. Instead, all database schema details live in dedicated configuration classes.

The TodoItemConfiguration class is the most complex one. It maps the Title property as required with a maximum length of 200 characters. It sets database-level defaults for boolean properties like IsCompleted, IsImportant, and IsInMyDay — all defaulting to false so that new rows get sensible defaults even if the application does not explicitly send a value.

The recurrence pattern is configured as an owned type using OwnsOne. This tells Entity Framework to store the RecurrencePattern's properties as columns in the TodoItems table, with custom column names like RecurrenceType, RecurrenceInterval, and RecurrenceEndDate.

The relationship between TodoItem and TodoList is configured as optional. The IsRequired(false) call makes the foreign key nullable, and OnDelete with SetNull means that if you delete a list, all its tasks survive — their TodoListId just gets set to null. This is different from the more common Cascade behavior, which would delete all tasks when their list is deleted.

The many-to-many relationship between TodoItem and Category is configured with a single call that names the join table TodoItemCategory. Entity Framework handles the rest — creating the join table, managing the foreign keys, and keeping everything in sync.

The configuration also creates database indexes on columns that are commonly used in filters: TodoListId, IsCompleted, DueDate, IsImportant, and IsInMyDay. Indexes make queries that filter on these columns significantly faster, especially as the table grows.

### Repositories: Implementing the Contracts

A repository class implements a domain interface using Entity Framework Core. The TodoItemRepository implements ITodoItemAbstraction and receives the DbContext through its constructor via dependency injection.

The GetByIdAsync method uses Include to eagerly load related categories and the parent todo list in a single database query. Without Include, those navigation properties would be null even if the data exists in the database — Entity Framework does not load related data automatically unless you tell it to.

The GetFilteredAsync method is particularly interesting because it demonstrates dynamic query building. It starts with the full set of todo items, then conditionally adds Where clauses based on which filter parameters are present. If the caller passes isInMyDay as true, a Where clause filters for tasks in My Day. If isImportant is passed, another Where clause is added. Each filter is optional and composes naturally.

Before pagination, the query adds an OrderBy clause. This is not optional — SQL databases require an ORDER BY when using OFFSET and LIMIT (which is what Skip and Take translate to). Without it, the database makes no guarantees about the order of results, and PostgreSQL will warn about non-deterministic paging.

The AddAsync method demonstrates the two-step save pattern that Entity Framework uses. First, you add the entity to the DbSet, which tells the change tracker about it. Then, you call SaveChangesAsync, which generates the INSERT statement, sends it to the database, and commits the transaction. Both steps are necessary.

### The Pagination Extension Method

The pagination logic is implemented as a reusable extension method. An extension method is a C# feature that lets you add methods to existing types without modifying them. By putting the "this" keyword before the first parameter (which is IQueryable), you can call the method as if it were a method on IQueryable itself — like writing "query.ToPaginatedResultAsync(page, pageSize)" instead of "PaginationHelper.Paginate(query, page, pageSize)."

The method performs two database queries: one to count the total number of matching records, and one to fetch just the current page using Skip and Take. It clamps the page number to a minimum of 1 and the page size to between 1 and 100, preventing misuse like requesting page negative-five or a page size of ten thousand.

### Data Transfer Objects: Separating API Shape from Database Shape

DTOs — Data Transfer Objects — define what data the client sends and receives. They are separate from entities for several important reasons.

First, the API contract should be stable. If you rename a database column or add an internal field, you do not want to break every client that depends on the API. DTOs give you a stable public interface that is independent of your internal database schema.

Second, entities can have circular references. A TodoItem has a TodoList, and a TodoList has a list of TodoItems. If you try to serialize an entity directly to JSON, you get an infinite loop. DTOs are designed to be flat and serialization-safe.

Third, security. You control exactly which fields the client can see. The entity might have internal fields — like soft-delete flags or audit data — that should never be exposed.

The project uses C# records for DTOs, which is ideal because records are concise (a single line can define a complete DTO with all its properties), immutable by default, and provide value-based equality.

### Custom Exceptions

The project defines two custom exception types: NotFoundException and ValidationException. NotFoundException carries the resource name and ID, generating messages like "TodoList with id 99 was not found." ValidationException carries a dictionary of field-level errors, like "Title" mapping to "Title is required."

These exceptions are not caught by individual controllers. Instead, they bubble up to a global middleware that translates them into consistent HTTP responses. This pattern eliminates repetitive try-catch blocks in every controller action.

---

## Part 4: The API Layer — HTTP Endpoints and Request Handling

The API layer is where HTTP requests enter the system and where all the other layers get wired together.

### Program.cs: The Application Entry Point

The entry point of an ASP.NET Core application is Program.cs. It follows a two-phase pattern: first you configure services (dependency injection registrations), then you configure the HTTP pipeline (middleware order).

The service configuration registers four things: the infrastructure services (DbContext and repositories) via an AddInfrastructure extension method, MediatR (which scans the assembly for command and query handlers), ASP.NET Core controllers, and OpenAPI documentation.

The middleware pipeline order matters. Exception handling is registered first because it needs to wrap everything else — if a controller throws, the exception middleware catches it. HTTPS redirection is only enabled outside of development, because local development typically uses HTTP only. Controllers are mapped last.

In development mode, the application also serves a Swagger UI page — a browser-based interface for testing the API interactively.

### Controllers: Thin and Focused

The controllers in this project follow an important principle: they are thin. A controller's only job is to receive an HTTP request, create the appropriate MediatR command or query, send it, and return the result. There is no business logic in controllers at all.

The TodoItemsController has seven endpoints. A GET request to the root path returns a filtered, paginated list of tasks. A GET request with an ID returns a single task. A POST request creates a new task. A PUT request updates an existing task. A DELETE request removes a task. Two additional POST endpoints handle marking a task as complete and toggling the My Day flag.

Each endpoint receives its dependencies through the controller's primary constructor — specifically, an IMediator instance. The primary constructor is a C# 12 feature that lets you declare constructor parameters directly on the class declaration, without needing a separate field or constructor body.

Attributes like HttpGet, HttpPost, FromQuery, and FromBody tell ASP.NET Core how to route and bind each request. The FromQuery attribute binds parameters from the URL query string (like "?myDay=true&page=2"), while FromBody binds from the JSON request body.

A particularly elegant detail is CreatedAtAction. When a new resource is created, this returns HTTP 201 with a Location header pointing to the GET endpoint for the new resource. This is a REST best practice — it tells the client exactly where to find the thing it just created.

### MediatR and CQRS: Separating Reads from Writes

MediatR is a library that implements the mediator pattern. Instead of a controller calling a repository directly, it sends a message (a command or query) and a handler processes it. This adds a layer of indirection that has several benefits.

The project follows CQRS — Command Query Responsibility Segregation. Commands change state: creating a task, updating a task, deleting a task. Queries read state: getting a single task, listing tasks with filters. Each command and each query is a separate class with its own handler.

Why is this separation valuable? Because each handler is a small, focused class that does exactly one thing. The CreateTodoItemCommandHandler only knows how to create a task. The GetTodoItemsQueryHandler only knows how to fetch and filter tasks. They are easy to understand, easy to test in isolation, and easy to modify without affecting other operations.

A command is defined as a record that implements IRequest with a return type. The handler is a class that implements IRequestHandler with the command type and return type. When the controller calls mediator.Send with a command, MediatR finds the matching handler and invokes it.

The CreateTodoItemCommandHandler demonstrates a typical workflow: first it validates the input (throwing a ValidationException if the title is empty), then it resolves relationships (loading the todo list and categories from the database), builds the entity, saves it through the repository, and maps the result to a response DTO.

MediatR is registered with a single line that scans the assembly for all handler implementations. You never have to manually register each handler — just create a new handler class and it gets discovered automatically.

### Mapping: Converting Between Entities and DTOs

The project uses extension methods to convert domain entities to response DTOs. A static method called ToResponse on TodoItem creates a TodoItemResponse by copying each property. For nested objects like RecurrencePattern and Categories, it creates the corresponding DTO types.

These mapping methods are marked as internal, meaning they are only visible within the API project. The domain and infrastructure layers cannot see or use them, which keeps the mapping logic contained at the boundary where it belongs.

### Global Exception Handling Middleware

Middleware in ASP.NET Core is a component that sits in the HTTP pipeline and processes every request. The ExceptionHandlingMiddleware wraps the entire pipeline in a try-catch block.

When everything goes well, it calls the next middleware in the chain (which eventually reaches the controller), and the response flows back normally. When any exception occurs, it catches it, logs the error, and returns a consistent JSON response with the appropriate HTTP status code.

The mapping is straightforward: NotFoundException becomes HTTP 404, ValidationException becomes HTTP 422 (Unprocessable Entity), and everything else becomes HTTP 500 with a generic message that hides internal details from the client. The 500 case is important for security — you never want to leak stack traces or database error messages to external clients.

This approach eliminates the need for try-catch blocks in every controller action. Handlers can simply throw exceptions, and the middleware handles the translation to HTTP responses. It keeps the error handling consistent across the entire API.

---

## Part 5: Following a Request from Start to Finish

To tie everything together, let us trace what happens when a client sends a POST request to create a new task.

The HTTP request arrives at the ASP.NET Core pipeline. First, the ExceptionHandlingMiddleware wraps the rest of the pipeline in a try-catch. Then, routing matches the URL to the TodoItemsController's Create action. Model binding deserializes the JSON request body into a CreateTodoItemRequest DTO.

The controller creates a CreateTodoItemCommand containing the request and sends it via MediatR. MediatR finds the CreateTodoItemCommandHandler and invokes its Handle method.

The handler validates the title. If the client sent a todo list ID, the handler loads that list from the database through the repository to verify it exists — if not, it throws NotFoundException. It loads any requested categories the same way. Then it builds a TodoItem entity, assigns all properties, and calls AddAsync on the repository.

The repository adds the entity to the DbContext's change tracker and calls SaveChangesAsync. Entity Framework generates an INSERT statement and sends it to PostgreSQL. The database assigns an auto-incremented ID and returns it.

Back in the handler, the saved entity (now with its database-generated ID) is converted to a TodoItemResponse DTO via the ToResponse mapping method. The handler returns this DTO to MediatR, which returns it to the controller.

The controller calls CreatedAtAction, which wraps the DTO in an HTTP 201 response with a Location header. The response flows back through the middleware pipeline and reaches the client.

If anything had gone wrong — say the todo list ID did not exist — the NotFoundException would have bubbled up through MediatR and the controller, been caught by the ExceptionHandlingMiddleware, and returned as HTTP 404 with a JSON error message. The controller would never have seen the exception at all.

---

## Part 6: Dependency Injection — The Glue That Holds Everything Together

Dependency injection is the mechanism that makes Clean Architecture work in practice. Every class declares what it needs through its constructor parameters, and the framework provides those dependencies automatically.

The infrastructure extension method registers three repository implementations as scoped services. "Scoped" means one instance per HTTP request. This is important because the DbContext is also scoped — all repositories within the same request share the same DbContext instance, which means they share the same database connection and change tracker. If two repositories modify data in the same request, SaveChanges commits everything in a single transaction.

MediatR is registered by scanning the assembly for handler classes. Each handler also receives its dependencies through its constructor — the repositories it needs, injected by the DI container.

The result is a chain of automatic resolution: when a request arrives, ASP.NET Core creates the controller and injects IMediator. When MediatR invokes a handler, it resolves the handler's dependencies — the repository interfaces. The DI container matches each interface to its registered implementation and injects the concrete repository, which itself receives the DbContext.

None of these classes create their dependencies manually. None of them use the "new" keyword to instantiate a repository or a DbContext. Everything flows through the DI container, which makes the system flexible, testable, and loosely coupled.

---

## Part 7: Modern C# Features Used Throughout

The codebase makes extensive use of modern C# features that are worth understanding.

Primary constructors, introduced in C# 12, let you declare constructor parameters directly on the class declaration. Instead of writing a constructor body that assigns parameters to private fields, the parameters are available throughout the class body. This eliminates significant boilerplate in classes that just need to store their dependencies.

Records are used for DTOs and value objects. A positional record like "public record CategoryResponse(int Id, string Name, string? Color)" generates a complete type with a constructor, read-only properties, value-based equality (Equals and GetHashCode), a ToString method, and a Deconstruct method — all from a single line of code.

Nullable reference types (the question mark on reference types like "string?") were introduced to help catch null reference bugs at compile time. When you declare a property as "string Title" (without the question mark), the compiler warns you if you try to assign null to it. When you declare "string? Description" (with the question mark), you are explicitly saying this value can be null and the caller should check for it.

The null-forgiving operator ("= null!") is used on navigation properties that Entity Framework populates. The entity declares "TodoList TodoList = null!" which tells the compiler "I know this starts as null, but EF Core will fill it in before I use it — trust me."

Collection expressions like "= []" are shorthand for "new List<>()" introduced in recent C# versions. They make property initializers more concise.

Extension methods (the "this" keyword on the first parameter of a static method) are used throughout for cleaner APIs — for DI registration, pagination, entity-to-DTO mapping, and middleware registration. They let you write "query.ToPaginatedResultAsync()" instead of "PaginationHelper.Paginate(query)" which reads more naturally and is more discoverable.

---

## Part 8: Database Design Decisions

Several database design decisions in this project are worth highlighting.

The optional relationship between TodoItem and TodoList (nullable foreign key with ON DELETE SET NULL) was a deliberate choice. Tasks can exist independently without belonging to any list. When a list is deleted, its tasks are not deleted — they just become "unassigned." This is a user-friendly behavior: deleting a list should not destroy all the work tracked within it.

The many-to-many relationship between TodoItem and Category uses Entity Framework Core's implicit join table. The database has a TodoItemCategory table with two foreign key columns, but the application code never defines a class for it. This is a convenience that EF Core provides — you work with lists of entities and EF Core manages the join table behind the scenes.

The owned type pattern for RecurrencePattern stores the recurrence data as columns within the TodoItems table (RecurrenceType, RecurrenceInterval, RecurrenceEndDate) rather than in a separate table. This is appropriate because a recurrence pattern has no independent identity — it only makes sense in the context of a specific task.

Database indexes are created on columns that appear in common query patterns: IsCompleted, DueDate, IsImportant, IsInMyDay, and TodoListId. Without these indexes, every query that filters on these columns would require scanning the entire table. With them, the database can quickly find matching rows using the index.

---

## Part 9: Error Handling Philosophy

The project takes a specific approach to error handling that is worth examining.

Business-level errors like "not found" and "validation failed" are expressed as custom exception types. These are thrown by command handlers (the business logic layer) and caught by a single global middleware. This means no individual controller or handler needs to think about HTTP status codes — they just throw the appropriate exception and the middleware translates it.

NotFoundException maps to HTTP 404. It carries the resource name and ID so the error message is specific and helpful: "TodoList with id 99 was not found."

ValidationException maps to HTTP 422 (Unprocessable Entity). It carries a dictionary of field-level errors, so the client receives structured information like: the field "Title" has the error "Title is required." This is far more useful to API consumers than a generic "bad request" message.

All other exceptions map to HTTP 500 with a generic message. The actual error details are logged on the server but never sent to the client. This is a security practice — internal server errors might contain stack traces, database query details, or configuration information that could be exploited.

---

## Part 10: Key Architectural Patterns Summary

The Repository Pattern separates the concern of data access from business logic. The domain defines interfaces that describe what data operations are needed. The infrastructure implements those interfaces using a specific database technology. The API layer consumes the interfaces without knowing or caring about the implementation details.

CQRS (Command Query Responsibility Segregation) separates read operations from write operations. Each operation is a self-contained class with a single handler. This makes the codebase highly modular — adding a new feature means adding a new command or query class, not modifying existing ones.

The Mediator Pattern, implemented via MediatR, decouples the sender (controller) from the receiver (handler). The controller does not know which class handles its command — it just sends a message. This makes it easy to add cross-cutting concerns like logging, validation, or caching by adding MediatR pipeline behaviors that intercept every command and query.

The DTO Pattern separates the API contract from the internal data model. Changes to the database schema do not automatically change the API response format, and vice versa. This is essential for maintaining backward compatibility as the application evolves.

Clean Architecture ties all of these patterns together with a clear dependency rule: dependencies point inward, toward the domain. The domain knows nothing about infrastructure or HTTP. Infrastructure knows about the domain but not about HTTP. The API layer orchestrates everything.

Together, these patterns create a codebase that is modular, testable, maintainable, and extensible. Each piece has a single, clear responsibility, and changing one piece does not require changing all the others.
