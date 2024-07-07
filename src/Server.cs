using System.Net;
using System.Net.Sockets;
using System.Text;


string responseOK = "HTTP/1.1 200 OK\r\n\r\n";
string notFound = "HTTP/1.1 404 Not Found\r\n\r\n";
// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
while (true)
{
    var socket = await server.AcceptSocketAsync(); // wait for client+

    var buffer = new byte[1024];
    var bytesRecived = await socket.ReceiveAsync(buffer);
    var request = Encoding.UTF8.GetString(buffer,0,bytesRecived);


    var path = request.Split("\r\n").FirstOrDefault()?.Split(" ")[1];
    if (path == "/")
    {
        string response = $"{responseOK}Content-Type: text/plain\r\nContent-Length: 0\r\n\r\n";
        var bytesResponse = Encoding.UTF8.GetBytes(response);
        await socket.SendAsync(bytesResponse, SocketFlags.None);
    }
    else if (path.StartsWith("/echo/"))
    {
        var echoString = path.Substring(6); // Get the string after /echo/
        
        // Construct the response
        var responseBody = echoString;
        var contentType = "text/plain";
        var contentLength = Encoding.UTF8.GetByteCount(responseBody).ToString();

        string response = $"{responseOK}Content-Type: {contentType}\r\nContent-Length: {contentLength}\r\n\r\n{responseBody}";
        var bytesResponse = Encoding.UTF8.GetBytes(response);
        await socket.SendAsync(bytesResponse, SocketFlags.None);
    }
    else
    {
        var bytesResponse = Encoding.UTF8.GetBytes(notFound);
        await socket.SendAsync(bytesResponse, SocketFlags.None);
    }
    socket.Shutdown(SocketShutdown.Both);
    socket.Close();
}


