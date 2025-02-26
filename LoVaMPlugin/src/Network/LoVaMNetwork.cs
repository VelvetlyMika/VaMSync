using System;
using System.Net.Sockets;
using System.Text;

namespace LoVaMPlugin.Network
{
    public class LoVaMNetwork : INetwork
    {
        private TcpClient _tcpClient;
        private string _serverIP;
        private int _serverPort;

        public bool Init(string ip, int sendPort)
        {
            if (_tcpClient != null) return true;

            _tcpClient = new TcpClient(ip, sendPort) ;
            _serverIP = ip;
            _serverPort = sendPort;

            return true;
        }

        public void Send<T>(T data)
        {
            try
            {
                using (var stream = _tcpClient.GetStream())
                {
                    var payload = "{ \"command\": \"GetToys\" }";
                    var request = "POST /command HTTP/1.1\r\n" +
                                  $"Host: {_serverIP}:{_serverPort}\r\n" +
                                  "User/Agent: VaM\r\n" +
                                  "Accept: */*\r\n" +
                                  "Content-Type: application/json\r\n" +
                                  "X-platform: LoVaM Plugin\r\n" +
                                  $"Content-Length: {payload.Length}\r\n" +
                                  "Connection: close\r\n\r\n" +
                                  payload;
                    
                    byte[] requestBytes = Encoding.ASCII.GetBytes(request);
                    stream.Write(requestBytes, 0, requestBytes.Length);
                    
                    byte[] responseBuffer = new byte[4096];
                    int bytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length);
                    string response = Encoding.ASCII.GetString(responseBuffer, 0, bytesRead);
                    
                    SuperController.LogMessage($"\r\n{request}\r\n\r\n{response}");
                }
            }
            catch (Exception ex)
            {
                SuperController.LogError(ex.Message);
            }
        }

        public void Stop()
        {
            return;
        }
    }
}