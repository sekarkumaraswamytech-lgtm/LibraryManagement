using LibrarySystem.gRpcUsers.Services;
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

builder.Services.AddDbContext<LibraryDbContext>(opts => opts.UseInMemoryDatabase("UsersGrpcDb"));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILendingRepository, LendingRepository>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();
app.MapGrpcService<UsersgRpcService>();
app.Run();  

namespace LibrarySystem.gRpcUsers
{
    public partial class Program { }
}