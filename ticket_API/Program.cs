using Microsoft.EntityFrameworkCore;
using ticket_API.Data;
using ticket_API.Models;



var builder = WebApplication.CreateBuilder(args);

// EF Core + SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/events", async (AppDbContext db) =>
{
    return await db.Events.ToListAsync();
});

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


app.MapGet("/tickets", async (AppDbContext db) =>
{
    return await db.Tickets.ToListAsync();
});

app.MapGet("/tickets/{id}", async (int id, AppDbContext db) =>
{
    return await db.Tickets.FindAsync(id)
        is Ticket ticket
        ? Results.Ok(ticket)
        : Results.NotFound();
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

app.MapGet("/users", async (AppDbContext db) =>
{
    return await db.Users.ToListAsync();
});

app.MapGet("/users/{id}", async (int id, AppDbContext db) =>
{
    return await db.Users.FindAsync(id)
        is User user
        ? Results.Ok(user)
        : Results.NotFound();
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
