using System.Windows;

namespace OkoloXray.Managers.Initializers
{
    using Models;
    using Core;
    using Managers;
    using Handlers;
    using Factories;

    public class HandlersInitializer
    {
        public HandlersManager HandlersManager { get; private set; }

        public void Register()
        {
            HandlersManager = new HandlersManager();

            HandlersManager.AddHandler(new SettingsHandler());
            HandlersManager.AddHandler(new TemplateHandler());
            HandlersManager.AddHandler(new ProcessHandler());
            HandlersManager.AddHandler(new ConfigHandler());
            HandlersManager.AddHandler(new ProxyHandler());
            HandlersManager.AddHandler(new TunnelHandler());
            HandlersManager.AddHandler(new NotifyHandler());
            HandlersManager.AddHandler(new VersionHandler());
            HandlersManager.AddHandler(new UpdateHandler());
            HandlersManager.AddHandler(new BroadcastHandler());
            HandlersManager.AddHandler(new DeepLinkHandler());
            HandlersManager.AddHandler(new LinkHandler());
            HandlersManager.AddHandler(new LocalizationHandler());
        }

        public void Setup(
            OkoloXrayCore core, 
            HandlersManager handlersManager, 
            WindowFactory windowFactory
        )
        {
            SetupProcessHandler();
            SetupTunnelHandler();
            SetupConfigHandler();
            SetupUpdateHandler();
            SetupNotifyHandler();
            SetupDeepLinkHandler();
            SetupLocalizationHandler();

            void SetupProcessHandler()
            {
                SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();
                handlersManager.GetHandler<ProcessHandler>().Setup(
                    getTunnelPort: settingsHandler.UserSettings.GetTunPort
                );
            }

            void SetupTunnelHandler()
            {
                ProcessHandler processHandler = handlersManager.GetHandler<ProcessHandler>();

                handlersManager.GetHandler<TunnelHandler>().Setup(
                    onStartTunnelingService: processHandler.TunnelProcess.Start,
                    isServiceRunning: processHandler.TunnelProcess.IsProcessRunning,
                    isServicePortActive: processHandler.TunnelProcess.IsProcessPortActive,
                    connectTunnelingService: processHandler.TunnelProcess.Connect,
                    executeCommand: processHandler.TunnelProcess.Execute
                );
            }

            void SetupConfigHandler()
            {
                SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();

                handlersManager.GetHandler<ConfigHandler>().Setup(
                    getCurrentConfigPath: settingsHandler.UserSettings.GetCurrentConfigPath
                );
            }

            void SetupUpdateHandler()
            {
                VersionHandler versionHandler = handlersManager.GetHandler<VersionHandler>();

                handlersManager.GetHandler<UpdateHandler>().Setup(
                    getApplicationVersion: versionHandler.GetApplicationVersion,
                    convertToAppVersion: versionHandler.ConvertToAppVersion
                );
            }

            void SetupNotifyHandler()
            {
                SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();

                handlersManager.GetHandler<NotifyHandler>().Setup(
                    getMode: settingsHandler.UserSettings.GetMode,
                    onOpenClick: OpenApplication,
                    onUpdateClick: OpenUpdateWindow,
                    onAboutClick: OpenAboutWindow,
                    onCloseClick: CloseApplication,
                    onProxyModeClick: () => { OnModeClick(Mode.PROXY); },
                    onTunnelModeClick: () => { OnModeClick(Mode.TUN); }
                );

                void CloseApplication()
                {
                    Application.Current.Shutdown();
                }
                
                void OpenUpdateWindow() 
                {
                    OpenApplication();
                    if(IsAnotherWindowOpened())
                        CloseOtherWindows();

                    UpdateWindow updateWindow = windowFactory.CreateUpdateWindow();
                    updateWindow.Owner = Application.Current.MainWindow;
                    updateWindow.ShowDialog();
                }

                void OpenAboutWindow()
                {
                    OpenApplication();
                    if(IsAnotherWindowOpened())
                        CloseOtherWindows();

                    AboutWindow aboutWindow = windowFactory.CreateAboutWindow();
                    aboutWindow.Owner = Application.Current.MainWindow;
                    aboutWindow.ShowDialog();
                }

                void OnModeClick(Mode mode) 
                {
                    if (mode == settingsHandler.UserSettings.GetMode())
                        return;

                    MainWindow mainWindow = windowFactory.GetMainWindow();
                    settingsHandler.UpdateMode(mode);
                    mainWindow.TryDisableModeAndRerun();
                }
            }

            void SetupDeepLinkHandler()
            {
                HandlersManager.GetHandler<DeepLinkHandler>().Setup(
                    onReceiveArg: ref PipeManager.OnReceiveArg,
                    onConfigLinkFetched: PrepareToImportConfigLink,
                    onSubscriptionLinkFetched: ImportSubscriptionLink
                );

                void PrepareToImportConfigLink(string link)
                {
                    ServerWindow serverWindow = GetServerWindow();
                    serverWindow.OpenImportConfigWithLinkSection(link);
                    serverWindow.ShowDialog();
                }

                void ImportSubscriptionLink(string link)
                {
                    ServerWindow serverWindow = GetServerWindow();
                    serverWindow.SaveSubscriptionWithLink(link);
                    serverWindow.ShowDialog();
                    (Application.Current.MainWindow as MainWindow).TryAutoConnect();
                }

                ServerWindow GetServerWindow()
                {
                    OpenApplication();
                    if (IsAnotherWindowOpened())
                        CloseOtherWindows();

                    ServerWindow serverWindow = windowFactory.CreateServerWindow();
                    serverWindow.Owner = Application.Current.MainWindow;

                    return serverWindow;
                }
            }

            void SetupLocalizationHandler()
            {
                SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();

                HandlersManager.GetHandler<LocalizationHandler>().Setup(
                    getCurrentLanguage: settingsHandler.UserSettings.GetLanguage
                );
            }
        }
        
        private void OpenApplication()
        {
            var mainWindow = Application.Current.MainWindow;
            mainWindow.Show();
            ForceShowWindowOnTop();
            mainWindow.WindowState = WindowState.Normal;

            void ForceShowWindowOnTop()
            {
                mainWindow.Topmost = true;
                mainWindow.Topmost = false;
                mainWindow.Activate();
            }
        }

        private void CloseOtherWindows()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (!IsMainWindow(window))
                    window.Close();
            }
        }

        private bool IsAnotherWindowOpened() => Application.Current.Windows.Count > 1;

        private bool IsMainWindow(Window window) => window == Application.Current.MainWindow;
    }
}