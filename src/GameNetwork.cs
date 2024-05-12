using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace SideBridge;

public static class GameClient {

    public static int PlayerCount { get; private set; }

    private const int Port = 80;
    private const string Delimeter = "|";
    private const char Terminator = '$';


    private static readonly IPEndPoint HostIP = new(IPAddress.Parse("192.168.2.41"), Port);
    
    private static NetworkStream s_stream;

    public static async Task Connect() {
        TcpClient client = new();
        await client.ConnectAsync(HostIP);
        s_stream = client.GetStream();
    }

    private static async Task Listen() {
        while (true) {
            await ReceiveAction();
        }
    }

    public static async Task WaitForPlayers(int targetPlayerCount) {
        while (PlayerCount < targetPlayerCount) {
            Console.WriteLine(PlayerCount);
            await ReceiveAction();
        }
        new Task(async () => await Listen()).Start();
    }

    private static async Task ReceiveAction() {
        var buffer = new byte[1_024];
        int received = await s_stream.ReadAsync(buffer);
        string message = Encoding.UTF8.GetString(buffer, 0, received);
        ParseMessage(message);
    }

    private static void ParseMessage(string message) {
        if (message.Contains(Terminator)) {
            foreach (string str in message.Split(Terminator)) {
                if (str.Length > 0) {
                    ParseMessage(str);
                }
            }
            return;
        }

        int firstPipe = message.IndexOf(Delimeter);
        if (firstPipe < 0) {
            PlayerCount = int.Parse(message);
            return;
        }

        int secondPipe = message.IndexOf(Delimeter, firstPipe + 1);
        
        try {
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
        catch (Exception) {
            Console.WriteLine(message);
        }
    }

    public static async Task SendAction(ulong uuid, PlayerAction action, bool active) {
        byte[] bytes = Encoding.UTF8.GetBytes(
            uuid + Delimeter + 
            (int) action + Delimeter + 
            (active ? 1 : 0) + Terminator
        );
        await s_stream.WriteAsync(bytes);
    }

    public static async Task SendAction(ulong uuid, Vector2 mousePos) {
        byte[] bytes = Encoding.UTF8.GetBytes(
            uuid + Delimeter + 
            (int) mousePos.X + "," +
            (int) mousePos.Y + Terminator
        );
        await s_stream.WriteAsync(bytes);
    }
}