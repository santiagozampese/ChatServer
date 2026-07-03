using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
string port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();
app.UseWebSockets();

var Rooms = new ConcurrentDictionary<string , Room>();

Rooms.TryAdd("Default", new Room("Default", null, 30));

app.Map("/ws", async (HttpContext context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        byte[] buffer = new byte[1024 * 4];

        var room = context.Request.Query["room"].ToString();

        using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

        (bool completed, WebSocketReceiveResult? result) = await ReceiveRequests(webSocket, Rooms);

        if (!completed)
        {         
            string json = JsonConvert.SerializeObject(Rooms);
            var send = Encoding.UTF8.GetBytes(json);

            await webSocket.SendAsync(new ArraySegment<byte>(send), WebSocketMessageType.Text, true, CancellationToken.None);

            if (string.IsNullOrEmpty(room))
            {
                if (result != null)
                {           
                    json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Sendable? sendable = JsonConvert.DeserializeObject<Sendable>(json);

                    if (sendable != null && sendable.room != null && sendable.room.name != null) room = sendable.room.name;
                }
            }
            
            Rooms[room].clients.Add(webSocket);
        }

        Console.WriteLine($"Client conncted in room: {room}");

        await ClientTratament(webSocket, room, Rooms);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});

app.Run();

static async Task ClientTratament(WebSocket socket, string roomName, ConcurrentDictionary<string, Room> rooms)
{
    try
    {
        byte[] buffer = new byte[1024 * 4];
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    
            if (result.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            
                byte[] sendBuffer = Encoding.UTF8.GetBytes(message);

                List<WebSocket> roomClients = new();;
                lock (rooms)
                {
                    if (rooms.ContainsKey(roomName))
                    {               
                        roomClients = rooms[roomName].clients.ToList();
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
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnected", CancellationToken.None);
            }
        }
    }
    catch {}
    finally
    {
        lock (rooms)
        {
            if (rooms.ContainsKey(roomName))
            {               
                rooms[roomName].clients.Remove(socket);
            }
        }

        Console.WriteLine($"User left room {roomName}!");
    }
}

static async Task CreateRoom(WebSocket socket, Room room, ConcurrentDictionary<string, Room> rooms)
{
    try
    {          
        if (room != null)
        {
            rooms.TryAdd(room?.name!, room!);
            rooms[room?.name!].clients.Add(socket);
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error: {e.Message}");
    }
}

static async Task<(bool, WebSocketReceiveResult?)> ReceiveRequests(WebSocket socket, ConcurrentDictionary<string, Room> rooms)
{
    WebSocketReceiveResult? result = null;
    try
    {
        byte[] buffer = new byte[1024 * 4];
        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
        Sendable? sendable = JsonConvert.DeserializeObject<Sendable>(json);

        if (sendable != null && sendable.message!.message == "createRoom")
        {
            await CreateRoom(socket, sendable?.room!, rooms);
        }
        return (true, null);
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error to receive: {e.Message}");
        return (false, result);
    }
}