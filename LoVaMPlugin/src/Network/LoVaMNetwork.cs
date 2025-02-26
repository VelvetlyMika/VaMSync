namespace LoVaMPlugin.Network
{
    public class LoVaMNetwork: INetwork
    {
        public bool Init(string ip, int sendPort)
        {
            return true;
        }

        public void Send<T>(T data)
        {
            
        }

        public void Stop()
        {
            return;
        }
    }
}