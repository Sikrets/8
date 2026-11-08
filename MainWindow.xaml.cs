using System.Data;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CommandClasses;
using Newtonsoft.Json;

namespace Client
{
    public partial class MainWindow : Window
    {
        TcpClient client;

        public bool IsX { get; set; }
        public char Symbol => IsX ? 'X' : 'O';
        public char OpponentSymbol => IsX ? 'O' : 'X';
        public string Nickname { get; set; }
        public string OpponentNickname { get; set; }
        public bool IsPlaying { get; set; }
        private bool isMoving = false;

        public bool IsMoving
        {
            get { return isMoving; }
            set
            {
                isMoving = value;
                var brush = isMoving ? Brushes.White : Brushes.Gray;
                foreach (Border item in fieldGrid.Children.OfType<Border>())
                {
                    item.Background = brush;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            IsPlaying = false;
            IsMoving = false;
        }

        private Task SendCommand(ClientCommand command)
        {
            return Task.Run(() =>
            {
                StreamWriter writer = new StreamWriter(client.GetStream());
                var com = JsonConvert.SerializeObject(command);
                writer.WriteLine(com);
                writer.Flush();
            });
        }

        private Task<ServerCommand> ReceiveCommand()
        {
            return Task.Run(() =>
            {
                StreamReader sr = new StreamReader(client.GetStream());
                string resp = sr.ReadLine()!;
                return JsonConvert.DeserializeObject<ServerCommand>(resp)!;
            });
        }

        private async void Listen()
        {
            try
            {
                bool isExit = false;
                while (!isExit)
                {
                    ServerCommand command = await ReceiveCommand();

                    switch (command.Type)
                    {
                        case CommandClasses.CommandType.WAIT:
                            opponentNameTxtBox.Content = "Waiting...";
                            break;

                        case CommandClasses.CommandType.START:
                            IsX = command.IsX;
                            IsMoving = IsX;
                            symbolLabel.Content = Symbol;
                            OpponentNickname = command.OpponentName;
                            opponentNameTxtBox.Content = OpponentNickname;
                            break;

                        case CommandClasses.CommandType.MOVE:
                            foreach (Border item in fieldGrid.Children.OfType<Border>())
                            {
                                if (item.Tag.Equals(command.MoveCoord))
                                {
                                    ((TextBlock)item.Child).Text = OpponentSymbol.ToString();
                                }
                            }
                            IsMoving = true;
                            break;

                        case CommandClasses.CommandType.CLOSE:
                            if (!string.IsNullOrEmpty(command.OpponentName))
                            {
                                MessageBox.Show(command.OpponentName, "Game Over");
                            }
                            CloseSession();
                            isExit = true;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsPlaying)
                {
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(ipTxtBox.Text), int.Parse(portTxtBox.Text));
                    client = new TcpClient();
                    client.Connect(serverEndPoint);
                    Nickname = nameTxtBox.Text;
                    await SendCommand(new ClientCommand(CommandClasses.CommandType.START, Nickname));
                    Listen();
                    IsPlaying = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void CloseSession()
        {
            await SendCommand(new ClientCommand(CommandClasses.CommandType.CLOSE, Nickname));
            client.Close();
            client = null;
            IsPlaying = false;
            IsMoving = false;
            symbolLabel.Content = "-";
            opponentNameTxtBox.Content = "-";

            foreach (Border item in fieldGrid.Children.OfType<Border>())
            {
                ((TextBlock)item.Child).Text = string.Empty;
            }
        }

        private async void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsMoving)
            {
                ((TextBlock)((Border)sender).Child).Text = Symbol.ToString();

                ClientCommand command = new ClientCommand(CommandClasses.CommandType.MOVE, Nickname)
                {
                    MoveCoord = (CellCoord)((Border)sender).Tag
                };
                await SendCommand(command);
                IsMoving = false;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (IsPlaying)
                SendCommand(new ClientCommand(CommandClasses.CommandType.EXIT, Nickname));
        }
    }
}