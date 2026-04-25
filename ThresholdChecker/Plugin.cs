using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using Dalamud.Game.ClientState.Conditions;
using System;
using System.Linq;
using ThresholdChecker.Windows;
using Dalamud.Game.Text;

namespace ThresholdChecker
{
    public class ThresholdResult
    {
        public ThresholdPhase Threshold { get; set; } = null!;
        public double ActualHpAtThreshold { get; set; }
        public double Difference => ActualHpAtThreshold - Threshold.TargetHpPercent;
    }

    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Threshold Checker";

        [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static IChatGui Chat { get; private set; } = null!;
        [PluginService] public static ITargetManager TargetManager { get; private set; } = null!;
        [PluginService] public static IFramework Framework { get; private set; } = null!;
        [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static ICondition Condition { get; private set; } = null!;
        [PluginService] public static IDataManager DataManager { get; private set; } = null!;
        [PluginService] public static IClientState ClientState { get; private set; } = null!;

        private const string CommandName = "/trackboss";
        
        public WindowSystem WindowSystem = new("ThresholdChecker");
        private MainWindow MainWindow { get; init; }
        private ConfigWindow ConfigWindow { get; init; }

        public Configuration Configuration { get; init; }

        public bool IsTracking => isTracking;
        public string TrackedTargetName { get; private set; } = "None";
        public double CurrentHpPercent { get; private set; } = 0.0;
        public TimeSpan CombatDuration { get; private set; } = TimeSpan.Zero;
        

        public TargetConfig? CurrentTargetConfig { get; private set; }
        
        public bool IsPhase2 { get; private set; } = false;

        public ThresholdPhase? NextThreshold { get; private set; }
        public double ProjectedHpPercent { get; private set; }
        public PacingState CurrentPace { get; private set; }

        public ThresholdResult? LastResult { get; private set; }
        public ThresholdPhase? LastEvaluatedThreshold { get; private set; }

        public string ErrorMessage { get; private set; } = string.Empty;

        private bool isTracking = false;
        private ulong trackedObjectId = 0;
        private DateTime? combatStartTime = null;

        public Plugin()
        {
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            MainWindow = new MainWindow(this);
            ConfigWindow = new ConfigWindow(this);
            
            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(ConfigWindow);

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the Threshold Checker UI.\n" +
                $"{CommandName} print → Prints the current tracking status to chat.\n"
            });

            Framework.Update += OnFrameworkUpdate;
            ClientState.TerritoryChanged += OnTerritoryChanged;
        }

        public void Dispose()
        {
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
            PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;

            WindowSystem.RemoveAllWindows();
            MainWindow.Dispose();
            ConfigWindow.Dispose();

            Framework.Update -= OnFrameworkUpdate;
            ClientState.TerritoryChanged -= OnTerritoryChanged;
            CommandManager.RemoveHandler(CommandName);
        }

        private void DrawUI() => WindowSystem.Draw();
        public void ToggleMainUi() => MainWindow.IsOpen = !MainWindow.IsOpen;
        public void ToggleConfigUi() => ConfigWindow.IsOpen = !ConfigWindow.IsOpen;

        private void OnCommand(string command, string args)
        {
            if (args.Trim().Equals("print", StringComparison.OrdinalIgnoreCase))
            {
                PrintStatusToChat(true);
            }
            else
            {
                ToggleMainUi();
            }
        }

        public void PrintStatusToChat(bool fromCommand = false)
        {
            if (!isTracking)
            {
                if (fromCommand)
                {
                    Chat.PrintError("Cannot print: No target is currently being tracked.");
                    return;
                }
            }

            if (LastResult == null)
            {
                if (fromCommand)
                {
                    Chat.PrintError("Cannot print: The first threshold has not been evaluated yet.");
                }
                return;
            }

            string chatMessage = CurrentPace switch
            {
                PacingState.TooFast => Configuration?.TooFastMessage ?? "Too Fast",
                PacingState.Behind => Configuration?.BehindMessage ?? "Behind",
                _ => Configuration?.OnTrackMessage ?? "On Track"
            };

            var diff = Math.Abs(LastResult.Difference);
            chatMessage = chatMessage.Replace("{diff}", diff.ToString("F2"));

            var channel = Configuration?.OutputChannel ?? ChatChannel.Echo;

            var chatType = channel switch
            {
                ChatChannel.Party => XivChatType.Party,
                ChatChannel.Alliance => XivChatType.Alliance,
                ChatChannel.Say => XivChatType.Say,
                ChatChannel.Yell => XivChatType.Yell,
                ChatChannel.Shout => XivChatType.Shout,
                _ => XivChatType.Echo
            };

            Chat.Print(new XivChatEntry
            {
                Type = chatType,
                Message = chatMessage
            });
        }

        public void ToggleTracking()
        {
            ErrorMessage = string.Empty;

            if (isTracking)
            {
                isTracking = false;
                TrackedTargetName = "None";
                CurrentHpPercent = 0;
                NextThreshold = null;
                LastResult = null;
                LastEvaluatedThreshold = null;
                CurrentTargetConfig = null;
                IsPhase2 = false;
                return;
            }

            var target = TargetManager.Target;
            if (target == null)
            {
                ErrorMessage = "You don't have a target to track!";
                return;
            }

            var currentTerritoryId = ClientState.TerritoryType;
            var currentDutyConfig = Configuration.Duties.FirstOrDefault(d => d.TerritoryId == currentTerritoryId);

            if (currentDutyConfig == null)
            {
                ErrorMessage = "Current duty is not configured in the settings!";
                return;
            }

            trackedObjectId = target.GameObjectId;
            TrackedTargetName = target.Name.ToString();
            
            CurrentTargetConfig = currentDutyConfig.Targets.FirstOrDefault(t => string.Equals(t.TargetName, TrackedTargetName, StringComparison.OrdinalIgnoreCase));
            
            if (CurrentTargetConfig == null)
            {
                ErrorMessage = $"The target '{TrackedTargetName}' is not configured for this duty!";
                return;
            }

            isTracking = true;
            IsPhase2 = false;
            LastResult = null;
            LastEvaluatedThreshold = null;
        }

        public void TogglePhase(bool isPhase2)
        {
            if (IsPhase2 != isPhase2)
            {
                IsPhase2 = isPhase2;
                LastResult = null;
                LastEvaluatedThreshold = null;
                NextThreshold = null;
                ProjectedHpPercent = 100.0;
                CurrentPace = PacingState.OnTrack;
            }
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (!isTracking) { return; }

            bool inCombat = Condition[ConditionFlag.InCombat];
            if (inCombat)
            {
                if (combatStartTime == null)
                {
                    combatStartTime = DateTime.Now;
                }

                CombatDuration = DateTime.Now - combatStartTime.Value;
            }
            else
            {
                combatStartTime = null;
                CombatDuration = TimeSpan.Zero;
                NextThreshold = null;
                LastEvaluatedThreshold = null;
                
                LastResult = null;
                ProjectedHpPercent = 100.0;
                CurrentPace = PacingState.OnTrack;
            }

            var target = TargetManager.Target;

            if (target != null && target.GameObjectId == trackedObjectId)
            {
                if (target is Dalamud.Game.ClientState.Objects.Types.IBattleChara battleChara)
                {
                    var maxHp = battleChara.MaxHp;
                    var currentHp = battleChara.CurrentHp;

                    if (maxHp > 0)
                    {
                        CurrentHpPercent = ((double)currentHp / maxHp) * 100.0;
                    }
                }
            }
            else if (!inCombat)
            {
                CurrentHpPercent = 100.0;
            }

            if (isTracking && inCombat && CombatDuration.TotalSeconds > 3 && CurrentTargetConfig != null)
            {
                var activeThresholds = IsPhase2 ? CurrentTargetConfig.P2Thresholds : CurrentTargetConfig.Thresholds;
                var thresholds = activeThresholds.OrderBy(t => t.TotalSeconds).ToList();
                var currentNext = thresholds.FirstOrDefault(t => t.TotalSeconds > CombatDuration.TotalSeconds);

                if (LastEvaluatedThreshold != null && currentNext != LastEvaluatedThreshold)
                {
                    if (CombatDuration.TotalSeconds >= LastEvaluatedThreshold.TotalSeconds)
                    {
                        LastResult = new ThresholdResult
                        {
                            Threshold = LastEvaluatedThreshold,
                            ActualHpAtThreshold = CurrentHpPercent
                        };
                    }
                }

                LastEvaluatedThreshold = currentNext;
                NextThreshold = currentNext;
                
                if (NextThreshold != null)
                {
                    double totalSecs = Math.Max(0.1, CombatDuration.TotalSeconds);
                    
                    double hpDropped = 100.0 - CurrentHpPercent;
                    double dropRatePerSec = hpDropped / totalSecs;
                    
                    ProjectedHpPercent = 100.0 - (dropRatePerSec * NextThreshold.TotalSeconds);
                    
                    if (ProjectedHpPercent > NextThreshold.TargetHpPercent + CurrentTargetConfig.TolerancePercent)
                    {
                        CurrentPace = PacingState.Behind;
                    }
                    else if (ProjectedHpPercent < NextThreshold.TargetHpPercent - CurrentTargetConfig.TolerancePercent)
                    {
                        CurrentPace = PacingState.TooFast;
                    }
                    else
                    {
                        CurrentPace = PacingState.OnTrack;
                    }
                }
            }
        }

        private void OnTerritoryChanged(ushort territoryId)
        {
            if (isTracking)
            {
                isTracking = false;
                TrackedTargetName = "None";
                CurrentHpPercent = 0;
                NextThreshold = null;
                LastResult = null;
                LastEvaluatedThreshold = null;
                CurrentTargetConfig = null;
                ErrorMessage = string.Empty;
            }
        }
    }
}
