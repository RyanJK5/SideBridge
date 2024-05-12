using System.Threading.Tasks;
using SideBridge;

internal class Program {
    static async Task Main() {
        await GameClient.Connect();
        Game.Start();
    }
}