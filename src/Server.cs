using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static readonly object _lock = new object();

    static async Task Main(string[] args)
    {
        string directory = "";

        if (args.Length == 2 && args[0] == "--directory")
        {
            // Use the directory specified by the --directory argument
            directory = args[1];
        }
        else if (args.Length > 0)
        {
            // Show usage instructions if invalid arguments are provided
            Console.WriteLine("Usage: your_server.exe --directory <directory>");
            return;
        }
        else
        {
            // Use the default "wwwroot" directory if no argument is provided
            directory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"); 

            // Create the directory if it doesn't exist
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Console.WriteLine($"Created default directory: {directory}");
            }
            else
            {
                Console.WriteLine($"Using default directory: {directory}");
            }
        }

        // Start the TCP listener on port 4221
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        Console.WriteLine("Server started. Waiting for connections...");

        while (true)
        {
            TcpClient client = await server.AcceptTcpClientAsync();
            Console.WriteLine("Client connected.");

            // Handle each client connection in a separate task
            Task.Run(() => HandleClientAsync(client, directory)); 
        }
    }

    static async Task HandleClientAsync(TcpClient client, string directory)
    {
        try
        {
            lock (_lock)
            {
                using (NetworkStream stream = client.GetStream())
                {
                    // Read the HTTP request from the client
                    byte[] buffer = new byte[4096];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    string path = ExtractPath(request);

                    if (path.StartsWith("/files/"))
                    {
                        // Construct the full file path based on the request
                        string filePath = Path.Combine(directory, path.Substring("/files/".Length));

                        if (File.Exists(filePath))
                        {
                            // If the file exists, open it and send the contents
                            using (FileStream fileStream = File.OpenRead(filePath))
                            {
                                // Prepare the HTTP response headers
                                string headers = $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {fileStream.Length}\r\n\r\n";
                                byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
                                stream.Write(headerBytes, 0, headerBytes.Length);

                                // Stream the file contents to the client
                                fileStream.CopyTo(stream);
                            }
                        }
                        else
                        {
                            // If the file doesn't exist, send a 404 Not Found response
                            string notFoundResponse = "HTTP/1.1 404 Not Found\r\n\r\n";
                            byte[] responseBuffer = Encoding.ASCII.GetBytes(notFoundResponse);
                            stream.Write(responseBuffer, 0, responseBuffer.Length);
                        }
                    }
                    else
                    {
                        // Handle other types of requests (add your logic here)
                    }
                }
            }
        }
        finally
        {
            // Close the client connection after handling the request
            client.Close();
            Console.WriteLine("Client disconnected.");
        }
    }

    static string ExtractPath(string request)
    {
        // Helper function to extract the requested path from the HTTP request
        string[] lines = request.Split("\r\n");
        string[] parts = lines[0].Split(" ");
        return parts.Length > 1 ? parts[1] : "";
    }
}
