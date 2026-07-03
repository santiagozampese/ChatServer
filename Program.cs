using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Endpoints;

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
                            bool completed = await ReceiveRequests(socket, result);
                            canSend = false;
                        }
                    }

                    if (canSend)
                    {              
                        lock (Rooms)
                        {
                            if (Rooms.ContainsKey(room!))
                            {               
                                roomClients = Rooms[room!].clients.ToList();
                                break;
                            }
                        }

                        foreach (var client in roomClients)
                        {
                            if (client.State == WebSocketState.Open && client != socket)
                            {
                                await client.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                        }
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
                if (Rooms.ContainsKey(room!))
                {               
                    Rooms[room!].clients.Remove(socket);
                }
            }

            Console.WriteLine($"User left room {room}!");
        }
    }

    static async Task CreateRoom(WebSocket socket, Room room)
    {
        try
        {          
            if (room != null)
            {
                Rooms.TryAdd(room?.name!, room!);
                Rooms[room?.name!].clients.Add(socket);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
    }

    static async Task<bool> ReceiveRequests(WebSocket socket, WebSocketReceiveResult receive)
    {
        try
        {
            byte[] buffer = new byte[1024 * 4];

            string json = Encoding.UTF8.GetString(buffer, 0, receive.Count);
            Sendable? sendable = JsonConvert.DeserializeObject<Sendable>(json);

            if (sendable != null && sendable.message!.message == "createRoom")
            {
                await CreateRoom(socket, sendable?.room!);
            }

            else if (sendable?.message!.message == "connect")
            {
                if (sendable?.room!.name != null) room = sendable.room.name;
            }

            else if (sendable?.message!.message == "getRooms")
            {
                sendable = new() {rooms=Rooms.Values.ToList()};
                await SendObj(sendable, socket);
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
        var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
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