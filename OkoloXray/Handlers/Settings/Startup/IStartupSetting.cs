namespace OkoloXray.Handlers.Settings.Startup
{
    public interface IStartupSetting
    {
        void EnableRunAtStartup();
        void DisableRunAtStartup();
    }
}