using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Security.Claims;

using ticket_API.Data;
using ticket_API.Models;
using ticket_API.Models.Auth;
using ticket_API.Services;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddSingleton<JwtService>();

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)
            )
            ,
            ClockSkew = TimeSpan.Zero
        };
        // Conserver le token (utile pour debug) et diminuer le ClockSkew pour tests locaux
        options.SaveToken = true;

        // Logger les évènements d'authentification pour diagnostiquer les 401
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtAuth");
                logger.LogError(ctx.Exception, "Authentication failed");
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtAuth");
                var sub = ctx.Principal?.FindFirst("sub")?.Value;
                logger.LogInformation("Token validated for user id {sub}", sub);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// Swagger 
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ticket_API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Entrer : Bearer {votre_token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// AUTH ENDPOINTS
app.MapPost("/auth/register", async (RegisterRequest req, AppDbContext db, JwtService jwt) =>
{
    if (await db.Users.AnyAsync(u => u.Email == req.Email))
        return Results.BadRequest("Email déjà utilisé");

    var user = new User
    {
        FirstName = req.FirstName,
        LastName = req.LastName,
        Email = req.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    var token = jwt.GenerateToken(user.Id, user.Email);

    return Results.Ok(new { token });
});

app.MapPost("/auth/login", async (LoginRequest req, AppDbContext db, JwtService jwt) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
    if (user is null)
        return Results.BadRequest("Identifiants invalides");

    if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        return Results.BadRequest("Identifiants invalides");

    var token = jwt.GenerateToken(user.Id, user.Email);

    return Results.Ok(new { token });
});

app.MapGet("/me", [Authorize] async (HttpContext ctx, AppDbContext db) =>
{
    // Certaines bibliothèques mappent le claim 'sub' vers ClaimTypes.NameIdentifier,
    // on vérifie plusieurs emplacements pour récupérer l'id utilisateur.
    var sub = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
              ?? ctx.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
              ?? ctx.User.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(sub))
        return Results.Unauthorized();

    if (!int.TryParse(sub, out var userId))
        return Results.Unauthorized();

    var user = await db.Users.FindAsync(userId);

    return user is not null ? Results.Ok(user) : Results.NotFound();
});

// EVENTS
app.MapGet("/events", async (AppDbContext db) => await db.Events.ToListAsync());

app.MapGet("/events/{id}", async (AppDbContext db, int id) =>
{
    var ev = await db.Events.FindAsync(id);
    return ev is not null ? Results.Ok(ev) : Results.NotFound();
});

app.MapPost("/events", async (AppDbContext db, Event newEvent) =>
{
    db.Events.Add(newEvent);
    await db.SaveChangesAsync();
    return Results.Created($"/events/{newEvent.Id}", newEvent);
});

app.MapPut("/events/{id}", async (AppDbContext db, int id, Event updatedEvent) =>
{
    var ev = await db.Events.FindAsync(id);
    if (ev is null) return Results.NotFound();

    ev.Name = updatedEvent.Name;
    ev.Date = updatedEvent.Date;
    ev.Location = updatedEvent.Location;

    await db.SaveChangesAsync();
    return Results.Ok(ev);
});

app.MapDelete("/events/{id}", async (AppDbContext db, int id) =>
{
    var ev = await db.Events.FindAsync(id);
    if (ev is null) return Results.NotFound();

    db.Events.Remove(ev);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// TICKETS
app.MapGet("/tickets", async (AppDbContext db) =>
    await db.Tickets.Include(t => t.User).Include(t => t.Event).ToListAsync());

app.MapGet("/tickets/{id}", async (int id, AppDbContext db) =>
{
    var ticket = await db.Tickets
        .Include(t => t.Event)
        .Include(t => t.User)
        .FirstOrDefaultAsync(t => t.Id == id);

    return ticket is not null ? Results.Ok(ticket) : Results.NotFound();
});

app.MapPost("/tickets", async (Ticket ticket, AppDbContext db) =>
{
    db.Tickets.Add(ticket);
    await db.SaveChangesAsync();
    return Results.Created($"/tickets/{ticket.Id}", ticket);
});

app.MapPut("/tickets/{id}", async (int id, Ticket input, AppDbContext db) =>
{
    var ticket = await db.Tickets.FindAsync(id);
    if (ticket is null) return Results.NotFound();

    ticket.Title = input.Title;
    ticket.Description = input.Description;
    ticket.Price = input.Price;
    ticket.EventId = input.EventId;

    await db.SaveChangesAsync();
    return Results.Ok(ticket);
});

app.MapDelete("/tickets/{id}", async (int id, AppDbContext db) =>
{
    var ticket = await db.Tickets.FindAsync(id);
    if (ticket is null) return Results.NotFound();

    db.Tickets.Remove(ticket);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// USERS
app.MapGet("/users", async (AppDbContext db) => await db.Users.ToListAsync());

app.MapGet("/users/{id}", async (int id, AppDbContext db) =>
{
    var user = await db.Users.Include(u => u.Tickets).FirstOrDefaultAsync(u => u.Id == id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

app.MapPost("/users", async (User user, AppDbContext db) =>
{
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{user.Id}", user);
});

app.MapPut("/users/{id}", async (int id, User input, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound();

    user.FirstName = input.FirstName;
    user.LastName = input.LastName;
    user.Email = input.Email;

    await db.SaveChangesAsync();
    return Results.Ok(user);
});

app.MapDelete("/users/{id}", async (int id, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound();

    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
