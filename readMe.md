LibrarySystem

A modular, layered .NET 8 library management system built with clean architecture principles, gRPC-based internal services, FluentValidation, structured logging with correlation IDs, optimistic concurrency, and full-stack automated testing.

Architecture Overview

Data Flow:

Client (HTTP) ? API Controllers ? gRPC Clients ? gRPC Hosts ?
Application Services ? Repositories ? EF Core ? Database

This architecture ensures separation of concerns, testability, and clear domain boundaries.

Solution Structure
Domain

Core entities: Book, User, LendingRecord

Concurrency management: RowVersion timestamp on Book

Application

Services: BookService, UserService, LendingService

Interfaces, custom exceptions, structured logging

Validation and business rules enforcement

Infrastructure

LibraryDbContext with EF Core

Repository implementations

Migrations and persistence logic

gRPC Hosts

LibrarySystem.gRpcBooks

LibrarySystem.gRpcUsers

LibrarySystem.gRpcLending

Each host exposes its own proto service
(can be combined into a single host later)

API Layer

REST gateway (LibraryController)

Delegates internal calls to gRPC services

Tests

Unit, repository, functional, integration, API E2E, validation, edge cases, and concurrency (planned)

Tech Stack

.NET 8 / C# 12

EF Core 8 (InMemory + SQL Server)

gRPC (Grpc.AspNetCore / Grpc.Net.Client)

FluentValidation

Logging: Structured logger + correlation ID middleware

Testing: xUnit, FluentAssertions, Moq, WebApplicationFactory

Optional roadmap: Polly, OpenTelemetry

Public API Endpoints
Endpoint	Description
GET /api/library/users/most-active?from=&to=	Most active users in date range (defaults to last 30 days)
GET /api/library/books/most-borrowed	Top borrowed book(s)
GET /api/library/books/{bookId}/related	Related books via co-borrowing analysis
GET /api/library/books/{userId}/{bookId}/reading-pace	Estimated reading hours
Validation

Implemented using FluentValidation with automatic 400 Bad Request:

MostActiveUsersQueryValidator

RelatedBooksRouteValidator

ReadingPaceRouteValidator

Exception Strategy
Custom Exception	gRPC Status	HTTP Status
ValidationException	InvalidArgument	400
NotFoundException	NotFound	404
DataAccessException	Internal	500

Centralized error mapping:

gRPC: GrpcExceptionInterceptor

HTTP: HttpExceptionMiddleware

Logging & Correlation

CorrelationIdMiddleware ensures all HTTP requests include x-correlation-id

gRPC interceptors propagate metadata between hosts

StructuredLogger<T> enriches logs with correlation ID, timestamp, method, and payload

Optimistic Concurrency

Implemented via Book.RowVersion ([Timestamp])

Prevents race conditions during borrow/return operations

TryAdjustAvailableCopiesAsync performs atomic updates with conflict detection

Testing Matrix
Test Type	Location	Focus
Unit Tests	ServiceTests	Business logic, validation
Repository Tests	RepositoryTests	EF queries, DB operations
Functional Tests	FunctionalTests	Multi-step flows
gRPC Integration	gRpcTests	Host wiring, interceptors
API End-to-End	ApiEndToEndTests	Full system flow
Validation Tests	Validation	DTO validation rules
Edge Cases	ServiceTests	Errors, boundaries
Concurrency (planned)	—	Parallel borrow attempts
Cancellation (planned)	—	Token propagation
Database & Migrations

Connection strings (appsettings.Development.json):

"ConnectionStrings": {
  "LibraryDbContext": "Server=(localdb)\\MSSQLLocalDB;Database=LibraryDb;Trusted_Connection=True;MultipleActiveResultSets=true",
  "LibraryDbContextSqlite": "Data Source=LibraryDb.sqlite"
}

Common Issues & Solutions
Issue	Cause	Fix
gRPC “Unavailable”	Host not running or wrong port	Check launchSettings and service URLs
Test failures (500)	Missing DI registration	Ensure repositories & interceptors registered
Wrong overload hit	String vs DateTime overload	Ensure mocks use the correct signature
Concurrency update lost	Detached entities	Use RowVersion + atomic update method
Running the System Locally
1. Configure Startup Projects (Multiple Start)

To run the entire platform locally, configure Visual Studio to launch all required services:

LibrarySystem.API

LibrarySystem.gRpcBooks

LibrarySystem.gRpcLending

LibrarySystem.gRpcUsers

2. Run the API (CLI alternative)
dotnet run --project LibrarySystemWeb.API

3. Access Swagger UI
https://localhost:<port>/swagger

4. (Optional) Correlation IDs

Include header x-correlation-id: <guid>
If omitted, the middleware automatically generates one.

License

Internal / educational usage.
Add a LICENSE file before public distribution.