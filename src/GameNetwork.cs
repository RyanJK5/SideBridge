using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace SideBridge;

public static class GameClient {
    private const int Port = 80;
    private const string Delimeter = "|";

    private static readonly IPEndPoint HostIP = new(IPAddress.Parse("192.168.2.41"), Port);
    
    private static NetworkStream s_stream;

    public static async Task Connect() {
        TcpClient client = new();
        await client.ConnectAsync(HostIP);
        s_stream = client.GetStream();
        new Task(async () => await Listen()).Start();
    }

    private static async Task Listen() {
        while (true) {
            await ReceiveAction();
        }
    }

    private static async Task ReceiveAction() {
        var buffer = new byte[1_024];
        int received = await s_stream.ReadAsync(buffer);
        
        string message = Encoding.UTF8.GetString(buffer, 0, received);

        int firstPipe = message.IndexOf(Delimeter);
        int secondPipe = message.IndexOf(Delimeter, firstPipe + 1);
        
        var uuid = ulong.Parse(message[0..firstPipe]);
        Player player = Game.EntityWorld.Find<Player>(p => p.UUID == uuid);

        if (secondPipe < 0) {
            int comma = message.IndexOf(",");
            var mousePos = new Vector2(int.Parse(message[(firstPipe + 1)..comma]), int.Parse(message[(comma + 1)..]));
            player.ProcessMouseMove(mousePos);
            return;
        }
        
        PlayerAction action = (PlayerAction) int.Parse(message[(firstPipe + 1)..secondPipe]);
        bool active = int.Parse(message[(secondPipe + 1)..]) == 1;

        player.ProcessAction(action, active);
    }

    public static async Task SendAction(ulong uuid, PlayerAction action, bool active) {
        byte[] bytes = Encoding.UTF8.GetBytes(
            uuid + Delimeter + 
            (int) action + Delimeter + 
            (active ? 1 : 0)
        );
        await s_stream.WriteAsync(bytes);
    }

    public static async Task SendAction(ulong uuid, Vector2 mousePos) {
        byte[] bytes = Encoding.UTF8.GetBytes(
            uuid + Delimeter + 
            mousePos.X + "," +
            mousePos.Y
        );
        await s_stream.WriteAsync(bytes);
    }
}