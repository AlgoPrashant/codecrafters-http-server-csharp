using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    static byte[] generateResponse(string status, string contentType, string responseBody)
    {
        // Status Line
        string response = $"HTTP/1.1 {status}\r\n";

        // Headers
        response += $"Content-Type: {contentType}\r\n";
        response += $"Content-Length: {responseBody.Length}\r\n";
        response += "\r\n";

        // Response Body
        response += responseBody;

        return Encoding.UTF8.GetBytes(response);
    }

    static void Main(string[] args)
    {
        Console.WriteLine("Logs from your program will appear here!");

        // Ensure correct command line arguments
        if (args.Length < 2 || args[0] != "--directory")
        {
            Console.WriteLine("Usage: ./your_server.sh --directory <directory>");
            return;
        }

        // Create TcpListener
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();

        while (true)
        {
            // Create new socket
            Socket client = server.AcceptSocket();
            Console.WriteLine("Connection Established");

            // Create response buffer and get response
            byte[] requestText = new byte[100];
            client.Receive(requestText);

            // Parse request path
            string parsed = Encoding.UTF8.GetString(requestText);
            string[] parsedLines = parsed.Split("\r\n");

            // Capturing specific parts of the request that will always be there
            string method = parsedLines[0].Split(" ")[0]; // GET, POST
            string path = parsedLines[0].Split(" ")[1]; // /echo/apple

            // Logic
            if (path.Equals("/"))
            {
                client.Send(generateResponse("200 OK", "text/plain", "Nothing"));
            }
            else if (method.Equals("POST") && path.StartsWith("/files/"))
            {
                // Extract filename from the request path
                string filename = path.Split("/")[2];
                
                // Read the entire request body
                MemoryStream requestBodyStream = new MemoryStream();
                int bytesRead;
                byte[] buffer = new byte[1024]; // Adjust buffer size as needed
                while ((bytesRead = client.Receive(buffer)) > 0)
                {
                    requestBodyStream.Write(buffer, 0, bytesRead);
                }
                byte[] requestBody = requestBodyStream.ToArray();
                string fileContents = Encoding.UTF8.GetString(requestBody);
                
                // Save the file contents to the specified directory
                string directoryName = args[1];
                string filePath = Path.Combine(directoryName, filename);
                File.WriteAllText(filePath, fileContents);
                
                // Send a response with status code 201 (Created)
                byte[] responseBytes = generateResponse("201 Created", "text/plain", "File created successfully");
                client.Send(responseBytes);

                // Introduce a small delay to allow the client to process the response
                System.Threading.Thread.Sleep(100); // Adjust delay time as needed
            }
            else if (path.Equals("/user-agent"))
            {
                string userAgent = parsedLines[2].Split(" ")[1];
                client.Send(generateResponse("200 OK", "text/plain", userAgent));
            }
            else if (path.StartsWith("/echo"))
            {
                string word = path.Split("/")[2];
                client.Send(generateResponse("200 OK", "text/plain", word));
            }
            else
            {
                client.Send(generateResponse("404 Not Found", "text/plain", "Nothing Dipshit"));
            }

            client.Close();
        }

        //server.Stop(); // This line is unreachable since the loop is infinite
    }
}
