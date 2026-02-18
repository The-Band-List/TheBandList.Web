using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using TheBandList.Web.Entities;
using TheBandList.Web.Entities.Context;

namespace TheBandList.Web.Auth;

public static class DiscordAuthExtensions
{
    public static IServiceCollection AddDiscordAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var clientId = FirstNonEmpty(config,
            "Authentication:Discord:ClientId",
            "AUTHENTICATION__DISCORD__CLIENTID",
            "DISCORD_CLIENT_ID");

        var clientSecret = FirstNonEmpty(config,
            "Authentication:Discord:ClientSecret",
            "AUTHENTICATION__DISCORD__CLIENTSECRET",
            "DISCORD_CLIENT_SECRET");

        var callbackPath = FirstNonEmpty(config,
            "Authentication:Discord:CallbackPath",
            "AUTHENTICATION__DISCORD__CALLBACKPATH") ?? "/signin-discord";

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException(
                "Discord OAuth non configuré. Renseigne au moins l’une des paires clés suivantes :\n" +
                " - Authentication:Discord:ClientId / Authentication:Discord:ClientSecret (appsettings)\n" +
                " - AUTHENTICATION__DISCORD__CLIENTID / AUTHENTICATION__DISCORD__CLIENTSECRET (.env)\n" +
                " - DISCORD_CLIENT_ID / DISCORD_CLIENT_SECRET (.env)"
            );
        }

        services
            .AddAuthentication(o =>
            {
                o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = "Discord";
            })
            .AddCookie(o =>
            {
                o.LoginPath = "/login/discord";
                o.LogoutPath = "/logout";
                o.ExpireTimeSpan = TimeSpan.FromDays(365);
                o.SlidingExpiration = true;
                o.Cookie.IsEssential = true;
            })
            .AddOAuth("Discord", o =>
            {
                o.ClientId = clientId!;
                o.ClientSecret = clientSecret!;
                o.CallbackPath = callbackPath;

                o.AuthorizationEndpoint = "https://discord.com/oauth2/authorize";
                o.TokenEndpoint = "https://discord.com/api/oauth2/token";
                o.UserInformationEndpoint = "https://discord.com/api/users/@me";

                o.Scope.Add("identify");
                o.SaveTokens = true;
                o.UsePkce = true;

                o.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                o.ClaimActions.MapJsonKey(ClaimTypes.Name, "username");
                o.ClaimActions.MapCustomJson("discord:global_name", u => u.TryGetProperty("global_name", out var g) ? g.GetString() : null);
                o.ClaimActions.MapJsonKey("discord:avatar", "avatar");

                o.Events = new OAuthEvents
                {
                    OnRedirectToAuthorizationEndpoint = ctx =>
                    {
                        ctx.Response.Redirect(ctx.RedirectUri);
                        return Task.CompletedTask;
                    },
                    OnCreatingTicket = async ctx =>
                    {
                        using var req = new HttpRequestMessage(HttpMethod.Get, ctx.Options.UserInformationEndpoint);
                        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ctx.AccessToken);
                        using var res = await ctx.Backchannel.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ctx.HttpContext.RequestAborted);
                        res.EnsureSuccessStatusCode();

                        using var payload = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                        var user = payload.RootElement;

                        ctx.RunClaimActions(user);
                        ctx.Properties.IsPersistent = true;
                        ctx.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddYears(1);

                        var discordId = user.GetProperty("id").GetString()!;
                        var username = user.TryGetProperty("global_name", out var gl) && gl.ValueKind != JsonValueKind.Null
                            ? gl.GetString()
                            : user.GetProperty("username").GetString();
                        var avatar = user.TryGetProperty("avatar", out var av) ? av.GetString() : null;

                        var db = ctx.HttpContext.RequestServices.GetRequiredService<TheBandListWebDbContext>();
                        var account = await db.DiscordAccounts
                            .Include(a => a.Utilisateur)
                            .SingleOrDefaultAsync(a => a.DiscordId == discordId);

                        if (account is null)
                        {
                            account = new DiscordAccount
                            {
                                DiscordId = discordId,
                                Username = username,
                                AvatarHash = avatar,
                                Utilisateur = new Utilisateur
                                {
                                    Nom = username ?? "Utilisateur Discord"
                                },
                                AccessToken = ctx.AccessToken,
                                RefreshToken = ctx.RefreshToken,
                                AccessTokenExpiresAt = ctx.ExpiresIn.HasValue ? DateTime.UtcNow.Add(ctx.ExpiresIn.Value) : null
                            };
                            db.DiscordAccounts.Add(account);
                        }
                        else
                        {
                            account.Username = username;
                            account.AvatarHash = avatar;
                            account.AccessToken = ctx.AccessToken;
                            account.RefreshToken = ctx.RefreshToken;
                            account.AccessTokenExpiresAt = ctx.ExpiresIn.HasValue ? DateTime.UtcNow.Add(ctx.ExpiresIn.Value) : null;
                        }

                        await db.SaveChangesAsync();

                        var id = ctx.Identity!;
                        id.AddClaim(new Claim("discord:id", discordId));
                        if (!string.IsNullOrEmpty(avatar)) id.AddClaim(new Claim("discord:avatar", avatar));
                        if (!string.IsNullOrEmpty(username)) id.AddClaim(new Claim(ClaimTypes.Name, username));
                    }
                };
            });

        return services;
    }

    private static string? FirstNonEmpty(IConfiguration config, params string[] keys)
    {
        foreach (var k in keys)
        {
            var v = config[k];
            if (!string.IsNullOrWhiteSpace(v)) return v;

            v = Environment.GetEnvironmentVariable(k);
            if (!string.IsNullOrWhiteSpace(v)) return v;
        }
        return null;
    }

    public static IEndpointRouteBuilder MapDiscordAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/login/discord", async (HttpContext http) =>
        {
            await http.ChallengeAsync("Discord", new AuthenticationProperties { RedirectUri = "/auth/close" });
        });

        app.MapGet("/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            http.Response.Redirect("/");
        });

        app.MapGet("/auth/close", async ctx =>
        {
            const string html = @"<!doctype html>
                <meta charset=""utf-8"">
                <title>Connexion Discord</title>
                <p>Connexion réussie. Cette fenêtre va se fermer…</p>
                <script>
                    if (window.opener) { try { window.opener.postMessage('auth:done', '*'); } catch(e) {} window.close(); }
                    else { location.href = '/'; }
                </script>";
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync(html);
        });

        return app;
    }
}