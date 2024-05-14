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

            string response = "HTTP/1.1 200 OK\r\n\r\n";
            byte[] buffer = Encoding.ASCII.GetBytes(response);

            stream.Write(buffer, 0, buffer.Length);

            stream.Close();
            client.Close();
            Console.WriteLine("Response sent. Client disconnected.");
        }
    }
}
