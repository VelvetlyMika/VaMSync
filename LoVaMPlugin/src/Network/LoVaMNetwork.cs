using System;
using System.Net.Sockets;
using System.Text;

namespace LoVaMPlugin.Network
{
    public class LoVaMNetwork : INetwork
    {
        private TcpClient _tcpClient;
        private string _request;

        public bool Init(string ip, int port)
        {
            if (_tcpClient != null) return true;

            _tcpClient = new TcpClient(ip, port);
            _request = "POST /command HTTP/1.1\r\n" +
                       $"Host: {ip}:{port}\r\n" +
                       "User/Agent: App\r\n" +
                       "Accept: */*\r\n" +
                       "Content-Type: application/json\r\n" +
                       "X-platform: Test Plugin\r\n" +
                       "Content-Length: {0}\r\n" +
                       "Connection: keep-alive\r\n\r\n" +
                       "{1}";
            
            return true;
        }

        public void Send(string payload)
        {
            try
            {
                var stream = _tcpClient.GetStream();
                SendRequest(stream, payload);
                stream.Flush();
            }
            catch (Exception ex)
            {
                SuperController.LogError(ex.Message);
            }
        }

        public string ReadResponse(string payload)
        {
            try
            {
                var stream = _tcpClient.GetStream();
                SendRequest(stream, payload);

                var response = "";
                var responseBuffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length)) > 0)
                {
                    response += Encoding.ASCII.GetString(responseBuffer, 0, bytesRead);
                }

                //SuperController.LogMessage($"\r\nReceived response:\r\n\r\n{response}");
                stream.Flush();
                return response;
            }
            catch (Exception ex)
            {
                SuperController.LogError(ex.Message);
            }

            return null;
        }

        private void SendRequest(NetworkStream stream, string payload)
        {
            var request = string.Format(_request, payload.Length, payload);
            var requestBytes = Encoding.ASCII.GetBytes(request);
            stream.Write(requestBytes, 0, requestBytes.Length);
            stream.Flush();
            //SuperController.LogMessage($"\r\nSent request:\r\n\r\n{request}\r\n");
        }

        public void Stop()
        {
            return;
        }
    }
}