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

            // Extract the URL path from the request
            string[] lines = request.Split("\r\n");
            string[] requestLine = lines[0].Split(' ');
            string path = requestLine[1];

            // Extract the string from the URL path
            string str = "";
            Match match = Regex.Match(path, @"\/echo\/(.+)");
            if (match.Success)
            {
                str = match.Groups[1].Value;
                // Prepare the response for /echo/{str}
                string response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {str.Length}\r\n\r\n{str}";
                // Send the response
                byte[] responseBuffer = Encoding.ASCII.GetBytes(response);
                stream.Write(responseBuffer, 0, responseBuffer.Length);
            }
            else if (path == "/")
            {
                // Respond with 200 for root endpoint
                string response = "HTTP/1.1 200 OK\r\n\r\n";
                byte[] responseBuffer = Encoding.ASCII.GetBytes(response);
                stream.Write(responseBuffer, 0, responseBuffer.Length);
            }
            else
            {
                // Respond with 404 for other paths
                string response = "HTTP/1.1 404 Not Found\r\n\r\n";
                byte[] responseBuffer = Encoding.ASCII.GetBytes(response);
                stream.Write(responseBuffer, 0, responseBuffer.Length);
            }

            stream.Close();
            client.Close();
            Console.WriteLine("Response sent. Client disconnected.");
        }
    }
}
