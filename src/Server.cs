using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        Console.WriteLine("Server started. Waiting for connections...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("Client connected.");

            NetworkStream stream = client.GetStream();

            // Read the request
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            // Extract the path from the request
            string path = ExtractPath(request);

            // Check if it's an echo request
            if (path.StartsWith("/echo/"))
            {
                // Extract the string to echo
                string echoString = path.Substring("/echo/".Length);

                // Prepare the response
                string response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {echoString.Length}\r\n\r\n{echoString}";

                // Send the response
                byte[] responseBuffer = Encoding.ASCII.GetBytes(response);
                stream.Write(responseBuffer, 0, responseBuffer.Length);
            }
            else if (path == "/user-agent")
            {
                // Extract the User-Agent header value from the request
                string userAgent = ExtractUserAgent(request);

                // Prepare the response
                string response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {userAgent.Length}\r\n\r\n{userAgent}";

                // Send the response
                byte[] responseBuffer = Encoding.ASCII.GetBytes(response);
                stream.Write(responseBuffer, 0, responseBuffer.Length);
            }
            else
            {
                // Prepare the response for unknown paths
                string response = "HTTP/1.1 404 Not Found\r\n\r\n";

                // Send the response
                byte[] responseBuffer = Encoding.ASCII.GetBytes(response);
                stream.Write(responseBuffer, 0, responseBuffer.Length);
            }

            stream.Close();
            client.Close();
            Console.WriteLine("Response sent. Client disconnected.");
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
        return "Unknown";
    }
}
