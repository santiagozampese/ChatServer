using System.Text;
using System.Net.WebSockets;
using Newtonsoft.Json;
using System.Collections.Concurrent;
public static class Program
{
    public static WebApplication? app = null;
    public static ConcurrentDictionary<string, Room> Rooms = new();

    public static string? room = null;
    public static void Main(string[] args)
    {  
        var builder = WebApplication.CreateBuilder(args);
        string port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

        app = builder.Build();
        app.UseWebSockets();

        Rooms.TryAdd("Default", new Room("Default", null, 30));

        Config();

        app.Run();
    }

    public static void Config()
    {
        app!.Map("/ws", async (HttpContext context) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

                await ClientTratament(webSocket);
            }
            
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        });
    }

    static async Task ClientTratament(WebSocket socket)
    {
        try
        {
            byte[] buffer = new byte[1024 * 4];
            while (socket.State == WebSocketState.Open)
            {
                bool canSend = true;

                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                
                    byte[] sendBuffer = Encoding.UTF8.GetBytes(message);

                    Sendable? sendable = JsonConvert.DeserializeObject<Sendable>(message);

                    List<WebSocket> roomClients = new();

                    if (sendable != null)
                    {
                        if (sendable.type == "request")
                        {
                            Console.WriteLine("Processing the request!"); 
                            bool completed = await ReceiveRequests(socket, sendable);
                            canSend = false;
                        }
                    }

                    if (canSend)
                    {              
                        lock (Rooms)
                        {   
                            if (sendable != null && sendable.message != null)
                            {          
                                room = sendable.message.toRoom;
                                if (room != null && Rooms.ContainsKey(room!))
                                {               
                                    roomClients = Rooms[room!].clients.ToList();
                                }
                            }
                        }

                        foreach (var client in roomClients)
                        {             
                            if (client.State == WebSocketState.Open && client != socket)
                            {
                                _ = client.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                        }
                        Console.WriteLine($"Message send: [{sendable?.message!.GetFormattedMessage()}] - {room}");
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnected", CancellationToken.None);
                }
            }
        }
        catch {}
        finally
        {
            lock (Rooms)
            {
                if (room != null && Rooms.ContainsKey(room!))
                {               
                    Rooms[room!].clients.Remove(socket);
                    if (Rooms[room!].clients.Count<=0 && room != "Default")
                    {
                        var roomObj = Rooms[room!];
                        Rooms.TryRemove(room!, out roomObj);
                    }
                }
            }

            Console.WriteLine($"User left room {room}!");
        }
    }

    static async Task CreateRoom(WebSocket socket, Room roomObj)
    {
        try
        {          
            if (roomObj != null && roomObj.name != null)
            {
                Rooms.TryAdd(roomObj.name, roomObj);
                Rooms[roomObj.name].clients.Add(socket);
                Console.WriteLine($"Room {roomObj.name} created!");
            }
            else 
            {
                room = "Default";
                Rooms[room].clients.Add(socket);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
    }

    static async Task<bool> ReceiveRequests(WebSocket socket, Sendable sendable)
    {
        try
        {
            if (sendable == null) return false;
            if (sendable.type != "request") return false;
            if (sendable.message == null) return false;

            if (sendable.message.Msg == "createRoom")
            {
                Console.WriteLine("Creating Room!");
                await CreateRoom(socket, sendable.room!);
            }

            else if (sendable.message.Msg == "connect")
            {
                Console.WriteLine("Connecting!");
                if (sendable?.room!.name != null) 
                {
                    room = sendable.room.name;
                }
                else 
                {
                    room = "Default";
                    Rooms[room].clients.Add(socket);
                };

                Console.WriteLine($"User connected to {sendable?.room?.name!}!");
            }

            else if (sendable.message.Msg == "exitRoom")
            {
                Console.WriteLine($"User exiting {sendable.message.toRoom}...");
                if (room != null && Rooms.ContainsKey(room!))
                {               
                    Rooms[room!].clients.Remove(socket);
                    if (Rooms[room!].clients.Count<=0 && room != "Default")
                    {
                        var roomObj = Rooms[room!];
                        Rooms.TryRemove(room!, out roomObj);
                    }
                }
            }

            else if (sendable.message.Msg == "getRooms")
            {
                Console.WriteLine("Sending!");
                Sendable roomList = new() {rooms=Rooms.Values.ToList()};
                await SendObj(roomList, socket);
            }
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error to receive: {e.Message}");
            return false;
        }
    }

    public static async Task<Sendable?> ReceiveObj(WebSocket webSocket)
    {
        byte[] buffer = new byte[1024 * 4];
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        string json = Encoding.UTF8.GetString(buffer, 0, result.Count);

        return JsonConvert.DeserializeObject<Sendable>(json);
    }
    public static async Task SendObj(Sendable sendable, WebSocket webSocket)
    {
        byte[] buffer = new byte[1024 * 4];
        string json = JsonConvert.SerializeObject(sendable);
        var bytes = Encoding.UTF8.GetBytes(json);

        await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
