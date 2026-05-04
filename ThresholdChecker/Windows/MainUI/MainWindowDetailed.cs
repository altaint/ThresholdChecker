using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace ThresholdChecker.Windows.MainUI
{
    public class MainWindowDetailed
    {
        private readonly Plugin plugin;

        public MainWindowDetailed(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void Draw()
        {
            MainWindowHelper.DrawConfigurationHeader(plugin);

            if (plugin.Tracker.LastResult != null)
            {
                ImGui.Spacing();
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "--- Previous Threshold ---");

                Vector4 behind = plugin.Configuration.BehindColor.ToVector4();
                Vector4 tooFast = plugin.Configuration.TooFastColor.ToVector4();
                Vector4 onTrack = plugin.Configuration.OnTrackColor.ToVector4();

                if (plugin.Tracker.LastResult.Difference > plugin.Tracker.CurrentTargetConfig?.TolerancePercent)
                {
                    ImGui.TextColored(behind, $"Actual: {plugin.Tracker.LastResult.ActualHpAtThreshold:F2}% (Behind by {plugin.Tracker.LastResult.Difference:F2}%)");
                }
                else if (plugin.Tracker.LastResult.Difference < -plugin.Tracker.CurrentTargetConfig?.TolerancePercent)
                {
                    var aheadAmount = Math.Abs(plugin.Tracker.LastResult.Difference);
                    ImGui.TextColored(tooFast, $"Actual: {plugin.Tracker.LastResult.ActualHpAtThreshold:F2}% (Fast by {aheadAmount:F2}%)");
                }
                else
                {
                    ImGui.TextColored(onTrack, $"Actual: {plugin.Tracker.LastResult.ActualHpAtThreshold:F2}% (On Track within {plugin.Tracker.CurrentTargetConfig?.TolerancePercent}%)");
                }
                ImGui.Spacing();
            }

            ImGui.Spacing();

            MainWindowHelper.DrawHealthBar(plugin);

            ImGui.Spacing();

            if (plugin.Tracker.NextThreshold != null)
            {
                ImGui.Spacing();

                MainWindowHelper.DrawNextThreshold(plugin);

                ImGui.Spacing();

                ImGui.TextColored(new Vector4(1.0f, 1.0f, 1.0f, 1.0f), "--- Prediction ---");

                if (plugin.Tracker.CurrentPace == PacingState.Behind)
                {
                    ImGui.TextColored(plugin.Configuration.BehindColor.ToVector4(), $"[BEHIND]\nProjected Health: {plugin.Tracker.ProjectedHpPercent:F2}%");
                }
                else if (plugin.Tracker.CurrentPace == PacingState.TooFast)
                {
                    ImGui.TextColored(plugin.Configuration.TooFastColor.ToVector4(), $"[TOO FAST]\nProjected Health: {plugin.Tracker.ProjectedHpPercent:F2}%");
                }
                else
                {
                    ImGui.TextColored(plugin.Configuration.OnTrackColor.ToVector4(), $"[ON TRACK]\nProjected Health: {plugin.Tracker.ProjectedHpPercent:F2}%");
                }
            }

            ImGui.Spacing();
            MainWindowHelper.DrawCombatTimer(plugin);
        }
    }
}
