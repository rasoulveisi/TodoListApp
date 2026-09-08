# TodoListApp API

This repository contains an ASP.NET Core task API with create/read/update/delete for todo items.

## Local setup

1. Install .NET 10 SDK.
2. Open a terminal in:
   - `/Users/rasoul/rasoul/legacy/TodoListApp`
3. Restore and build:
   - `dotnet build`

## Run API locally

- `dotnet run --project src/TodoList.Api/TodoList.Api.csproj`

The API uses PostgreSQL from `src/TodoList.Api/appsettings.json` by default.
For local API runs, set `DefaultConnection` to a local PostgreSQL database.

## Run tests

The integration test uses an isolated in-memory database per test run.
- `dotnet test tests/Test.Api/TodoList.Domain.Tests.csproj`

## Example request flow

1. Create a todo item
```bash
curl -s -X POST http://localhost:5067/api/todoitems \
  -H "Content-Type: application/json" \
  -d '{"title":"Buy groceries","description":"Milk and bread","todoListId":null,"dueDate":null,"isImportant":true,"isInMyDay":true,"recurrence":null,"categoryIds":[]}'
```

2. Get by id
```bash
curl -s http://localhost:5067/api/todoitems/1
```

3. Mark as completed
```bash
curl -s -X POST http://localhost:5067/api/todoitems/1/complete \
  -H "Content-Type: application/json" \
  -d '{"complete":true}'
```

4. Filter by My Day
```bash
curl -s http://localhost:5067/api/todoitems?isInMyDay=true&page=1&pageSize=10
```

## Known limits

- This test project intentionally avoids shared PostgreSQL data and uses isolation in memory.
- Pagination validation now requires `page >= 1` and `1 <= pageSize <= 100`.
