using System.Net;
using System.Net.Sockets;
using System.Text;


string responseOK = "HTTP/1.1 200 OK\r\n";
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


    var lines = request.Split("\r\n");
    var path = lines.FirstOrDefault()?.Split(" ")[1];

    if (path == "/")
    {
        var response = new StringBuilder();
        response.Append(responseOK);
        response.Append("\r\n");
        var bytesResponse = Encoding.UTF8.GetBytes(response.ToString());
        await socket.SendAsync(bytesResponse, SocketFlags.None);
    }
    else if (path.StartsWith("/echo/"))
    {
        var echoString = path.Substring(6);
        
        var responseBody = echoString;
        var contentType = "text/plain";
        var contentLength = Encoding.UTF8.GetByteCount(responseBody).ToString();

        var response = new StringBuilder();
        response.Append(responseOK);
        response.Append($"Content-Type: {contentType}\r\n");
        response.Append($"Content-Length: {contentLength}\r\n");
        response.Append("\r\n");
        response.Append(responseBody);
        
        var bytesResponse = Encoding.UTF8.GetBytes(response.ToString());
        await socket.SendAsync(bytesResponse, SocketFlags.None);
    }
    else if(path == "/user-agent")
    {
        var userAgent = lines
            .FirstOrDefault(line => line.StartsWith("User-Agent:", StringComparison.CurrentCultureIgnoreCase))
            ?.Substring(12);

        if (userAgent != null)
        {
            var responseBody = userAgent;

            var contentType = "text/plain";
            var contentLength = Encoding.UTF8.GetByteCount(responseBody).ToString();

            var response = new StringBuilder();
            response.Append(responseOK);
            response.Append($"Content-Type: {contentType}\r\n");
            response.Append($"Content-Length: {contentLength}\r\n");
            response.Append("\r\n");
            response.Append(responseBody);

            var bytesResponse = Encoding.UTF8.GetBytes(response.ToString());
            await socket.SendAsync(bytesResponse, SocketFlags.None);
        }
    }
    else if (path.StartsWith("/files/"))
    {
        var filename = path.Substring(7); // Pobierz nazwê pliku po "/files/"
        var directory = "/tmp/"; // Przyk³adowy katalog z plikami, mo¿na zmieniæ na inny
        var filePath = Path.Combine(directory, filename);

        if (File.Exists(filePath))
        {
            var fileContents = await File.ReadAllBytesAsync(filePath);
            var contentType = "application/octet-stream";
            var contentLength = fileContents.Length.ToString();

            string response = $"{responseOK}Content-Type: {contentType}\r\nContent-Length: {contentLength}\r\n\r\n";
            var responseHeaderBytes = Encoding.UTF8.GetBytes(response);
            await socket.SendAsync(responseHeaderBytes, SocketFlags.None);
            await socket.SendAsync(fileContents, SocketFlags.None);
        }
        else
        {
            var bytesResponse = Encoding.UTF8.GetBytes(notFound);
            await socket.SendAsync(bytesResponse, SocketFlags.None);
        }
    }
    else
    {
        var bytesResponse = Encoding.UTF8.GetBytes(notFound);
        await socket.SendAsync(bytesResponse, SocketFlags.None);
    }
    socket.Shutdown(SocketShutdown.Both);
    socket.Close();
}


