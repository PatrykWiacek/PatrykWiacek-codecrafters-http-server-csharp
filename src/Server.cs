using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

string responseOK = "HTTP/1.1 200 OK\r\n";
string responseCreated = "HTTP/1.1 201 Created\r\n\r\n";
string notFound = "HTTP/1.1 404 Not Found\r\n\r\n";
string directory = args.Length > 0 ? args[1] : ".";

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

while (true)
{
    var socket = await server.AcceptSocketAsync(); // wait for client+
    _ = Task.Run(async () => await HandleClient(socket));
}

async Task HandleClient(Socket socket)
{
    var buffer = new byte[1024];
    var bytesRecived = await socket.ReceiveAsync(buffer);
    var request = Encoding.UTF8.GetString(buffer, 0, bytesRecived);


    var lines = request.Split("\r\n");
    var requestLine = lines.FirstOrDefault();
    var path = requestLine?.Split(" ")[1];
    var method = requestLine?.Split(" ")[0];

    string acceptEncoding = null;
    foreach (var line in lines)
    {
        if (line.StartsWith("Accept-Encoding:", StringComparison.OrdinalIgnoreCase))
        {
            acceptEncoding = line.Substring(16).Trim();
            break;
        }
    }
    if (method == "GET" && path == "/")
    {
        var response = new StringBuilder();
        response.Append(responseOK);
        response.Append("Content-Type: text/plain\r\n");
        response.Append("Content-Length: 0\r\n");
        response.Append("\r\n");
        var bytesResponse = Encoding.UTF8.GetBytes(response.ToString());
        await socket.SendAsync(bytesResponse, SocketFlags.None);
    }
    else if (method == "GET" && path.StartsWith("/echo/"))
    {
        var echoString = path.Substring(6);
        
        byte[] responseBodyBytes = Encoding.UTF8.GetBytes(echoString);
        var contentType = "text/plain";
        var response = new StringBuilder();
        response.Append(responseOK);
        response.Append($"Content-Type: {contentType}\r\n");

        // Check for supported encoding
        if (acceptEncoding != null && acceptEncoding.Contains("gzip"))
        {
            responseBodyBytes = CompressWithGzip(responseBodyBytes);
            response.Append("Content-Encoding: gzip\r\n");
        }

        response.Append($"Content-Length: {responseBodyBytes.Length}\r\n");
        response.Append("\r\n");
        
        var headerBytes = Encoding.UTF8.GetBytes(response.ToString());
        await socket.SendAsync(headerBytes, SocketFlags.None);
        await socket.SendAsync(responseBodyBytes, SocketFlags.None);
    }
    else if (method == "GET" && path == "/user-agent")
    {
        var userAgent = lines.FirstOrDefault(line => line.StartsWith("User-Agent:", StringComparison.OrdinalIgnoreCase))?.Substring(12).Trim();

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
        else
        {
            var bytesResponse = Encoding.UTF8.GetBytes(notFound);
            await socket.SendAsync(bytesResponse, SocketFlags.None);
        }
    }
    else if(method =="GET" && path.StartsWith("/files/"))
    {
        var filename = path.Substring(7);
        var filepath = Path.Combine(directory, filename);

        if (File.Exists(filepath))
        {
            var fileContent = File.ReadAllText(filepath);
            var contentType = "application/octet-stream";
            var contentLength = fileContent.Length.ToString();

            var response = new StringBuilder();
            response.Append(responseOK);
            response.Append($"Content-Type: {contentType}\r\n");
            response.Append($"Content-Length: {contentLength}\r\n");
            response.Append("\r\n");
            
            var headerBytes = Encoding.UTF8.GetBytes(response.ToString());
            var fileContentBytes = Encoding.UTF8.GetBytes(fileContent);
            await socket.SendAsync(headerBytes, SocketFlags.None);
            await socket.SendAsync(fileContentBytes, SocketFlags.None);
        }
        else
        {
            var bytesResponse = Encoding.UTF8.GetBytes(notFound);
            await socket.SendAsync(bytesResponse, SocketFlags.None);
        }
    }
    else if (method == "POST" && path.StartsWith("/files/"))
    {
        var filename = path.Substring(7);
        var filePath = Path.Combine(directory, filename);

        // Znalezienie nag��wka Content-Length
        var contentLengthHeader = lines.FirstOrDefault(line => line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase));
        if (contentLengthHeader != null)
        {
            var contentLength = int.Parse(contentLengthHeader.Split(":")[1].Trim());

            // Odczyt cia�a ��dania, pomijaj�c nag��wki
            var bodyIndex = request.IndexOf("\r\n\r\n") + 4;
            var requestBody = request.Substring(bodyIndex, contentLength);

            await File.WriteAllTextAsync(filePath, requestBody);

            var bytesResponse = Encoding.UTF8.GetBytes(responseCreated);
            await socket.SendAsync(bytesResponse, SocketFlags.None);
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

byte[] CompressWithGzip(byte[] data)
{
    using (var compressedStrem = new MemoryStream())
    {
        using (var gzipStream = new GZipStream(compressedStrem, CompressionMode.Compress))
        {
            gzipStream.Write(data, 0 ,data.Length);
        }
        return compressedStrem.ToArray();
    }
}
