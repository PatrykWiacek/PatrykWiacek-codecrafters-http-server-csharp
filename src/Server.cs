using System.Net;
using System.Net.Sockets;
using System.Text;


string responeOK = "HTTP/1.1 200 OK\r\n\r\n";
string notFound = "HTTP/1.1 404 Not Found\r\n\r\n";
// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start(); 
var socket = server.AcceptSocket(); // wait for client+

var buffer = new byte[256];
await socket.ReceiveAsync(buffer);

var request = Encoding.UTF8.GetString(buffer);
var path = request.Split("\r\n").FirstOrDefault()?.Split(" ")[1];

var response = path is "/" ? responeOK : notFound ;
var bytesRespone = Encoding.UTF8.GetBytes(response);
socket.Send(bytesRespone);

