using System.Net.WebSockets;

public class Room
{
    public string? name {get; set;}

    public HashSet<WebSocket> clients {get; set;} = new HashSet<WebSocket>();
    public string? password {get; set;} = null;
    public int MaxUsers {get; set;} = 10;

    public int CurrentUsers {get; set;} = 0;

    public Room(string name, string? password = null, int maxUsers = 10)
    {
        this.name = name;
        this.password = password;
        this.MaxUsers = maxUsers;
    }
}