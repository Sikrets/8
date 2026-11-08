using System;
using System.Net;
using System.Net.Sockets;
using Server;
using CommandClasses;
using Newtonsoft.Json;

internal class Program
{
    static Random rnd = new Random();
    static void Main(string[] args)
    {
        const int port = 8080;
        IPAddress address = IPAddress.Parse("127.0.0.1");

        TcpListener server = new TcpListener(address, port);

        bool hasPlayer = false;
        PlayerInfo player1 = null;

        server.Start();
        Console.WriteLine("Server started. Waiting for players...");

        while (true)
        {
            try
            {
                TcpClient client = server.AcceptTcpClient();
                
                NetworkStream ns = client.GetStream();
                StreamReader sr = new StreamReader(ns);
                string resp = sr.ReadLine()!;
                
                var command = JsonConvert.DeserializeObject<ClientCommand>(resp);
                
                switch (command.Type)
                {
                    case CommandClasses.CommandType.START:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Request to start a new game from {command.Nickname}");
                        
                        if (!hasPlayer)
                        {
                            player1 = new PlayerInfo()
                            {
                                Nickname = command.Nickname,
                                TcpClient = client,
                                IsX = Convert.ToBoolean(rnd.Next(2))
                            };
                            player1.SendWaitCommand();
                            hasPlayer = true;
                        }
                        else
                        {
                            PlayerInfo.ResetBoard();

                            PlayerInfo player2 = new PlayerInfo()
                            {
                                Nickname = command.Nickname,
                                TcpClient = client,
                                IsX = !player1.IsX
                            };

                            player1.SendStartCommand(player2.Nickname);
                            player2.SendStartCommand(player1.Nickname);

                            Task.Run(() => player1.StartSession(player2));
                            Task.Run(() => player2.StartSession(player1));

                            hasPlayer = false;
                        }
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"Unknown command from {command.Nickname}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
