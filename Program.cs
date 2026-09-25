using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "User Management API",
        Version = "v1"
    });
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
    });
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Management API v1");
    c.RoutePrefix = "swagger";
});

app.MapGet("/", () => Results.Redirect("/swagger"));

var users = new ConcurrentDictionary<int, User>();
var emailToId = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
var nextId = 1;

void AddSeedUser(string firstName, string lastName, string email, string role)
{
    var user = new User
    {
        FirstName = firstName,
        LastName = lastName,
        Email = email,
        Role = role
    };
    users.TryAdd(nextId, user);
    emailToId.TryAdd(email, nextId);
    nextId++;
}

AddSeedUser("Alex", "Rivera", "alex.rivera@example.com", "Admin");
AddSeedUser("Jamie", "Chen", "jamie.chen@example.com", "User");
AddSeedUser("Sam", "Patel", "sam.patel@example.com", "User");

app.MapGet("/users", () =>
{
    try
    {
        var result = users
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new { Id = kvp.Key, kvp.Value.FirstName, kvp.Value.LastName, kvp.Value.Email, kvp.Value.Role })
            .ToList();

        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.Problem("Unable to retrieve users.");
    }
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError)
.WithOpenApi(operation =>
{
    operation.Summary = "Get all users";
    operation.Description = "Returns a list of all users.";
    return operation;
});

app.MapGet("/users/{id:int}", (int id) =>
{
    try
    {
        if (!users.TryGetValue(id, out var user))
            return Results.NotFound(new { error = $"User with ID {id} was not found." });

        return Results.Ok(new { Id = id, user.FirstName, user.LastName, user.Email, user.Role });
    }
    catch (Exception)
    {
        return Results.Problem("Unable to retrieve the user.");
    }
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status500InternalServerError)
.WithOpenApi(operation =>
{
    operation.Summary = "Get user by ID";
    operation.Description = "Returns a user by ID. Returns 404 if the user does not exist.";
    return operation;
});

app.MapPost("/users", (User user) =>
{
    try
    {
        var validationError = UserValidationService.ValidateUserData(user);
        if (validationError != null)
            return validationError;

        if (UserValidationService.DuplicateEmail(user.Email, emailToId))
            return Results.BadRequest(new { error = "A user with this email already exists." });

        var id = nextId;
        if (!emailToId.TryAdd(user.Email, id))
            return Results.BadRequest(new { error = "A user with this email already exists." });

        if (!users.TryAdd(id, user))
        {
            emailToId.TryRemove(user.Email, out _);
            return Results.Problem("Could not add user.");
        }

        nextId++;
        return Results.Created($"/users/{id}", new { Id = id, user.FirstName, user.LastName, user.Email, user.Role });
    }
    catch (Exception)
    {
        return Results.Problem("Unable to create the user.");
    }
})
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status500InternalServerError)
.WithOpenApi(operation =>
{
    operation.Summary = "Create a new user";
    operation.Description = "Creates a user after validating required fields and email format.";
    return operation;
});

app.MapPut("/users/{id:int}", (int id, User updatedUser) =>
{
    try
    {
        var validationError = UserValidationService.ValidateUserData(updatedUser);
        if (validationError != null)
            return validationError;

        if (!users.TryGetValue(id, out var existingUser))
            return Results.NotFound(new { error = $"User with ID {id} was not found." });

        var newEmail = updatedUser.Email;
        var currentEmail = existingUser.Email;

        if (!string.Equals(newEmail, currentEmail, StringComparison.OrdinalIgnoreCase))
        {
            if (UserValidationService.DuplicateEmail(newEmail, emailToId, id))
                return Results.BadRequest(new { error = "A user with this email already exists." });

            if (!emailToId.TryAdd(newEmail, id))
                return Results.BadRequest(new { error = "A user with this email already exists." });

            emailToId.TryRemove(currentEmail, out _);
        }

        users[id] = updatedUser;
        return Results.Ok(new { Id = id, updatedUser.FirstName, updatedUser.LastName, updatedUser.Email, updatedUser.Role });
    }
    catch (Exception)
    {
        return Results.Problem("Unable to update the user.");
    }
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status500InternalServerError)
.WithOpenApi(operation =>
{
    operation.Summary = "Update an existing user";
    operation.Description = "Updates a user by ID. Returns 404 if the user does not exist.";
    return operation;
});

app.MapDelete("/users/{id:int}", (int id) =>
{
    try
    {
        if (!users.TryRemove(id, out var removedUser))
            return Results.NotFound(new { error = $"User with ID {id} was not found." });

        emailToId.TryRemove(removedUser.Email, out _);
        return Results.NoContent();
    }
    catch (Exception)
    {
        return Results.Problem("Unable to delete the user.");
    }
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status500InternalServerError)
.WithOpenApi(operation =>
{
    operation.Summary = "Delete a user by ID";
    operation.Description = "Deletes a user by ID. Returns 404 if the user does not exist.";
    return operation;
});

app.Run();
