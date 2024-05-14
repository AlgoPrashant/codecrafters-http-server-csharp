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
        string directory = "";

        if (args.Length == 2 && args[0] == "--directory")
        {
            directory = args[1];
        }
        else
        {
            Console.WriteLine("Usage: your_server.exe --directory <directory>");
            return;
        }

        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        Console.WriteLine("Server started. Waiting for connections...");

        while (true)
        {
            TcpClient client = await server.AcceptTcpClientAsync();
            Console.WriteLine("Client connected.");

            _ = Task.Run(() => HandleClientAsync(client, directory)); // Start handling in background
        }
    }

    static async Task HandleClientAsync(TcpClient client, string directory)
    {
        try
        {
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[4096];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                string path = ExtractPath(request);

                if (path.StartsWith("/files/"))
                {
                    string filePath = Path.Combine(directory, path.Substring("/files/".Length));

                    if (File.Exists(filePath))
                    {
                        using (FileStream fileStream = File.OpenRead(filePath))
                        {
                            string headers = $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {fileStream.Length}\r\n\r\n";
                            byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
                            await stream.WriteAsync(headerBytes, 0, headerBytes.Length);

                            await fileStream.CopyToAsync(stream);
                        }
                    }
                    else
                    {
                        string notFoundResponse = "HTTP/1.1 404 Not Found\r\n\r\n";
                        byte[] responseBuffer = Encoding.ASCII.GetBytes(notFoundResponse);
                        await stream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                    }
                }
                else
                {
                    // Handle other types of requests (add your logic here)
                }
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
