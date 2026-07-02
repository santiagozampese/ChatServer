public class Sendable
{
    public bool isList {get; set;} = false;
    public List<Message> messages = new();
    public Message? message = null;
}