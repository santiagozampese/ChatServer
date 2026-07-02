public class Sendable
{
    public string? type {get; set;} = "message";
    public List<Message> messages = new();
    public Message? message = null;

    public List<Room>? rooms = null;
    public Room? room = null;
}