using Microsoft.AspNetCore.Identity;
using Platform.Infrastructure.Identity;

namespace Platform.Api.Identity;

public static class LocalUserBootstrap
{
    public static async Task RunAsync(IServiceProvider services, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment()) throw new InvalidOperationException("La creación de usuarios locales solo está disponible en Development.");
        var email = Environment.GetEnvironmentVariable("KIPO_LOCAL_USER_EMAIL");
        var password = Environment.GetEnvironmentVariable("KIPO_LOCAL_USER_PASSWORD");
        var name = Environment.GetEnvironmentVariable("KIPO_LOCAL_USER_NAME");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(name) || name.Length > 160)
        {
            throw new InvalidOperationException("Configura KIPO_LOCAL_USER_EMAIL, KIPO_LOCAL_USER_PASSWORD y KIPO_LOCAL_USER_NAME antes de crear el usuario.");
        }
        await using var scope = services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var result = await users.CreateAsync(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email.Trim(),
            Email = email.Trim(),
            DisplayName = name.Trim()
        }, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("No se pudo crear el usuario: " + string.Join(", ", result.Errors.Select(error => error.Code)));
        }
        Console.WriteLine("Usuario local creado con estado Pendiente de aprobación y sin acceso operativo.");
    }
}
