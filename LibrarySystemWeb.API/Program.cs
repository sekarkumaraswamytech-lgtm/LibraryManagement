using LibrarySystem.gRpcUsers;
using LibrarySystem.gRpcBooks;
using LibrarySystem.gRpcLending;
using FluentValidation;
using FluentValidation.AspNetCore;
using LibrarySystemWeb.API.Validation;
using LibrarySystemWeb.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<MostActiveUsersQueryValidator>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Correlation ID services
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<GrpcCorrelationClientInterceptor>();

// gRPC clients with correlation interceptor
builder.Services.AddGrpcClient<UsersService.UsersServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["Grpc:Users"] ?? "https://localhost:7029"))
    .AddInterceptor<GrpcCorrelationClientInterceptor>();

builder.Services.AddGrpcClient<BookService.BookServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["Grpc:Books"] ?? "https://localhost:7047"))
    .AddInterceptor<GrpcCorrelationClientInterceptor>();

builder.Services.AddGrpcClient<LendingDetailsService.LendingDetailsServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["Grpc:Lending"] ?? "https://localhost:7041"))
    .AddInterceptor<GrpcCorrelationClientInterceptor>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Insert correlation middleware early
app.UseMiddleware<CorrelationIdMiddleware>();

// Existing HTTP-level exception middleware (if present) should come after correlation
app.UseMiddleware<HttpExceptionMiddleware>();

app.MapControllers();
app.Run();

// IMPORTANT: Add a public partial Program class for WebApplicationFactory discovery in tests.
namespace LibrarySystemWeb.API
{
    public partial class Program { }
}
