using System.Net;
using System.Net.Sockets;
using System.Text;

var listener = new TcpListener(IPAddress.Loopback, 5000); // sirf apne laptop pe, port 5000
listener.Start();
Console.WriteLine("Server is working, port 5000...");

using var client = listener.AcceptTcpClient();   // yahan ruk ke intezar karta hai
Console.WriteLine("Client is connected!");

var stream = client.GetStream();
var buffer = new byte[1024];
int n = stream.Read(buffer, 0, buffer.Length);   // client ka message parho
string msg = Encoding.UTF8.GetString(buffer, 0, n);
Console.WriteLine($"GOT: {msg}");

byte[] reply = Encoding.UTF8.GetBytes("Hello from server");
stream.Write(reply, 0, reply.Length);            // jawab bhejo