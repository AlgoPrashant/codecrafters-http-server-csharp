using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

class Server
{
    static byte[] generateResponse(string status, string contentType, string responseBody)
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
        // Ensure the --directory argument is provided
        if (args.Length < 2 || args[0] != "--directory")
        {
            Console.WriteLine("Error: Missing or incorrect --directory argument.");
            return; // Exit the program
        }
        
        // Create TcpListener
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();

        while (true)
        {
            // Create new socket
            Socket client = server.AcceptSocket();
            Console.WriteLine("Connection Established");

            // Create response buffer and get resonse
            byte[] requestText = new byte[1024]; // Increased buffer size for larger requests
            client.Receive(requestText);

            // Parse request path
            string parsed = System.Text.Encoding.UTF8.GetString(requestText);
            string[] parsedLines = parsed.Split("\r\n");

            // Capturing specific parts of the request that will always be there
            string method = parsedLines[0].Split(" ")[0]; 
            string path = parsedLines[0].Split(" ")[1];

            // Logic
            if (path.Equals("/")) 
            {
                client.Send(generateResponse("200 OK", "text/plain", "Nothing"));
            }
            // Return if file specified after '/files/' exists, return contents in resonse body
            else if (path.StartsWith("/files/"))
            {
                string directoryName = args[1];
                string filename = path.Split("/")[2];

                try
                {
                    using (StreamReader fp = new StreamReader(Path.Combine(directoryName, filename))) 
                    {
                        string fileContents = fp.ReadToEnd(); 
                        client.Send(generateResponse("200 OK", "application/octet-stream", fileContents));
                    }
                }
                catch (FileNotFoundException)
                {
                    client.Send(generateResponse("404 Not Found", "text/plain", $"File '{filename}' Not Found"));
                }
                catch (Exception ex) 
                {
                    Console.WriteLine($"Error reading file: {ex.Message}");
                    client.Send(generateResponse("500 Internal Server Error", "text/plain", "An error occurred while processing the request."));
                }
            }
            // Return User-Agent in resonse body
            else if (path.Equals("/user-agent"))
            {
                string userAgent = parsedLines.Length > 2 ? parsedLines[2].Split(" ")[1] : "User-Agent not found"; // Handle cases where User-Agent might be missing
                client.Send(generateResponse("200 OK", "text/plain", userAgent));
            }
            // Return text after '/echo/' in resonse body
            else if (path.StartsWith("/echo/")) 
            {
                string word = path.Split("/")[2];
                client.Send(generateResponse("200 OK", "text/plain", word));
            }

            // You're a loser
            else
            {
                client.Send(generateResponse("404 Not Found", "text/plain", "Nothing Dipshit"));
            }
            
            client.Close();
        }
    }
}
