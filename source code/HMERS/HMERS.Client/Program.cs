using System.Net.Sockets;
using System.Text;

using var client = new TcpClient();
client.Connect("127.0.0.1", 5000);               // server ke address pe jao
Console.WriteLine("Server is connected!");

var stream = client.GetStream();
byte[] msg = Encoding.UTF8.GetBytes("Hello from client");
stream.Write(msg, 0, msg.Length);                // message bhejo

var buffer = new byte[1024];
int n = stream.Read(buffer, 0, buffer.Length);   // jawab parho
Console.WriteLine($"Server said: {Encoding.UTF8.GetString(buffer, 0, n)}");
