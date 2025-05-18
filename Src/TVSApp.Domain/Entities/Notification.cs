namespace TVS_App.Domain.Entities;

public class Notification : Entity
{
    public Notification(string title, string message)
    {
        Title = title;
        Message = message;
        CreatedAt = DateTime.UtcNow;
    }

    public string Title { get; private set; }
    public string Message { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsRead { get; private set; }

    public void MarkAsRead() => IsRead = true;
}