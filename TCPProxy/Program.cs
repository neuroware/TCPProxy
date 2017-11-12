using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MITMProxy
{
    class Program
    {
        
        const int BufferSize = 4096;
        static int ThreadsCount;
        static void Main()
        {
            var Configs = File.ReadAllLines("Config.inf");
            List<TcpListener> servers = new List<TcpListener>();
            foreach (var line in Configs)
            {
                var LocalPort = int.Parse(line.Split('<')[0]);
                var RemoteHost = line.Split('<')[1].Split(':')[0];
                var RemotePort = int.Parse(line.Split('<')[1].Split(':')[1]);
                TcpListener Listener = new TcpListener(IPAddress.Any, LocalPort);
                Listener.Start();
                servers.Add(Listener);
                new Task(() =>
                {
                    while (true)
                    {
                        var client = Listener.AcceptTcpClient();
                        new Task(() => AcceptConnection(client, RemoteHost, RemotePort )).Start();
                    }
                }).Start();
                Console.WriteLine("Server listening on port {0} < {1}:{2}.", LocalPort, RemoteHost,RemotePort );
            }
            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
            foreach (var Listener in servers)
            {
                Listener.Stop();
            }
        }

        private static void AcceptConnection(TcpClient client, string host, int port)
        {
            try
            {
                var server = new TcpClient("127.0.0.1", 3000);
                ThreadsCount++;
                new Task(() => ReadFromClient(client, client.GetStream(), server.GetStream())).Start();
                new Task(() => ReadFromServer(server.GetStream(), client.GetStream())).Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private static void ReadFromServer(Stream serverStream, Stream clientStream)
        {
            var message = new byte[BufferSize];
            while (true)
            {
                int serverBytes;
                try
                {
                    serverBytes = serverStream.Read(message, 0, BufferSize);
                    clientStream.Write(message, 0, serverBytes);
                }
                catch
                {
                    break;
                }
                if (serverBytes == 0)
                {
                    break;
                }
            }
        }

        private static void ReadFromClient(TcpClient client, Stream clientStream, Stream serverStream)
        {
            var message = new byte[BufferSize];
            while (true)
            {
                int clientBytes;
                try
                {
                    clientBytes = clientStream.Read(message, 0, BufferSize);
                }
                catch
                {
                    break;
                }
                if (clientBytes == 0)
                {
                    break;
                }
                serverStream.Write(message, 0, clientBytes);
            }
            client.Close();
        }
    }
}

