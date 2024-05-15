using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;

class Program
{
    const int PORT = 4221;
    const int BUFFER_SIZE = 1024;

    static byte[] GenerateResponse(string status, string contentType, string responseBody)
    {
        string response = $"HTTP/1.1 {status}\r\n";
        response += $"Content-Type: {contentType}\r\n";
        response += $"Content-Length: {responseBody.Length}\r\n";
        response += "\r\n";
        response += responseBody;

        return Encoding.UTF8.GetBytes(response);
    }

    static void Main(string[] args)
    {
        if (args.Length < 2 || args[0] != "--directory")
        {
            Console.WriteLine("Usage: ./your_server.sh --directory <directory>");
            return;
        }

        using (TcpListener server = new TcpListener(IPAddress.Any, PORT))
        {
            server.Start();
            Console.WriteLine($"Server listening on port {PORT}...");

            while (true)
            {
                using (Socket client = server.AcceptSocket())
                {
                    Console.WriteLine("Connection established");

                    byte[] requestBuffer = new byte[BUFFER_SIZE];
                    int bytesRead = client.Receive(requestBuffer);
                    string request = Encoding.UTF8.GetString(requestBuffer, 0, bytesRead);

                    string[] lines = request.Split(Environment.NewLine);
                    string[] firstLine = lines[0].Split(" ");
                    string method = firstLine[0];
                    string path = firstLine[1];

                    if (path.Equals("/"))
                    {
                        client.Send(GenerateResponse("200 OK", "text/plain", "Nothing"));
                    }
                    else if (method.Equals("POST") && path.StartsWith("/files/"))
                    {
                        HandleFileUpload(client, path, args[1], requestBuffer, bytesRead);
                    }
                    else if (path.Equals("/user-agent"))
                    {
                        string userAgent = lines.FirstOrDefault(l => l.StartsWith("User-Agent:"));
                        userAgent = userAgent?.Substring("User-Agent: ".Length) ?? "Unknown";
                        client.Send(GenerateResponse("200 OK", "text/plain", userAgent));
                    }
                    else if (path.StartsWith("/echo/"))
                    {
                        string word = path.Substring(6);
                        client.Send(GenerateResponse("200 OK", "text/plain", word));
                    }
                    else
                    {
                        client.Send(GenerateResponse("404 Not Found", "text/plain", "Nothing Dipshit"));
                    }
                } 
            }
        } 
    }

    static void HandleFileUpload(Socket client, string path, string directory, byte[] requestBuffer, int bytesRead)
    {
        string filename = path.Substring(7);
        string sanitizedFilename = Path.GetInvalidFileNameChars().Aggregate(filename, (current, c) => current.Replace(c.ToString(), string.Empty));

        try
        {
            string fullPath = Path.Combine(directory, sanitizedFilename);
            if (!fullPath.StartsWith(directory))
            {
                throw new InvalidOperationException("Invalid file path.");
            }

            using (var fs = new FileStream(fullPath, FileMode.Create))
            {
                fs.Write(requestBuffer, bytesRead, requestBuffer.Length - bytesRead);
                while ((bytesRead = client.Receive(requestBuffer)) > 0)
                {
                    fs.Write(requestBuffer, 0, bytesRead);
                }
            }

            client.Send(GenerateResponse("201 Created", "text/plain", "File created successfully"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving file: {ex.Message}");
            client.Send(GenerateResponse("500 Internal Server Error", "text/plain", "Error saving file"));
        }
        finally
        {
            client.Close();
        }
    }
}
