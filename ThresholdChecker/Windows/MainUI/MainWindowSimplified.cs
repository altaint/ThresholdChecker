using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace ThresholdChecker.Windows.MainUI
{
    public class MainWindowSimplified
    {
        private readonly Plugin plugin;

        public MainWindowSimplified(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void Draw()
        {
            MainWindowHelper.DrawConfigurationHeader(plugin);

            ImGui.Spacing();

            if (plugin.Tracker.LastResult != null)
            {
                Vector4 statusColor;
                string statusText;
                double difference = plugin.Tracker.LastResult.Difference;

                if (plugin.Tracker.CurrentPace == PacingState.Behind)
                {
                    statusColor = plugin.Configuration.BehindColor.ToVector4();
                    statusText = $"(BEHIND +{difference:F2}%)";
                }
                else if (plugin.Tracker.CurrentPace == PacingState.TooFast)
                {
                    statusColor = plugin.Configuration.TooFastColor.ToVector4();
                    statusText = $"(TOO FAST {difference:F2}%)";
                }
                else
                {
                    statusColor = plugin.Configuration.OnTrackColor.ToVector4();
                    statusText = $"(ON TRACK {difference:F2}%)";
                }

                var timerText = $"Combat Time: {plugin.Tracker.CombatDuration:mm\\:ss}";
                var statusWidth = ImGui.CalcTextSize(statusText).X;
                var availableWidth = ImGui.GetContentRegionAvail().X;

                ImGui.Text(timerText);
                ImGui.SameLine(availableWidth - statusWidth);

                ImGui.TextColored(statusColor, statusText);
                ImGui.Spacing();
            }
            else
            {
                MainWindowHelper.DrawCombatTimer(plugin);
                ImGui.Spacing();
            }

            MainWindowHelper.DrawNextThreshold(plugin);

            if (plugin.Tracker.NextThreshold != null)
            {
                ImGui.Spacing();
            }
        }
    }
}
