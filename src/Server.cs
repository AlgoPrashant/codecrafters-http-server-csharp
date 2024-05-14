using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length != 2 || args[0] != "--directory")
        {
            Console.WriteLine("Usage: your_server.exe --directory <directory>");
            return;
        }

        string directory = args[1];

        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        Console.WriteLine("Server started. Waiting for connections...");

        while (true)
        {
            TcpClient client = await server.AcceptTcpClientAsync();
            Console.WriteLine("Client connected.");

            _ = Task.Run(() => HandleClientAsync(client, directory));
        }
    }

    static async Task HandleClientAsync(TcpClient client, string directory)
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

            // Check if the request is for a file
            if (path.StartsWith("/files/"))
            {
                string filePath = Path.Combine(directory, path.Substring("/files/".Length));
                if (File.Exists(filePath))
                {
                    // File exists, read its contents
                    byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                    string contentType = "application/octet-stream";

                    // Prepare the response
                    string response = $"HTTP/1.1 200 OK\r\nContent-Type: {contentType}\r\nContent-Length: {fileBytes.Length}\r\n\r\n";
                    byte[] responseBuffer = Encoding.ASCII.GetBytes(response);

                    // Send the response headers
                    await stream.WriteAsync(responseBuffer, 0, responseBuffer.Length);

                    // Send the file contents
                    await stream.WriteAsync(fileBytes, 0, fileBytes.Length);
                }
                else
                {
                    // File does not exist, return 404
                    string notFoundResponse = "HTTP/1.1 404 Not Found\r\n\r\n";
                    byte[] responseBuffer = Encoding.ASCII.GetBytes(notFoundResponse);
                    await stream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                }
            }
            else
            {
                // Handle other types of requests (e.g., /user-agent, /echo)
                // Add your existing request handling logic here
            }
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
}
