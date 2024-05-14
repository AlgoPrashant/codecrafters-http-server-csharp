using System;
using System.Net;
using System.Net.Sockets;

class Program
{
    static void Main(string[] args)
    {
        // You can use print statements as follows for debugging, they'll be visible when running tests.
        Console.WriteLine("Logs from your program will appear here!");

        // Create a TcpListener that listens on any available IP address and port 4221
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();

        Console.WriteLine("Server started. Waiting for connections...");

        try
        {
            while (true)
            {
                // Accept an incoming connection and return a TcpClient object for communication
                TcpClient client = server.AcceptTcpClient();
                
                // Spawn a new thread or task to handle the client connection asynchronously
                // You can handle the client connection here, e.g., by passing it to a method
                // for processing requests.
                HandleClient(client);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            // Handle the error gracefully, possibly by logging and allowing the server to continue running.
        }
        finally
        {
            // Ensure that the server is stopped when the loop exits (e.g., due to an exception).
            server.Stop();
        }
    }

    static void HandleClient(TcpClient client)
    {
        // Implement logic to handle the client connection here.
        // This could involve reading data from the client, processing requests,
        // and sending responses back.
        // Example:
        // NetworkStream stream = client.GetStream();
        // // Read data from the client and send responses
        // stream.Close();
        // client.Close();
    }
}
