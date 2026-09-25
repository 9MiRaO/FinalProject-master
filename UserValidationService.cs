using System.Collections.Concurrent;
using System.Net.Mail;

public static class UserValidationService
{
    public static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public static bool DuplicateEmail(string email, ConcurrentDictionary<string, int> emailToId, int? currentUserId = null)
    {
        if (emailToId.TryGetValue(email, out var existingId))
        {
            if (currentUserId == null || existingId != currentUserId)
                return true;
        }

        return false;
    }

    public static IResult? ValidateUserData(User user)
    {
        if (string.IsNullOrWhiteSpace(user.FirstName) ||
            string.IsNullOrWhiteSpace(user.LastName) ||
            string.IsNullOrWhiteSpace(user.Email) ||
            string.IsNullOrWhiteSpace(user.Role))
        {
            return Results.BadRequest(new { error = "All fields are required and cannot be empty." });
        }

        if (user.FirstName.Length > 50)
            return Results.BadRequest(new { error = "FirstName cannot exceed 50 characters." });

        if (user.LastName.Length > 50)
            return Results.BadRequest(new { error = "LastName cannot exceed 50 characters." });

        if (user.Email.Length > 75)
            return Results.BadRequest(new { error = "Email cannot exceed 75 characters." });

        if (user.Role.Length > 10)
            return Results.BadRequest(new { error = "Role cannot exceed 10 characters." });

        if (!IsValidEmail(user.Email))
            return Results.BadRequest(new { error = "Email is not in a valid format." });

        if (!string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(user.Role, "User", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { error = "Role must be either 'Admin' or 'User'." });
        }

        return null;
    }
}
