using CommandClasses;
using Newtonsoft.Json;
using System;
using System.Net.Sockets;

namespace Server
{
    public class PlayerInfo
    {
        public TcpClient TcpClient { get; set; }
        public string Nickname { get; set; }
        public bool IsX { get; set; }

        private static char[,] board = new char[3, 3];
        private static bool isGameOver = false;

        public static void ResetBoard()
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    board[i, j] = ' ';
            isGameOver = false;
        }

        private void SendCommand(ServerCommand command)
        {
            StreamWriter writer = new StreamWriter(TcpClient.GetStream());
            writer.WriteLine(JsonConvert.SerializeObject(command));
            writer.Flush();
        }

        public void SendCloseCommand(string message = "")
        {
            SendCommand(new ServerCommand(CommandType.CLOSE) { OpponentName = message });
        }
        
        public void SendWaitCommand()
        {
            SendCommand(new ServerCommand(CommandType.WAIT));
        }
        
        public void SendStartCommand(string opponentName)
        {
            ServerCommand command = new ServerCommand(CommandType.START)
            {
                IsX = this.IsX,
                OpponentName = opponentName
            };
            SendCommand(command);
        }
        
        public void SendMoveCommand(CellCoord moveCoord)
        {
            ServerCommand command = new ServerCommand(CommandType.MOVE)
            {
                MoveCoord = moveCoord
            };
            SendCommand(command);
        }

        public ClientCommand ReceiveCommand()
        {
            StreamReader sr = new StreamReader(TcpClient.GetStream());
            string resp = sr.ReadLine()!;
            return JsonConvert.DeserializeObject<ClientCommand>(resp)!;
        }

        public void StartSession(PlayerInfo opponent)
        {
            bool isExit = false;
            while (!isExit)
            {
                try
                {
                    ClientCommand command = ReceiveCommand();
                    if (command == null) break;

                    switch (command.Type)
                    {
                        case CommandType.MOVE:
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"Move on {command.MoveCoord} from {command.Nickname}");

                            if (!isGameOver)
                            {
                                board[command.MoveCoord.RowIndex, command.MoveCoord.ColumnIndex] = IsX ? 'X' : 'O';
                            }

                            opponent.SendMoveCommand(command.MoveCoord);

                            char winner = CheckWinner();
                            if (winner != '\0')
                            {
                                isGameOver = true;
                                string winMsg = winner == 'X' ? "Player 1 (X) Wins!" : "Player 2 (O) Wins!";
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine(winMsg);
                                
                                SendCloseCommand(winMsg);
                                opponent.SendCloseCommand(winMsg);
                                isExit = true;
                            }
                            else if (IsBoardFull())
                            {
                                isGameOver = true;
                                string drawMsg = "It's a Draw!";
                                Console.WriteLine(drawMsg);
                                
                                SendCloseCommand(drawMsg);
                                opponent.SendCloseCommand(drawMsg);
                                isExit = true;
                            }
                            break;

                        case CommandType.CLOSE:
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine($"Close command from {command.Nickname}");
                            isExit = true;
                            break;

                        case CommandType.EXIT:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Exit command from {command.Nickname}");
                            opponent.SendCloseCommand("Opponent left the game");
                            isExit = true;
                            break;
                    }
                }
                catch
                {
                    isExit = true;
                }
            }
        }

        private char CheckWinner()
        {
            for (int i = 0; i < 3; i++)
            {
                if (board[i, 0] != ' ' && board[i, 0] == board[i, 1] && board[i, 1] == board[i, 2]) return board[i, 0];
                if (board[0, i] != ' ' && board[0, i] == board[1, i] && board[1, i] == board[2, i]) return board[0, i];
            }
            if (board[0, 0] != ' ' && board[0, 0] == board[1, 1] && board[1, 1] == board[2, 2]) return board[0, 0];
            if (board[0, 2] != ' ' && board[0, 2] == board[1, 1] && board[1, 1] == board[2, 0]) return board[0, 2];
            return '\0';
        }

        private bool IsBoardFull()
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if (board[i, j] == ' ') return false;
            return true;
        }
    }
}