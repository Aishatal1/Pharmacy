using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Models;

namespace Pharmacy.Middleware;

public class ActivityLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ActivityLoggingMiddleware> _logger;

    public ActivityLoggingMiddleware(RequestDelegate next, ILogger<ActivityLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, PharmaContext dbContext)
    {
        var originalBodyStream = context.Response.Body;

        try
        {
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            var method = context.Request.Method.ToUpper();
            if (method == "POST" || method == "PUT" || method == "PATCH" || method == "DELETE")
            {
                await LogActivityAsync(context, dbContext);
            }

            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception)
        {
            await _next(context);
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LogActivityAsync(HttpContext context, PharmaContext dbContext)
    {
        try
        {
            // Get user ID from claims - FIX: Don't use 0 for unauthenticated users
            int? userId = null;
            var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int parsedUserId))
            {
                userId = parsedUserId;
            }

            // If user is not authenticated, skip logging entirely
            if (!userId.HasValue)
            {
                _logger.LogDebug("Skipping activity log - user not authenticated");
                return;
            }

            // Verify the user actually exists in the database
            var userExists = await dbContext.Users.AnyAsync(u => u.Id == userId.Value);
            if (!userExists)
            {
                _logger.LogWarning($"User with ID {userId} does not exist in database. Skipping activity log.");
                return;
            }

            var path = context.Request.Path.Value ?? "";
            var method = context.Request.Method.ToUpper();
            
            var tableName = GetTableNameFromPath(path);
            if (string.IsNullOrEmpty(tableName))
            {
                return;
            }

            var action = method switch
            {
                "POST" => "CREATE",
                "PUT" => "UPDATE",
                "PATCH" => "UPDATE",
                "DELETE" => "DELETE",
                _ => "UNKNOWN"
            };

            int? recordId = ExtractRecordIdFromPath(path);
            var details = await GetRequestDetailsAsync(context);

            // Check for duplicate logs
            var existingLog = await dbContext.ActivityLogs
                .Where(l => l.UserId == userId.Value)
                .Where(l => l.TableName == tableName)
                .Where(l => l.RecordId == recordId)
                .Where(l => l.Action == action)
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefaultAsync();

            if (existingLog != null && (DateTime.UtcNow - existingLog.Timestamp).TotalSeconds < 1)
            {
                return;
            }

            // Create activity log with valid UserId
            var activityLog = new ActivityLog
            {
                Action = action,
                TableName = tableName,
                RecordId = recordId,
                Details = details,
                Timestamp = DateTime.UtcNow,
                UserId = userId.Value  // Now this is guaranteed to be a valid user ID
            };

            await dbContext.ActivityLogs.AddAsync(activityLog);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log activity");
        }
    }

    private string? GetTableNameFromPath(string path)
    {
        var segments = path.Trim('/').Split('/');
        if (segments.Length == 0) return null;

        var firstSegment = segments[0].ToLower();
        return firstSegment switch
        {
            "customers" => "Customers",
            "products" => "Products",
            "invoices" => "Invoices",
            "transactions" => "Transactions",
            "users" => "Users",
            "sales" => "Sales",
            "activity-logs" => "ActivityLogs",
            _ => null
        };
    }

    private int? ExtractRecordIdFromPath(string path)
    {
        var segments = path.Trim('/').Split('/');
        
        for (int i = 1; i < segments.Length; i++)
        {
            if (int.TryParse(segments[i], out int id))
            {
                return id;
            }
        }
        return null;
    }

    private async Task<string?> GetRequestDetailsAsync(HttpContext context)
    {
        try
        {
            context.Request.EnableBuffering();
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (string.IsNullOrEmpty(body))
            {
                return null;
            }

            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(body);
                return JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
            }
            catch
            {
                return body.Length > 500 ? body.Substring(0, 500) + "..." : body;
            }
        }
        catch
        {
            return null;
        }
    }
}