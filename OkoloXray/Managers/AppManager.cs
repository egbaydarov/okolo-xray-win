using System;
using System.Windows;
using System.Threading;

namespace OkoloXray.Managers
{
    using Core;
    using Initializers;
    using Factories;
    using Handlers;
    using Services;
    using Values;

    public class AppManager
    {
        private CoreInitializer coreInitializer;
        private HandlersInitializer handlersInitializer;
        private ServicesInitializer servicesInitializer;
        private FactoriesInitializer factoriesInitializer;

        public OkoloXrayCore Core => coreInitializer.Core;
        public WindowFactory WindowFactory => factoriesInitializer.WindowFactory;
        public HandlersManager HandlersManager => handlersInitializer.HandlersManager;

        private string[] args;
        private static Mutex mutex;
        private const string APP_GUID = "{7I6N0VI4-S9I1-43bl-A0eM-72A47N6EDH8M}";

        public AppManager(string[] args)
        {
            this.args = args;
        }

        public void Initialize()
        {
            AvoidRunningMultipleInstances();
            SetApplicationCurrentDirectory();

            RegisterCore();
            RegisterHandlers();
            RegisterServices();
            RegisterFactories();

            SetupHandlers();
            SetupServices();
            SetupCore();
            SetupFactories();
            DisableModeByDefault();
        }

        private void AvoidRunningMultipleInstances()
        {
            mutex = new Mutex(true, APP_GUID, out bool isCreatedNew);
            
            if(!isCreatedNew)
            {
                if (IsThereAnyArg())
                    PipeManager.SignalOpenedApp(args);
                else
                    ShowAppAlreadyRunningMessageBox();
                
                Environment.Exit(0);
            }

            bool IsThereAnyArg() => args.Length != 0;

            void ShowAppAlreadyRunningMessageBox()
            {
                SettingsHandler settingsHandler = new SettingsHandler();
                
                LocalizationHandler localizationHandler = new LocalizationHandler();
                localizationHandler.Setup(
                    getCurrentLanguage: settingsHandler.UserSettings.GetLanguage
                );

                LocalizationService localizationService = new LocalizationService();
                localizationService.Setup(
                    getLocalizationResource: localizationHandler.GetLocalizationResource
                );

                MessageBox.Show(localizationService.GetTerm(Localization.APP_ALREADY_RUNNING));
            }
        }

        private void SetApplicationCurrentDirectory()
        {
            Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(
                path: Environment.ProcessPath
            );
        }

        private void RegisterCore()
        {
            coreInitializer = new CoreInitializer();
            coreInitializer.Register();
        }

        private void RegisterHandlers()
        {
            handlersInitializer = new HandlersInitializer();
            handlersInitializer.Register();
        }

        private void RegisterServices()
        {
            servicesInitializer = new ServicesInitializer();
            servicesInitializer.Register();
        }

        private void RegisterFactories()
        {
            factoriesInitializer = new FactoriesInitializer();
            factoriesInitializer.Register();
        }

        private void SetupHandlers()
        {
            handlersInitializer.Setup(
                core: coreInitializer.Core,
                handlersManager: handlersInitializer.HandlersManager,
                windowFactory: factoriesInitializer.WindowFactory
            );
        }

        private void SetupServices()
        {
            servicesInitializer.Setup(
                handlersManager: handlersInitializer.HandlersManager
            );
        }

        private void SetupCore()
        {
            coreInitializer.Setup(
                handlersManager: handlersInitializer.HandlersManager
            );
        }

        private void SetupFactories()
        {
            factoriesInitializer.Setup(
                core: coreInitializer.Core,
                handlersManager: handlersInitializer.HandlersManager
            );
        }

        private void DisableModeByDefault()
        {
            coreInitializer.Core.DisableMode();
        }
    }
}