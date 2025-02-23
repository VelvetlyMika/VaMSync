namespace VaMLaunchPlugin.MotionSources
{
    public interface IMotionSource
    {
        void OnInit(VaMLaunch plugin);
        void OnInitPluginSettings(VaMLaunch plugin);
        bool OnUpdate(ref byte outPos, ref byte outSpeed);
        void OnSimulatorUpdate(float prevPos, float newPos, float deltaTime);
        void OnDestroy(VaMLaunch plugin);
    }
}