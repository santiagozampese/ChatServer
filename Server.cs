using System.Net;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
public static class Server
{
    private static HashSet<Room> _rooms = new();
    public static async Task Start()
    {
        string door = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add($"http://*:{door}/");
        listener.Start();

        Console.WriteLine($"Room Server running in {door}...");

        while (true)
        {
            HttpListenerContext context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                string? roomName = context.Request.QueryString["room"];

                HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);

                WebSocket socket = wsContext.WebSocket;

                if (string.IsNullOrEmpty(roomName))
                {
                    byte[] buffer = new byte[1024 * 4];
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    
                    string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            
                    Sendable? received = JsonConvert.DeserializeObject<Sendable>(json);
                    if (received == null) continue;

                    _ = SendRooms(socket, received);
                    _ = CreateRoom(socket, received);

                    continue;
                }

                lock (_rooms)
                {
                    foreach (var room in _rooms)
                    {
                        if (room.name == roomName)
                        {
                            if (room.MaxUsers<room.CurrentUsers)
                            {
                                room.clients.Add(socket);
                                room.CurrentUsers++;
                                break;           
                            }
                        }              
                    }
                }

                Console.WriteLine($"New user join in room {roomName}!");

                _ = ClientTratament(socket, roomName);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }

        }

    }

    private static async Task ClientTratament(WebSocket socket, string roomName)
    {
        byte[] buffer = new byte[1024 * 4];
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
               
                    byte[] sendBuffer = Encoding.UTF8.GetBytes(message);

                    List<WebSocket> roomClients = new();;
                    lock (_rooms)
                    {
                        foreach (var room in _rooms)
                        {               
                            if (room.name == roomName)
                            {
                                roomClients = room.clients.ToList();
                                break;
                            }
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
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
            }
        }
        catch {}
        finally
        {
            lock (_rooms)
            {
                foreach (var room in _rooms)
                {
                    if (room.name == roomName)
                    {
                        room.clients.Remove(socket);
                        if (room.clients.Count == 0) _rooms.Remove(room);
                        break;
                    }
                }
            }

            Console.WriteLine($"User left room {roomName}!");
        }
    }

    public static async Task SendRooms(WebSocket socket, Sendable received)
    {
        if (received.request != null && received.request.requestType == "roomList")
        {
            Sendable sendable = new Sendable
            {
                type = "roomList",
                rooms = _rooms.ToList()
            };

            // Add rooms to the sendable object
            string responseJson = JsonConvert.SerializeObject(sendable);
            byte[] responseBuffer = Encoding.UTF8.GetBytes(responseJson);
            await socket.SendAsync(new ArraySegment<byte>(responseBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }  

    public static async Task CreateRoom(WebSocket socket, Sendable received)
    {
        if (received.request != null && received.request.requestType == "createRoom")
        {
            if (received.room != null) _rooms.Add(received.room);
        }   
    }
}
