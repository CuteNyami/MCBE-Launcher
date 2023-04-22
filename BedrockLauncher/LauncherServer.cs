using Blazored.Modal.Services;
using Blazored.Modal;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net;
using System.Threading;
using BedrockLauncher.Modals;
using System.IO;

namespace BedrockLauncher
{
    public class LauncherServer
    {
        private IModalService ModalService;

        public bool IsRunning { get; set; }

        public TcpListener? Listener { get; set; }

        public Thread? Thread { get; set; }

        public void StartServer(IModalService ModalService)
        {
            this.ModalService = ModalService;
            IsRunning = true;

            IPAddress localAdd = IPAddress.Parse("127.0.0.1");
            Listener = new(localAdd, 5000);
            Listener.Start();
            Console.WriteLine("Started Server!");

            Thread = new(ServerThread);
            Thread.IsBackground = true;
            Thread.Start();
        }

        private void ServerThread()
        {
            try
            {
                while (true) 
                {
                    TcpClient client = Listener!.AcceptTcpClient();
                    NetworkStream nwStream = client.GetStream();
                    client.NoDelay = true;

                    byte[] buffer = new byte[client.ReceiveBufferSize];
                    int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);

                    string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    //TODO: make this better
                    if (dataReceived.Equals("test"))
                    {
                        var options1 = new List<(string, EventCallback)>
                        {
                            ("Ok", new EventCallback(null, (
                            () =>
                            {
                                byte[] bytesToSend = Encoding.ASCII.GetBytes("clicked");
                                nwStream.Write(bytesToSend, 0, bytesToSend.Length);
                                client.Close();
                            }
                            )))
                        };

                        var parameters = new ModalParameters()
                        .Add(nameof(MessageBox.Buttons), options1)
                        .Add(nameof(MessageBox.Message), "Test message!");

                        ModalService.Show<MessageBox>("Test message box", parameters);
                    }
                }
            }
            catch (Exception) { }
          
            Listener!.Stop();
        }

        public void StopServer() 
        {
            IsRunning = false;
        }
    }
}
