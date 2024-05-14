using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

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

            // Determine the response based on the path
            string response;
            if (path == "/")
            {
                response = "HTTP/1.1 200 OK\r\n\r\n";
            }
            else
            {
                response = "HTTP/1.1 404 Not Found\r\n\r\n";
            }

            // Send the response
            byte[] responseBuffer = Encoding.ASCII.GetBytes(response);
            stream.Write(responseBuffer, 0, responseBuffer.Length);

            stream.Close();
            client.Close();
            Console.WriteLine("Response sent. Client disconnected.");
        }
    }
}
