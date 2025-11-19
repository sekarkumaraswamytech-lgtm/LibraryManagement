using LibrarySystem.gRpcLending.Services;
using LibrarySystem.Application.Interfaces;
using LibrarySystem.Application.Services;
using LibrarySystem.Infrastructure.DBContext;
using LibrarySystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<GrpcExceptionInterceptor>(); // correlation + exception mapping
});

builder.Services.AddDbContext<LibraryDbContext>(opts => opts.UseInMemoryDatabase("LendingGrpcDb"));
builder.Services.AddScoped<ILendingRepository, LendingRepository>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILendingService, LendingService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();
app.MapGrpcService<LendingGrpcService>();
app.Run();

namespace LibrarySystem.gRpcLending
{
    public partial class Program { }
}
