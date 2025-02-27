namespace LoVaMPlugin.Network
{
    public interface INetwork
    {
        bool Init(string ip, int port);
        void Send(string payload);
        string ReadResponse(string payload);
        void Stop();
    }
}