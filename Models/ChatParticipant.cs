using projekt_zespołowy.Models;
public class ChatParticipant
{
    public Guid Id { get; set; }

    public Guid ChatId { get; set; }
    public Chat Chat { get; set; }

    public Guid UserId { get; set; } // ✅ GUID
}