using projekt_zespołowy.Models;

public class ChatMessage
{
    public Guid Id { get; set; }

    public Guid ChatId { get; set; }
    public Chat Chat { get; set; }

    public Guid SenderId { get; set; } // ✅ GUID
    public User Sender { get; set; }

    public string Content { get; set; }

    public DateTime CreatedAt { get; set; }
}