using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.Domain.Identity;
using Platform.Infrastructure.Identity;
using Platform.Infrastructure.Persistence;

namespace Platform.Api.Identity;

public static class AdminBootstrap
{
    public static async Task RunAsync(IServiceProvider services, bool resetMfa)
    {
        if (Console.IsInputRedirected) throw new InvalidOperationException("El enrolamiento administrativo requiere una consola interactiva.");
        var email = Environment.GetEnvironmentVariable("KIPO_ADMIN_EMAIL")?.Trim();
        var password = Environment.GetEnvironmentVariable("KIPO_ADMIN_PASSWORD");
        var name = Environment.GetEnvironmentVariable("KIPO_ADMIN_NAME")?.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)
            || (!resetMfa && (string.IsNullOrWhiteSpace(name) || name.Length > 160)))
            throw new InvalidOperationException("Configura el correo, nombre y contraseña administrativos mediante el script interactivo.");
        await using var scope = services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var database = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        await using var transaction = await database.Database.BeginTransactionAsync();
        ApplicationUser user;
        if (resetMfa)
        {
            var userId = await database.Users.AsNoTracking().Where(account => account.NormalizedEmail == users.NormalizeEmail(email))
                .Select(account => (Guid?)account.Id).SingleOrDefaultAsync();
            if (userId is null) throw new InvalidOperationException("No se pudo autorizar la recuperación administrativa.");
            await AdminAuthenticationService.LockAsync(database, userId.Value, CancellationToken.None);
            user = (await users.FindByIdAsync(userId.Value.ToString()))!;
            if (!user.IsPlatformAdmin || user.State != UserState.Active || !user.TwoFactorEnabled || await users.IsLockedOutAsync(user))
                throw new InvalidOperationException("No se pudo autorizar la recuperación administrativa.");
            Console.Write("Código de recuperación de un solo uso: ");
            var code = ReadSecret();
            if (!await users.CheckPasswordAsync(user, password)
                || !await AdminAuthenticationService.VerifyMfaAsync(users, user, code, true, DateTimeOffset.UtcNow))
            {
                AdminAuthenticationService.Require(await users.AccessFailedAsync(user));
                await transaction.CommitAsync();
                throw new InvalidOperationException("No se pudo autorizar la recuperación administrativa.");
            }
        }
        else
        {
            await database.Database.ExecuteSqlRawAsync("DECLARE @result int; EXEC @result = sp_getapplock @Resource = 'kipo:admin:bootstrap', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 5000; IF @result < 0 THROW 51000, 'Administrative bootstrap is busy.', 1;");
            if (await database.Users.AnyAsync(account => account.IsPlatformAdmin))
                throw new InvalidOperationException("Ya existe un administrador. El bootstrap solo crea la primera cuenta.");
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                DisplayName = name!,
                IsPlatformAdmin = true,
                EmailConfirmed = true,
                State = UserState.Active
            };
            AdminAuthenticationService.Require(await users.CreateAsync(user, password));
        }
        AdminAuthenticationService.Require(await users.ResetAuthenticatorKeyAsync(user));
        AdminAuthenticationService.Require(await users.RemoveAuthenticationTokenAsync(user, "Kipo.Admin", "UsedTotp"));
        var key = await users.GetAuthenticatorKeyAsync(user);
        Console.WriteLine("Añade una cuenta TOTP en tu autenticador: Kipo, 6 dígitos, período de 30 segundos.");
        Console.WriteLine("Clave de configuración (guárdala de forma privada): " + key);
        Console.Write("Código generado por el autenticador: ");
        if (!await AdminAuthenticationService.VerifyMfaAsync(users, user, ReadSecret(), false, DateTimeOffset.UtcNow))
            throw new InvalidOperationException("Código incorrecto. No se guardaron los cambios del enrolamiento.");
        AdminAuthenticationService.Require(await users.SetTwoFactorEnabledAsync(user, true));
        AdminAuthenticationService.Require(await users.ResetAccessFailedCountAsync(user));
        AdminAuthenticationService.Require(await users.UpdateSecurityStampAsync(user));
        var recoveryCodes = (await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))?.ToArray()
            ?? throw new InvalidOperationException("No se pudieron generar códigos de recuperación.");
        var now = DateTimeOffset.UtcNow;
        await database.UserSessions.Where(session => session.UserId == user.Id && session.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RevokedAt, now));
        database.AuthenticationAudits.Add(new AuthenticationAudit
        {
            UserId = user.Id,
            Action = resetMfa ? "admin.mfa_reset" : "admin.bootstrap",
            CorrelationId = Guid.NewGuid().ToString()
        });
        await database.SaveChangesAsync();
        await transaction.CommitAsync();
        Console.WriteLine("MFA configurado. Guarda estos códigos de recuperación; cada uno puede utilizarse una sola vez:");
        foreach (var code in recoveryCodes) Console.WriteLine(code);
        Console.WriteLine("Espera al siguiente código del autenticador antes de iniciar sesión.");
    }

    private static string ReadSecret()
    {
        var value = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace)
            {
                if (value.Length > 0) value.Length--;
            }
            else if (!char.IsControl(key.KeyChar) && value.Length < 64) value.Append(key.KeyChar);
        }
        Console.WriteLine();
        return value.ToString();
    }
}
