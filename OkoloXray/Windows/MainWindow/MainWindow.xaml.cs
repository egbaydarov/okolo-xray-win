﻿using System;
using System.Windows;
using System.ComponentModel;

namespace OkoloXray
{
    using Models;
    using Values;
    using Services;

    public partial class MainWindow : Window
    {
        private bool isRerunRequest;

        private Func<bool> isNeedToShowPolicyWindow;
        private Func<bool> shouldStartHidden;
        private Func<bool> isNeedToAutoConnect;
        private Func<Config> getConfig;
        private Func<Status> loadConfig;
        private Func<Status> enableMode;
        private Func<Status> checkForUpdate;
        private Func<Status> checkForBroadcast;
        private Func<ServerWindow> openServerWindow;
        private Func<SettingsWindow> openSettingsWindow;
        private Func<UpdateWindow> openUpdateWindow;
        private Func<AboutWindow> openAboutWindow;
        private Func<PolicyWindow> openPolicyWindow;
        private Action<string> onRunServer;
        private Action onCancelServer;
        private Action onStopServer;
        private Action onDisableMode;
        private Action onGenerateClientId;
        private Action onGitHubClick;
        private Action onBugReportingClick;
        private Action<string> onCustomLinkClick;

        private BackgroundWorker runWorker;
        private BackgroundWorker broadcastWorker;

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();

        public MainWindow()
        {
            InitializeComponent();
            InitializeRunWorker();
            InitializeUpdateWorker();
            InitializeBroadcastWorker();

            broadcastWorker.RunWorkerAsync();

            void InitializeRunWorker()
            {
                runWorker = new BackgroundWorker();

                runWorker.RunWorkerCompleted += (sender, e) => {
                    if (isRerunRequest)
                    {
                        runWorker.RunWorkerAsync();
                        isRerunRequest = false;
                    }
                };

                runWorker.DoWork += (sender, e) => {
                    Dispatcher.BeginInvoke(new Action(delegate {
                        ShowWaitForRunStatus();
                    }));

                    Status configStatus = loadConfig.Invoke();

                    if (configStatus.Code == Code.ERROR)
                    {
                        Dispatcher.BeginInvoke(new Action(delegate {
                            HandleError();
                            ShowStopStatus();
                        }));

                        return;
                    }

                    Status modeStatus = enableMode.Invoke();

                    if (modeStatus.Code == Code.ERROR)
                    {
                        Dispatcher.BeginInvoke(new Action(delegate {
                            MessageBox.Show(
                                this,
                                modeStatus.Content.ToString(), 
                                Caption.ERROR, 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error
                            );
                            ShowStopStatus();
                        }));

                        return;
                    }
                    else if (modeStatus.Code == Code.INFO)
                    {
                        if (modeStatus.SubCode == SubCode.CANCELED)
                        {
                            Dispatcher.BeginInvoke(new Action(delegate {
                                ShowStopStatus();
                            }));

                            return;
                        }
                    }

                    Dispatcher.BeginInvoke(new Action(delegate {
                        ShowRunStatus();
                    }));

                    onRunServer.Invoke(configStatus.Content.ToString());

                    Dispatcher.BeginInvoke(new Action(delegate {
                        ShowStopStatus();
                    }));

                    void HandleError()
                    {
                        if (IsAnotherWindowOpened())
                            return;
                        
                        ForceShowWindowIfNeeded();

                        switch (configStatus.SubCode)
                        {
                            case SubCode.NO_CONFIG:
                                HandleNoConfigError();
                                break;
                            case SubCode.INVALID_CONFIG:
                                HandleInvalidConfigError();
                                break;
                            default:
                                return;
                        }

                        bool IsWindowHidden() => this.Visibility == Visibility.Hidden;

                        bool IsAnotherWindowOpened() => Application.Current.Windows.Count > 1;

                        void ForceShowWindowIfNeeded()
                        {
                            if (!IsWindowHidden())
                                return;
                            
                            this.Show();
                        }

                        void HandleNoConfigError()
                        {
                        }

                        void HandleInvalidConfigError()
                        {
                            MessageBox.Show(
                                this,
                                configStatus.Content.ToString(), 
                                Caption.ERROR, 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error
                            );
                        }
                    }
                };
            }

            void InitializeUpdateWorker()
            {
            }

            void InitializeBroadcastWorker()
            {
                broadcastWorker = new BackgroundWorker();

                broadcastWorker.DoWork += (sender, e) => {
                    Status broadcastStatus = checkForBroadcast.Invoke();
                    if (IsBroadcastAvailable())
                        Dispatcher.BeginInvoke(new Action(delegate {
                            barBroadcast.Setup(broadcastStatus.Content as Broadcast, onCustomLinkClick);
                            barBroadcast.Appear();
                        }));

                    bool IsBroadcastAvailable() => broadcastStatus.Code == Code.SUCCESS;
                };
            }
        }

        public void Setup(
            Func<bool> isNeedToShowPolicyWindow,
            Func<bool> shouldStartHidden,
            Func<bool> isNeedToAutoConnect,
            Func<Config> getConfig,
            Func<Status> loadConfig, 
            Func<Status> enableMode,
            Func<Status> checkForUpdate,
            Func<Status> checkForBroadcast,
            Func<ServerWindow> openServerWindow,
            Func<SettingsWindow> openSettingsWindow,
            Func<UpdateWindow> openUpdateWindow,
            Func<AboutWindow> openAboutWindow,
            Func<PolicyWindow> openPolicyWindow,
            Action<string> onRunServer,
            Action onStopServer,
            Action onCancelServer,
            Action onDisableMode,
            Action onGenerateClientId,
            Action onGitHubClick,
            Action onBugReportingClick,
            Action<string> onCustomLinkClick)
        {
            this.isNeedToShowPolicyWindow = isNeedToShowPolicyWindow;
            this.shouldStartHidden = shouldStartHidden;
            this.isNeedToAutoConnect = isNeedToAutoConnect;
            this.getConfig = getConfig;
            this.loadConfig = loadConfig;
            this.checkForUpdate = checkForUpdate;
            this.checkForBroadcast = checkForBroadcast;
            this.openServerWindow = openServerWindow;
            this.openSettingsWindow = openSettingsWindow;
            this.openUpdateWindow = openUpdateWindow;
            this.openAboutWindow = openAboutWindow;
            this.openPolicyWindow = openPolicyWindow;
            this.onRunServer = onRunServer;
            this.onCancelServer = onCancelServer;
            this.onStopServer = onStopServer;
            this.enableMode = enableMode;
            this.onDisableMode = onDisableMode;
            this.onGenerateClientId = onGenerateClientId;
            this.onGitHubClick = onGitHubClick;
            this.onBugReportingClick = onBugReportingClick;
            this.onCustomLinkClick = onCustomLinkClick;

            UpdateUI();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            TryOpenPolicyWindow();
            TryStartHidden();
            TryAutoConnect();
        }

        public void UpdateUI()
        {
            Config config = getConfig.Invoke();

            if (config == null)
            {
                textServerConfig.Text = LocalizationService.GetTerm(Localization.NO_SERVER_CONFIGURATION);
                return;
            }
            
            textServerConfig.Text = config.Name.Replace(".json", "");
        }

        public void TryRerun()
        {
            if (!runWorker.IsBusy)
                return;
            
            onStopServer.Invoke();
            isRerunRequest = true;
        }

        public void TryDisableModeAndRerun()
        {
            if (!runWorker.IsBusy)
                return;
            
            onDisableMode.Invoke();
            onStopServer.Invoke();
            isRerunRequest = true;
        }

        private void OnManageServersClick(object sender, RoutedEventArgs e)
        {
            OpenServerWindow();
        }

        private void OnRunButtonClick(object sender, RoutedEventArgs e)
        {
            if (runWorker.IsBusy)
                return;

            runWorker.RunWorkerAsync();
        }

        private void OnStopButtonClick(object sender, RoutedEventArgs e)
        {
            onStopServer.Invoke();
            onDisableMode.Invoke();
            isRerunRequest = false;
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            if (!runWorker.IsBusy)
                return;

            onCancelServer.Invoke();
        }

        private void OnGitHubButtonClick(object sender, RoutedEventArgs e)
        {
            onGitHubClick.Invoke();
        }

        private void OnBugReportingButtonClick(object sender, RoutedEventArgs e)
        {
            onBugReportingClick.Invoke();
        }

        private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            OpenSettingsWindow();
        }

        private void OnUpdateButtonClick(object sender, RoutedEventArgs e)
        {
            OpenUpdateWindow();
        }

        private void OnAboutButtonClick(object sender, RoutedEventArgs e)
        {
            OpenAboutWindow();
        }

        private void TryStartHidden()
        {
            if (!shouldStartHidden.Invoke())
                return;

            if(ShouldAvoidStartHidden())
                return;
            
            OnClosing(new CancelEventArgs());

            bool ShouldAvoidStartHidden() => Application.Current.Windows.Count > 1;
        }

        public void TryAutoConnect()
        {
            if (!isNeedToAutoConnect.Invoke())
                return;
            
            OnRunButtonClick(null, null);
        }

        private void TryOpenPolicyWindow()
        {
            if (!isNeedToShowPolicyWindow.Invoke())
                return;

            onGenerateClientId.Invoke();

            PolicyWindow policyWindow = openPolicyWindow.Invoke();
            policyWindow.Owner = this;
            policyWindow.ShowDialog();
        }

        private void OpenServerWindow()
        {
            ServerWindow serverWindow = openServerWindow.Invoke();
            serverWindow.Owner = this;
            serverWindow.ShowDialog();
        }

        private void OpenSettingsWindow()
        {
            SettingsWindow settingsWindow = openSettingsWindow.Invoke();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        private void OpenUpdateWindow()
        {
            UpdateWindow updateWindow = openUpdateWindow.Invoke();
            updateWindow.Owner = this;
            updateWindow.ShowDialog();
        }

        private void OpenAboutWindow()
        {
            AboutWindow aboutWindow = openAboutWindow.Invoke();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void ShowRunStatus()
        {
            statusRun.Visibility = Visibility.Visible;
            statusStop.Visibility = Visibility.Hidden;
            statusWaitForRun.Visibility = Visibility.Hidden;
        }

        private void ShowStopStatus()
        {
            statusStop.Visibility = Visibility.Visible;
            statusRun.Visibility = Visibility.Hidden;
            statusWaitForRun.Visibility = Visibility.Hidden;
        }

        private void ShowWaitForRunStatus()
        {
            statusWaitForRun.Visibility = Visibility.Visible;
            statusStop.Visibility = Visibility.Hidden;
            statusRun.Visibility = Visibility.Hidden;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
