using Microsoft.EntityFrameworkCore;
using Pharmacy.Data;
using Pharmacy.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Pharmacy.Endpoints;

public static class ActivityLogEndpoints
{
    public static void MapActivityLogEndpoints(this WebApplication app)
    {
        // Get all activity logs with filtering and pagination
        app.MapGet("/activity-logs", async (
            string? action,
            string? tableName,
            int? userId,
            DateTime? startDate,
            DateTime? endDate,
            int? recordId,
            int page,
            int pageSize,
            PharmaContext context) =>
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var query = context.ActivityLogs
                .Include(l => l.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(action))
                query = query.Where(l => l.Action == action.ToUpper());

            if (!string.IsNullOrEmpty(tableName))
                query = query.Where(l => l.TableName == tableName);

            if (userId.HasValue && userId.Value > 0)
                query = query.Where(l => l.UserId == userId.Value);

            if (startDate.HasValue)
                query = query.Where(l => l.Timestamp >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(l => l.Timestamp <= endDate.Value.Date.AddDays(1));

            if (recordId.HasValue)
                query = query.Where(l => l.RecordId == recordId.Value);

            var totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new ActivityLogDto(
                    l.Id,
                    l.Action,
                    l.TableName,
                    l.RecordId,
                    l.Details,
                    l.Timestamp,
                    l.User.FullName
                ))
                .ToListAsync();

            return Results.Ok(new
            {
                Data = logs,
                Pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                },
                Filters = new
                {
                    Action = action,
                    TableName = tableName,
                    UserId = userId,
                    StartDate = startDate,
                    EndDate = endDate,
                    RecordId = recordId
                }
            });
        });

        // Get activity log statistics - SIMPLIFIED VERSION
        app.MapGet("/activity-logs/stats", async (
            DateTime? startDate,
            DateTime? endDate,
            PharmaContext context) =>
        {
            try
            {
                var query = context.ActivityLogs.AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(l => l.Timestamp >= startDate.Value.Date);

                if (endDate.HasValue)
                    query = query.Where(l => l.Timestamp <= endDate.Value.Date.AddDays(1));

                // Total actions
                var totalActions = await query.CountAsync();

                // Actions by type
                var actionsByType = await query
                    .GroupBy(l => l.Action)
                    .Select(g => new { Action = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Actions by table
                var actionsByTable = await query
                    .GroupBy(l => l.TableName)
                    .Select(g => new { Table = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Latest activity
                var latestActivity = await query
                    .OrderByDescending(l => l.Timestamp)
                    .Select(l => new { l.Timestamp, l.Action, l.TableName })
                    .FirstOrDefaultAsync();

                return Results.Ok(new
                {
                    TotalActions = totalActions,
                    ActionsByType = actionsByType,
                    ActionsByTable = actionsByTable,
                    LatestActivity = latestActivity
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error: {ex.Message}");
            }
        });

        // Get activity logs for a specific record
        app.MapGet("/activity-logs/record/{tableName}/{recordId}", async (
            string tableName,
            int recordId,
            PharmaContext context) =>
        {
            var logs = await context.ActivityLogs
                .Include(l => l.User)
                .Where(l => l.TableName == tableName && l.RecordId == recordId)
                .OrderByDescending(l => l.Timestamp)
                .Select(l => new ActivityLogDto(
                    l.Id,
                    l.Action,
                    l.TableName,
                    l.RecordId,
                    l.Details,
                    l.Timestamp,
                    l.User.FullName
                ))
                .ToListAsync();

            if (!logs.Any())
                return Results.NotFound($"No activity logs found for {tableName} with ID {recordId}");

            return Results.Ok(logs);
        });

        // Get user activity summary
        app.MapGet("/activity-logs/user/{userId}/summary", async (
            int userId,
            DateTime? startDate,
            DateTime? endDate,
            PharmaContext context) =>
        {
            var user = await context.Users.FindAsync(userId);
            if (user == null)
                return Results.NotFound($"User with ID {userId} not found");

            var query = context.ActivityLogs.Where(l => l.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(l => l.Timestamp >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(l => l.Timestamp <= endDate.Value.Date.AddDays(1));

            var summary = new
            {
                User = new
                {
                    user.Id,
                    user.FullName,
                    user.Username,
                    user.Role
                },
                Statistics = new
                {
                    TotalActivities = await query.CountAsync(),
                    LastActivity = await query
                        .OrderByDescending(l => l.Timestamp)
                        .Select(l => new { l.Timestamp, l.Action, l.TableName })
                        .FirstOrDefaultAsync(),
                    ActionsByType = await query
                        .GroupBy(l => l.Action)
                        .Select(g => new { Action = g.Key, Count = g.Count() })
                        .ToListAsync(),
                    MostActiveTable = await query
                        .GroupBy(l => l.TableName)
                        .Select(g => new { Table = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .FirstOrDefaultAsync()
                }
            };

            return Results.Ok(summary);
        });
    }
}
