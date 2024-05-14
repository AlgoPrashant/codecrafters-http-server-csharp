using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

byte[] generateResponse(string status, string contentType, string responseBody) {
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

string readFile(string filepath) {
    // Handling exceptions "higher" up
    string fileContents = "";

    // Read file using stream
    StreamReader fp = new StreamReader(filepath);
    var line = fp.ReadLine();

    while (line != null) {
        fileContents += line;
        line = fp.ReadLine();
    }
    fp.Close();

    return fileContents;
}


// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Create TcpListener
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

while (true) {
    // Create new socket
    Socket client =  server.AcceptSocket();
    Console.WriteLine("Connection Established");

    // Create response buffer and get resonse
    byte[] requestText = new byte[100];
    client.Receive(requestText);

    // Parse request path
    string parsed = System.Text.Encoding.UTF8.GetString(requestText);
    string[] parsedLines = parsed.Split("\r\n");

    // Capturing specific parts of the request that will always be there
    string method = parsedLines[0].Split(" ")[0]; // GET, POST
    string path = parsedLines[0].Split(" ")[1]; // /echo/apple

    // Logic
    if (path.Equals("/")) {
        client.Send(generateResponse("200 OK", "text/plain", "Nothing"));
    }

    // Return if file specified after '/files/' exists, return contents in resonse body
    else if (path.StartsWith("/files/")) {
        // Instructions mention the program WILL be run like this ./program --directory dir
        string directoryName = args[1];
        string filename = path.Split("/")[2];

        try {
            string fileContents = readFile(directoryName + filename);
            client.Send(generateResponse("200 OK", "application/octet-stream", fileContents));
        }
        catch (Exception e){
            client.Send(generateResponse("404 Not Found", "text/plain", "File Not Found"));
        }
    }

    // Return User-Agent in resonse body
    else if (path.Equals("/user-agent")) {
        string userAgent = parsedLines[2].Split(" ")[1];
        client.Send(generateResponse("200 OK", "text/plain", userAgent));
    }

    // Return text after '/echo/' in resonse body
    else if (path.StartsWith("/echo")) {
        string word = path.Split("/")[2];
        client.Send(generateResponse("200 OK", "text/plain", word));
    }

    // You're a loser
    else {
        client.Send(generateResponse("404 Not Found", "text/plain", "Nothing Dipshit"));
    }

    client.Close();
}

server.Stop();