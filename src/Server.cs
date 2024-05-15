using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Logs from your program will appear here!");

        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        byte[] data = new byte[256];
        string RESP_200 = "HTTP/1.1 200 OK\r\n";
        string RESP_201 = "HTTP/1.1 201 Created\r\n\r\n";
        string RESP_404 = "HTTP/1.1 404 Not Found\r\n\r\n";

        while (true)
        {
            Console.Write("Waiting for a connection... ");
            using TcpClient client = server.AcceptTcpClient();
            NetworkStream stream = client.GetStream();
            Console.WriteLine("Connected!");

            int bytesRead = stream.Read(data, 0, data.Length);
            string request = Encoding.ASCII.GetString(data, 0, bytesRead);
            string[] requestData = request.Split("\r\n");
            string requestURL = requestData[0].Split(" ")[1];
            string[] requestArr = requestData[0].Split(" ");
            string requestType = requestArr[0];
            string action = requestURL.Split("/")[1];
            string status = "";

            switch (action)
            {
                case "":
                    status = RESP_200 + "\r\n";
                    break;

                case "files":
                    string directoryName = args[1];
                    string fileName = Path.Combine(directoryName, requestURL.Split("/")[2]);

                    if (requestType == "GET")
                    {
                        if (!File.Exists(fileName))
                        {
                            status = RESP_404;
                        }
                        else
                        {
                            string fileContent = File.ReadAllText(fileName);
                            status = RESP_200 + "Content-Type: application/octet-stream\r\n";
                            status += $"Content-Length: {fileContent.Length}\r\n\r\n{fileContent}";
                        }
                    }
                    else if (requestType == "POST")
                    {
                        string content = requestData[requestData.Length - 1];
                        int length = 0;

                        foreach (string rData in requestData)
                        {
                            if (rData.Contains("Content-Length"))
                            {
                                length = int.Parse(rData.Split(" ")[1]);
                                break;
                            }
                        }

                        File.WriteAllText(fileName, content.Substring(0, length));
                        status = RESP_201;
                    }
                    break;

                case "user-agent":
                    string userAgent = "";
                    foreach (string rData in requestData)
                    {
                        if (rData.Contains("User-Agent"))
                        {
                            userAgent = rData.Split(" ")[1];
                            break;
                        }
                    }
                    status = RESP_200 + $"Content-Type: text/plain\r\nContent-Length: {userAgent.Length}\r\n\r\n{userAgent}";
                    break;

                case "echo":
                    string echoData = requestURL.Split("/")[2];
                    string contentEncoding = "";
                    foreach (string rData in requestData)
                    {
                        if (rData.StartsWith("Accept-Encoding:", StringComparison.OrdinalIgnoreCase))
                        {
                            contentEncoding = rData.Split(":")[1].Trim().ToLower();
                            break;
                        }
                    }

                    bool gzipRequested = contentEncoding.Contains("gzip");

                    StringBuilder responseBuilder = new StringBuilder();
                    responseBuilder.Append(RESP_200);
                    if (gzipRequested)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress, true))
                            {
                                byte[] bytes = Encoding.UTF8.GetBytes(echoData);
                                gzip.Write(bytes, 0, bytes.Length);
                            }
                            byte[] gzipData = ms.ToArray();
                            int gzipDataLength = gzipData.Length;

                            responseBuilder.Append("Content-Encoding: gzip\r\n");
                            responseBuilder.Append($"Content-Length: {gzipDataLength}\r\n\r\n");

                            byte[] headerBytes = Encoding.ASCII.GetBytes(responseBuilder.ToString());
                            stream.Write(headerBytes, 0, headerBytes.Length);

                            stream.Write(gzipData, 0, gzipData.Length);
                        }
                    }
                    else
                    {
                        responseBuilder.Append("Content-Type: text/plain\r\n");
                        responseBuilder.Append($"Content-Length: {echoData.Length}\r\n\r\n{echoData}");
                        status = responseBuilder.ToString();
                    }

                    break;

                default:
                    status = RESP_404;
                    break;
            }

            byte[] response = Encoding.ASCII.GetBytes(status);
            stream.Write(response, 0, response.Length);
        }
    }
}
