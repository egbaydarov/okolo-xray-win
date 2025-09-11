using System;
using System.Windows;
using Microsoft.Win32;

namespace OkoloXray
{
    using Managers;
    using Handlers;
    using Factories;
    using OkoloXray.Utilities;

    public partial class App : Application
    {
        private AppManager appManager;
        private WindowFactory windowFactory;

        protected override void OnStartup(StartupEventArgs e)
        {
            LibLoader.ExtractDll("okolo-TUN.exe");
            LibLoader.ExtractDll("tun2socks.exe");
            LibLoader.ExtractDll("tun.dll");
            LibLoader.ExtractDll("wintun.dll");
            LibLoader.ExtractDll("XrayCore.dll");
            LibLoader.ExtractDll("geosite.dat");
            LibLoader.ExtractDll("geoip.dat");

            InitializeAppManager();
            InitializeNotifyIcon();
            InitializeWindowFactory();
            InitializeMainWindow();
            HandlePipes();
            HandleExitingEvents();

            void InitializeAppManager()
            {
                appManager = new AppManager(e.Args);
                appManager.Initialize();
            }

            void InitializeNotifyIcon()
            {
                appManager.HandlersManager.GetHandler<NotifyHandler>().InitializeNotifyIcon();
            }

            void InitializeWindowFactory()
            {
                windowFactory = appManager.WindowFactory;
            }

            void InitializeMainWindow()
            {
                MainWindow mainWindow = windowFactory.CreateMainWindow();
                mainWindow.Show();
            }

            void HandlePipes()
            {
                if (IsThereAnyArg())
                    PipeManager.SignalThisApp(e.Args);
                
                PipeManager.ListenForPipes();
            }

            void HandleExitingEvents()
            {
                AppDomain.CurrentDomain.ProcessExit += (sender, e) => CleanupBeforeExit();
                SystemEvents.SessionEnded += (sender, e) => CleanupBeforeExit();
            }

            bool IsThereAnyArg() => e.Args.Length != 0;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppDomain.CurrentDomain.ProcessExit -= (sender, e) => CleanupBeforeExit();
            SystemEvents.SessionEnded -= (sender, e) => CleanupBeforeExit();

            base.OnExit(e);
        }

        void CleanupBeforeExit()
        {
            appManager.Core.DisableMode();
        }
    }
}
