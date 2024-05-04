using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SideBridge;

public static class GameNetwork {

    private const int Port = 80;
    private static readonly IPEndPoint EndPoint = new(IPAddress.Parse("192.168.2.72"), Port);

    public static async Task Start(bool hosting) {
        if (hosting) {
            await CreateServer();
        }
        else {
            await CreateClient();
        }
    }

    private static async Task CreateServer() {
        TcpListener listener = new(EndPoint);
        try {
            listener.Start();

            using TcpClient handler = await listener.AcceptTcpClientAsync();
            await using NetworkStream stream = handler.GetStream();
        }
        finally {
            listener.Stop();
        }
    }

    private static async Task CreateClient() {
        using TcpClient client = new();
        await client.ConnectAsync(EndPoint);
    }

    // public static async Task SendAction(Player player, PlayerAction action) {
        
    // }
}