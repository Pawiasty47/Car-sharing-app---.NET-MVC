using projekt_zespołowy.Models;
public class Chat
{
    public Guid Id { get; set; }
    public Guid RideId { get; set; }

    public OfferedRide Ride { get; set; }

    public List<ChatMessage> Messages { get; set; }
}