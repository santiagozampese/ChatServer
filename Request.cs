public class Request
{
    public string? requestType {get; set;} = "message";

    public string? description {get; set;} = null;
    public Request(string? requestType = "message", string? description = null)
    {
        this.requestType = requestType;
        this.description = description;
    }
}