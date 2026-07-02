using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using System.Threading.Tasks;

public static class Server
{   
    private static Dictionary<string, List<WebSocket>> _rooms = new();
    public static async Task Main(string[] args)
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
                string roomName = context.Request.QueryString["room"] ?? "default";

                HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
                WebSocket socket = wsContext.WebSocket;

                lock (_rooms)
                {
                    if (!_rooms.ContainsKey(roomName)) _rooms[roomName] = new List<WebSocket>();

                    _rooms[roomName].Add(socket);
                }

                Console.WriteLine($"New user join in room {roomName}");

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

                    List<WebSocket> roomClients;
                    lock (_rooms)
                    {
                        roomClients = new List<WebSocket>(_rooms[roomName]);
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
                if (_rooms.ContainsKey(roomName))
                {
                    _rooms[roomName].Remove(socket);
                    if (_rooms[roomName].Count == 0) _rooms.Remove(roomName);
                }
            }
            Console.WriteLine($"User left room {roomName}");
        }
    }
}
