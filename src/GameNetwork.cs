using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SideBridge;

public static class GameNetwork {

    private const int Port = 16320;
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
        Console.WriteLine("Created host");
        TcpListener listener = new(EndPoint);
        try {
            listener.Start();

            using TcpClient handler = await listener.AcceptTcpClientAsync();
            await using NetworkStream stream = handler.GetStream();
            
            var buffer = new byte[1_024];
            int recieved = await stream.ReadAsync(buffer);
            Console.WriteLine("recieved " + Encoding.UTF8.GetString(buffer, 0, recieved));
        }
        finally {
            listener.Stop();
        }
    }

    private static async Task CreateClient() {
        Console.WriteLine("Created client");

        using TcpClient client = new();
        await client.ConnectAsync(EndPoint);
        await using NetworkStream stream = client.GetStream();

        await stream.WriteAsync(Encoding.UTF8.GetBytes("Player.Left"));
    }
}