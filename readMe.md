LibrarySystem

A modular, layered .NET 8 Library Management System built using clean architecture principles, gRPC microservices, FluentValidation, structured logging with correlation IDs, optimistic concurrency, and a full automated testing strategy.

Architecture Overview
Data Flow
Client (HTTP)
      ↓
API Controllers
      ↓
gRPC Clients → gRPC Hosts
      ↓
Application Services
      ↓
Repositories
      ↓
EF Core
      ↓
Database


This architecture provides:

✔ Strong separation of concerns
✔ High testability
✔ Clear domain boundaries
✔ Scalable microservice-style structure

 Solution Structure
 Domain Layer

Core entities: Book, User, LendingRecord

Optimistic concurrency: RowVersion timestamp on Book

Application Layer

Services: BookService, UserService, LendingService

Features:

Business rules

Validation

Structured logging

Custom exceptions

Infrastructure Layer

EF Core (SQL Server + SQLite + InMemory)

LibraryDbContext

Repository implementations

Database migrations

gRPC Hosts

Each domain area exposed as its own gRPC service:

LibrarySystem.gRpcBooks

LibrarySystem.gRpcUsers

LibrarySystem.gRpcLending

(Services can be merged into a single host in future versions.)

 API Layer

REST gateway

Routes incoming HTTP requests → delegates to gRPC services

Main entry point for UI or third-party clients

 Tests

Automation coverage includes:

Unit tests

Repository tests

Functional tests

gRPC integration tests

API end-to-end tests

Validation tests

Edge case tests

Planned: concurrency + cancellation tests

Tech Stack

.NET 8 / C# 12

EF Core 8

gRPC (Grpc.AspNetCore / Grpc.Net.Client)

FluentValidation

SQL Server / SQLite

Structured Logging

xUnit + FluentAssertions + Moq + WebApplicationFactory

(Optional Roadmap) Polly, OpenTelemetry

 Public API Endpoints
HTTP Endpoint	Description
GET /api/library/users/most-active?from=&to=	Returns most active users in the date range (default = last 30 days)
GET /api/library/books/most-borrowed	Retrieves the most borrowed books
GET /api/library/books/{bookId}/related	Returns related books using co-borrowing analysis
GET /api/library/books/{userId}/{bookId}/reading-pace	Estimates reading pace in hours
Validation

Implemented with FluentValidation
Automatic 400 Bad Request returns for invalid input.

Validators:

MostActiveUsersQueryValidator

RelatedBooksRouteValidator

ReadingPaceRouteValidator

 Exception Strategy
Custom Exception	gRPC Status	HTTP Status
ValidationException	InvalidArgument	400
NotFoundException	NotFound	404
DataAccessException	Internal	500

Centralized handlers

gRPC: GrpcExceptionInterceptor

HTTP: HttpExceptionMiddleware

Logging & Correlation

CorrelationIdMiddleware
Automatically attaches or propagates x-correlation-id

gRPC interceptors propagate metadata across services

StructuredLogger<T> adds:

timestamp

correlation ID

method name

request payload

Optimistic Concurrency

Implemented using:

[Timestamp]
public byte[] RowVersion { get; set; }


Prevents race conditions when borrowing or returning books.

Atomic updates through:

TryAdjustAvailableCopiesAsync

Testing Matrix
Test Type	Location	Purpose
Unit Tests	ServiceTests	Business rules & validations
Repository Tests	RepositoryTests	EF Core queries, DB ops
Functional Tests	FunctionalTests	Multi-step behaviors
gRPC Integration	gRpcTests	Service wiring & interceptors
API End-to-End	ApiEndToEndTests	Full-system request flow
Validation Tests	Validation	DTO & request validation
Edge Case Tests	ServiceTests	Boundaries & error paths
Concurrency (planned)	—	Parallel borrow attempts
Cancellation (planned)	—	Token verification
Database Configuration

Example (appsettings.Development.json):

"ConnectionStrings": {
  "LibraryDbContext": "Server=(localdb)\\MSSQLLocalDB;Database=LibraryDb;Trusted_Connection=True;MultipleActiveResultSets=true",
  "LibraryDbContextSqlite": "Data Source=LibraryDb.sqlite"
}

Common Issues & Fixes
Issue	Cause	Fix
gRPC Unavailable	Wrong port / host not running	Check launchSettings & URLs
500 Errors in Tests	Missing DI registration	Ensure all services are registered
Wrong overload used	String vs DateTime mismatch	Adjust mocks or request types
Concurrency update lost	Detached entities	Use RowVersion + atomic update
“No lending records found”	Empty DB for range	Seed test data appropriately
Running the System Locally
1. Set Multiple Startup Projects

Enable these to run together:

LibrarySystem.API

LibrarySystem.gRpcBooks

LibrarySystem.gRpcLending

LibrarySystem.gRpcUsers

2. CLI Alternative
dotnet run --project LibrarySystemWeb.API

3. Open Swagger
https://localhost:<port>/swagger

4. Correlation IDs

Optional:

x-correlation-id: <guid>


Middleware auto-generates one if missing.

License

Internal / educational use only.
Add a LICENSE file before making this repository public.