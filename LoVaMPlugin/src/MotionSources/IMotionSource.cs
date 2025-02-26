namespace LoVaMPlugin.MotionSources
{
    public interface IMotionSource
    {
        void OnInit(LoVaM plugin);
        void OnInitPluginSettings(LoVaM plugin);
        bool OnUpdate(ref byte outPos, ref byte outSpeed);
        void OnSimulatorUpdate(float prevPos, float newPos, float deltaTime);
        void OnDestroy(LoVaM plugin);
    }
}