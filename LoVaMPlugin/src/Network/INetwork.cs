namespace LoVaMPlugin.Network
{
    public interface INetwork
    {
        bool Init(string ip, int sendPort);
        void Send<T>(T payload);
        void Stop();
    }
}