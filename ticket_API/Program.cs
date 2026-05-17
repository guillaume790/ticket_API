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

app.Run();
