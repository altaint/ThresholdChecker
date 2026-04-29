using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using System;
using ThresholdChecker.Windows;
using ThresholdChecker.Windows.Config;

namespace ThresholdChecker
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Threshold Checker";

        private const string CommandName = "/trackboss";

        public WindowSystem WindowSystem = new("ThresholdChecker");
        private MainWindow MainWindow { get; init; }
        private ConfigWindow ConfigWindow { get; init; }
        public Configuration Configuration { get; init; }
        public CombatTracker Tracker { get; init; }
        public ChatManager ChatManager { get; init; }

        public Plugin(IDalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Service>();

            Configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            Tracker = new CombatTracker(Configuration);
            ChatManager = new ChatManager(this);
            MainWindow = new MainWindow(this);
            ConfigWindow = new ConfigWindow(this);

            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(ConfigWindow);

            Service.PluginInterface.UiBuilder.Draw += DrawUI;
            Service.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
            Service.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

            Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the Threshold Checker UI.\n" +
                $"{CommandName} print → Prints the current tracking status to chat.\n" +
                $"{CommandName} config → Opens the configuration window.\n"
            });

            Service.Framework.Update += Tracker.OnFrameworkUpdate;
            Service.ClientState.TerritoryChanged += Tracker.OnTerritoryChanged;
        }

        public void Dispose()
        {
            Service.PluginInterface.UiBuilder.Draw -= DrawUI;
            Service.PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
            Service.PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;

            WindowSystem.RemoveAllWindows();
            MainWindow.Dispose();
            ConfigWindow.Dispose();

            Service.Framework.Update -= Tracker.OnFrameworkUpdate;
            Service.ClientState.TerritoryChanged -= Tracker.OnTerritoryChanged;
            Service.CommandManager.RemoveHandler(CommandName);
        }

        private void DrawUI() => WindowSystem.Draw();
        public void ToggleMainUi() => MainWindow.IsOpen = !MainWindow.IsOpen;
        public void ToggleConfigUi() => ConfigWindow.IsOpen = !ConfigWindow.IsOpen;

        private void OnCommand(string command, string args)
        {
            var trimmed = args.Trim();

            if (trimmed.Equals("print", StringComparison.OrdinalIgnoreCase))
            {
                ChatManager.PrintStatusToChat(true);
            }
            else if (trimmed.Equals("config", StringComparison.OrdinalIgnoreCase))
            {
                ToggleConfigUi();
            }
            else
            {
                ToggleMainUi();
            }
        }

        public void ToggleTracking() => Tracker.ToggleTracking();

        public void TogglePhase(bool isPhase2) => Tracker.TogglePhase(isPhase2);
    }
}
