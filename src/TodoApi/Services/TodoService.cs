using TodoApi.Application;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Domain.Exceptions;
using TodoApi.Models.Requests;
using TodoApi.Models.Responses;

namespace TodoApi.Services;

public class TodoService(
    IUserRepository userRepository,
    ITodoRepository todoRepository,
    IAttachmentStorage attachmentStorage) : ITodoService
{
    private const int MaxAttachments = 3;
    private const int MaxAttachmentBytes = 1_048_576;

    public async Task<IReadOnlyList<TodoListResponse>> ListAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        var items = await todoRepository.ListByUserAsync(user.Id, cancellationToken);
        return items.Select(MapList).ToList();
    }

    public async Task<TodoResponse> CreateAsync(string userId, TodoUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);

        var item = new TodoItem
        {
            Id = Guid.NewGuid(),
            UserRefId = user.Id,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Status = ParseStatus(request.Status),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await ApplyAttachmentsForCreateAsync(item, request.Attachments, cancellationToken);

        await todoRepository.AddAsync(item, cancellationToken);
        await todoRepository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<TodoResponse> UpdateAsync(string userId, Guid id, TodoUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        var item = await todoRepository.FindByIdAsync(id, user.Id, cancellationToken) ?? throw new NotFoundException("Todo not found");

        item.Title = request.Title.Trim();
        item.Description = request.Description?.Trim() ?? string.Empty;
        item.Status = ParseStatus(request.Status);
        item.UpdatedAt = DateTimeOffset.UtcNow;

        if (request.Attachments is not null)
        {
            await ApplyAttachmentsForUpdateAsync(item, request.Attachments, cancellationToken);
        }

        await todoRepository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeleteAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        var item = await todoRepository.FindByIdAsync(id, user.Id, cancellationToken) ?? throw new NotFoundException("Todo not found");
        foreach (var attachment in item.Attachments)
        {
            await attachmentStorage.DeleteAsync(attachment.S3Key, cancellationToken);
        }
        await todoRepository.RemoveAsync(item, cancellationToken);
        await todoRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetUserAsync(string userId, CancellationToken cancellationToken)
        => await userRepository.FindByUserIdAsync(userId, cancellationToken) ?? throw new ForbiddenException();

    private TodoResponse Map(TodoItem item) => new()
    {
        Id = item.Id.ToString(),
        Title = item.Title,
        Description = item.Description,
        Status = item.Status switch
        {
            TodoStatus.Pending => "pending",
            TodoStatus.InProgress => "in_progress",
            TodoStatus.Done => "done",
            _ => throw new ValidationException("Invalid status")
        },
        Attachments = item.Attachments.Select(MapAttachment).ToList()
    };

    private static TodoListResponse MapList(TodoItem item) => new()
    {
        Id = item.Id.ToString(),
        Title = item.Title,
        Description = item.Description,
        Status = item.Status switch
        {
            TodoStatus.Pending => "pending",
            TodoStatus.InProgress => "in_progress",
            TodoStatus.Done => "done",
            _ => throw new ValidationException("Invalid status")
        },
        AttachmentCount = item.Attachments.Count
    };

    private static TodoStatus ParseStatus(string status) => status switch
    {
        "pending" => TodoStatus.Pending,
        "in_progress" => TodoStatus.InProgress,
        "done" => TodoStatus.Done,
        _ => throw new ValidationException("Invalid status")
    };

    private TodoAttachmentResponse MapAttachment(TodoAttachment attachment) => new()
    {
        Id = attachment.Id.ToString(),
        Name = attachment.Name,
        Size = attachment.Size,
        Type = attachment.Type,
        DataUrl = attachmentStorage.GetReadUrl(attachment.S3Key)
    };

    private async Task ApplyAttachmentsForCreateAsync(TodoItem item, List<TodoAttachmentRequest>? attachments, CancellationToken cancellationToken)
    {
        var normalized = NormalizeAttachments(attachments);
        foreach (var input in normalized)
        {
            var key = await attachmentStorage.UploadAsync(item.Id, input.FileName, input.Type, input.Content, cancellationToken);
            item.Attachments.Add(new TodoAttachment
            {
                Id = input.Id ?? Guid.NewGuid(),
                TodoRefId = item.Id,
                Name = input.Name,
                Size = input.Size,
                Type = input.Type,
                S3Key = key,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    private async Task ApplyAttachmentsForUpdateAsync(TodoItem item, List<TodoAttachmentRequest> attachments, CancellationToken cancellationToken)
    {
        var normalized = NormalizeAttachments(attachments);
        var existing = item.Attachments.ToList();
        foreach (var attachment in existing)
        {
            await attachmentStorage.DeleteAsync(attachment.S3Key, cancellationToken);
            item.Attachments.Remove(attachment);
        }

        foreach (var input in normalized)
        {
            var newKey = await attachmentStorage.UploadAsync(item.Id, input.FileName, input.Type, input.Content, cancellationToken);
            item.Attachments.Add(new TodoAttachment
            {
                Id = input.Id ?? Guid.NewGuid(),
                TodoRefId = item.Id,
                Name = input.Name,
                Size = input.Size,
                Type = input.Type,
                S3Key = newKey,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    private static List<NormalizedAttachment> NormalizeAttachments(List<TodoAttachmentRequest>? attachments)
    {
        if (attachments is null || attachments.Count == 0)
        {
            return [];
        }

        if (attachments.Count > MaxAttachments)
        {
            throw new ValidationException($"Attachments must be {MaxAttachments} files or less");
        }

        var normalized = new List<NormalizedAttachment>(attachments.Count);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var attachment in attachments)
        {
            if (string.IsNullOrWhiteSpace(attachment.Name) ||
                string.IsNullOrWhiteSpace(attachment.Type) ||
                string.IsNullOrWhiteSpace(attachment.DataUrl))
            {
                throw new ValidationException("Attachment fields are required");
            }

            var fileName = SanitizeFileName(attachment.Name);
            if (!names.Add(fileName))
            {
                throw new ValidationException("Attachment file names must be unique");
            }

            if (attachment.Size <= 0 || attachment.Size > MaxAttachmentBytes)
            {
                throw new ValidationException("Attachment size exceeds limit");
            }

            var bytes = DecodeDataUrl(attachment.DataUrl);
            if (bytes.Length > MaxAttachmentBytes)
            {
                throw new ValidationException("Attachment size exceeds limit");
            }

            if (bytes.Length != attachment.Size)
            {
                throw new ValidationException("Attachment size mismatch");
            }

            Guid? id = null;
            if (!string.IsNullOrWhiteSpace(attachment.Id))
            {
                if (!Guid.TryParse(attachment.Id, out var parsed))
                {
                    throw new ValidationException("Attachment id is invalid");
                }
                id = parsed;
            }

            normalized.Add(new NormalizedAttachment(
                id,
                fileName,
                attachment.Size,
                attachment.Type.Trim(),
                bytes));
        }

        return normalized;
    }

    private static byte[] DecodeDataUrl(string dataUrl)
    {
        var markerIndex = dataUrl.IndexOf(";base64,", StringComparison.OrdinalIgnoreCase);
        if (markerIndex <= 0)
        {
            throw new ValidationException("Attachment dataUrl is invalid");
        }

        var base64Part = dataUrl[(markerIndex + ";base64,".Length)..];
        try
        {
            return Convert.FromBase64String(base64Part);
        }
        catch (FormatException)
        {
            throw new ValidationException("Attachment dataUrl is invalid");
        }
    }

    private static string SanitizeFileName(string name)
    {
        var fileName = Path.GetFileName(name.Trim());
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ValidationException("Attachment filename is invalid");
        }

        return fileName;
    }

    private sealed record NormalizedAttachment(
        Guid? Id,
        string FileName,
        long Size,
        string Type,
        byte[] Content)
    {
        public string Name => FileName;
    }
}
