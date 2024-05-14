using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        Console.WriteLine("Server started. Waiting for connections...");

        while (true)
        {
            TcpClient client = await server.AcceptTcpClientAsync();
            Console.WriteLine("Client connected.");

            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    static async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            NetworkStream stream = client.GetStream();

            // Read the request asynchronously
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            // Extract the path from the request
            string path = ExtractPath(request);

            // Prepare the response
            string response;
            if (path == "/user-agent")
            {
                // Extract User-Agent header
                string userAgent = ExtractUserAgent(request);
                if (!string.IsNullOrEmpty(userAgent))
                {
                    response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {userAgent.Length}\r\n\r\n{userAgent}";
                }
                else
                {
                    response = "HTTP/1.1 400 Bad Request\r\n\r\n";
                }
            }
            else if (path.StartsWith("/echo/"))
            {
                string echoMessage = path.Substring("/echo/".Length);
                response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {echoMessage.Length}\r\n\r\n{echoMessage}";
            }
            else if (path == "/")
            {
                response = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: 0\r\n\r\n";
            }
            else
            {
                response = "HTTP/1.1 404 Not Found\r\n\r\n";
            }

            // Send the response asynchronously
            byte[] responseBuffer = Encoding.ASCII.GetBytes(response);
            await stream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
        }
        finally
        {
            client.Close();
            Console.WriteLine("Client disconnected.");
        }
    }

    static string ExtractPath(string request)
    {
        string[] lines = request.Split("\r\n");
        string[] parts = lines[0].Split(" ");
        return parts.Length > 1 ? parts[1] : "";
    }

    static string ExtractUserAgent(string request)
    {
        string[] lines = request.Split("\r\n");
        foreach (string line in lines)
        {
            if (line.StartsWith("User-Agent:"))
            {
                return line.Substring("User-Agent:".Length).Trim();
            }
        }
        return null;
    }
}
