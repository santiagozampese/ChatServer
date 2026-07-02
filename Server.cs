using System.Net;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
public static class Server
{ 

    public static async Task SendRooms(WebSocket socket, Sendable received)
    {
        if (received.message != null && received.message.message == "roomList")
        {
            Sendable sendable = new Sendable
            {
                type = "roomList",
                //rooms = _rooms.ToList()
            };

            // Add rooms to the sendable object
            string responseJson = JsonConvert.SerializeObject(sendable);
            byte[] responseBuffer = Encoding.UTF8.GetBytes(responseJson);
            await socket.SendAsync(new ArraySegment<byte>(responseBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }  

    
}
