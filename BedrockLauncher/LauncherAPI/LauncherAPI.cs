using Blazored.Modal.Services;
using Blazored.Modal;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Net.NetworkInformation;
using System.Net;
using System.Threading;
using BedrockLauncher.Modals;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace LauncherAPI
{
    public static class LauncherAPI
    {
        public static bool IsValidJson(string input)
        {
            input = input.Trim();
            if ((input.StartsWith("{") && input.EndsWith("}")) || (input.StartsWith("[") && input.EndsWith("]")))
            {
                try
                {
                    var jObject = JObject.Parse(input);

                    foreach (var jo in jObject)
                    {
                        string name = jo.Key;
                        JToken value = jo.Value!;
                        
                        if (value!.Type == JTokenType.Undefined)
                        {
                            return false;
                        }
                    }
                }
                catch (JsonReaderException ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        public static TcpState GetState(this TcpClient tcpClient)
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections().SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint) && x.RemoteEndPoint.Equals(tcpClient.Client.RemoteEndPoint));
            return properties != null ? properties.State : TcpState.Unknown;
        }
    }

    public class RootObject
    {
        public ApiMessageBox? messagebox { get; set; }
    }

    public class ApiMessageBox
    {
        public string? Title { get; set; }
        public string? Text { get; set; }
        public string? Buttons { get; set; }

        public static void SendJsonResponse(int buttonId, TcpClient client)
        {
            NetworkStream networkStream = client.GetStream();
            if (LauncherAPI.GetState(client).ToString().Equals("Established"))
            {
                string json = JsonConvert.SerializeObject(new { response = new { id = buttonId } });
                byte[] bytesToSend = Encoding.ASCII.GetBytes(json);
                networkStream.Write(bytesToSend, 0, bytesToSend.Length);
            }
            client.Close();
        }

        public List<(string, EventCallback)> GetButtonList(TcpClient client)
        {
            if (!String.IsNullOrWhiteSpace(Buttons))
            {
                if (Buttons.Equals("AbortRetryIgnore"))
                {
                    return new List<(string, EventCallback)> 
                    { 
                        ("Abort", new EventCallback(null, (() => { SendJsonResponse(1, client); }))),
                        ("Retry", new EventCallback(null, (() => { SendJsonResponse(2, client); }))),
                        ("Ignore", new EventCallback(null, (() => { SendJsonResponse(3, client); })))
                    };
                }

                if (Buttons.Equals("CancelTryContinue"))
                {
                    return new List<(string, EventCallback)>
                    {
                        ("Cancel", new EventCallback(null, (() => { SendJsonResponse(4, client); }))),
                        ("Try", new EventCallback(null, (() => { SendJsonResponse(5, client); }))),
                        ("Continue", new EventCallback(null, (() => { SendJsonResponse(6, client); })))
                    };
                }

                if (Buttons.Equals("OK")) return new List<(string, EventCallback)> { ("OK", new EventCallback(null, (() => { SendJsonResponse(7, client); }))) };

                if (Buttons.Equals("OKCancel"))
                {
                    return new List<(string, EventCallback)>
                    {
                        ("OK", new EventCallback(null, (() => { SendJsonResponse(7, client); }))),
                        ("Cancel", new EventCallback(null, (() => { SendJsonResponse(4, client); })))
                    };
                }

                if (Buttons.Equals("RetryCancel"))
                {
                    return new List<(string, EventCallback)>
                    {
                        ("Retry", new EventCallback(null, (() => { SendJsonResponse(2, client); }))),
                        ("Cancel", new EventCallback(null, (() => { SendJsonResponse(4, client); })))
                    };
                }

                if (Buttons.Equals("YesNo"))
                {
                    return new List<(string, EventCallback)>
                    {
                        ("Yes", new EventCallback(null, (() => { SendJsonResponse(8, client); }))),
                        ("No", new EventCallback(null, (() => { SendJsonResponse(9, client); })))
                    };
                }

                if (Buttons.Equals("YesNoCancel"))
                {
                    return new List<(string, EventCallback)>
                    {
                        ("Yes", new EventCallback(null, (() => { SendJsonResponse(8, client); }))),
                        ("No", new EventCallback(null, (() => { SendJsonResponse(9, client); }))),
                        ("Cancel", new EventCallback(null, (() => { SendJsonResponse(4, client); })))
                    };
                }
            }
            return new List<(string, EventCallback)> { ("OK", new EventCallback(null, (() => { SendJsonResponse(7, client); }))) };
        }
    }

    public class LauncherServer
    {
        private IModalService? ModalService;

        public bool IsRunning { get; set; }

        public TcpListener? Listener { get; set; }

        public Thread? Thread { get; set; }

        public void StartServer(IModalService ModalService)
        {
            this.ModalService = ModalService;
            IsRunning = true;

            IPAddress localAdd = IPAddress.Parse("127.0.0.1");
            Listener = new(localAdd, 4260);
            Listener.Start();
            Console.WriteLine("Started Server!");

            Thread = new(ServerThread);
            Thread.IsBackground = true;
            Thread.Start();
        }

        private async void ServerThread()
        {
            try
            {
                while (IsRunning)
                {
                    TcpClient client = Listener!.AcceptTcpClient();
                    NetworkStream networkStream = client.GetStream();
                    client.NoDelay = true;

                    if (networkStream.DataAvailable)
                    {
                        byte[] buffer = new byte[client.ReceiveBufferSize];
                        int bytesRead = networkStream.Read(buffer, 0, client.ReceiveBufferSize);

                        string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                        if (LauncherAPI.IsValidJson(dataReceived))
                        {
                            var json = JObject.Parse(dataReceived);
                            if (json.ContainsKey("messagebox"))
                            {
                                var messagebox = JsonConvert.DeserializeObject<RootObject>(dataReceived)!.messagebox;

                                var parameters = new ModalParameters()
                                .Add(nameof(MessageBox.Buttons), messagebox!.GetButtonList(client))
                                .Add(nameof(MessageBox.Message), messagebox.Text!);

                                var modal = ModalService!.Show<MessageBox>(messagebox.Title!, parameters);
                                var result = await modal.Result;

                                if (result.Cancelled)
                                {
                                    ApiMessageBox.SendJsonResponse(0, client);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception) {}
            Listener!.Stop();
        }

        public void StopServer()
        {
            IsRunning = false;
        }
    }
}
