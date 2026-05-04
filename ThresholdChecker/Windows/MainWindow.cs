using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace ThresholdChecker.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private readonly Plugin plugin;

        public MainWindow(Plugin plugin)
            : base("Threshold Checker###Threshold Checker", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.plugin = plugin;

            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(250, 375),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
        }

        public void Dispose() { }

        public override void PreDraw()
        {
            WindowName = plugin.Tracker.IsTracking 
                ? $"Threshold Checker - {plugin.Tracker.TrackedTargetName}###Threshold Checker" 
                : "Threshold Checker###Threshold Checker";
        }

        public override void Draw()
        {
            ImGui.Text("Status: ");
            ImGui.SameLine();

            if (plugin.Tracker.IsTracking)
            {
                ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), "Tracking");
            }
            else
            {
                ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), "Not Tracking");
            }

            var buttonWidth = ImGui.CalcTextSize("Config").X + ImGui.GetStyle().FramePadding.X * 2;
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - buttonWidth);
            if (ImGui.Button("Config", new Vector2(buttonWidth, 24)))
            {
                plugin.ToggleConfigUi();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (plugin.Tracker.IsTracking)
            {
                ImGui.Text("Configuration: ");
                ImGui.SameLine();
                
                if (plugin.Tracker.CurrentKillTimeConfig != null)
                {
                    ImGui.TextColored(new Vector4(0.7f, 0.9f, 1.0f, 1.0f), $"[{plugin.Tracker.CurrentKillTimeConfig.ConfigurationName}]");
                }
                else
                {
                    ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), "None");
                }

                var phase2CheckSize = ImGui.CalcTextSize("Phase 2").X + ImGui.GetStyle().FramePadding.X * 2;
                ImGui.SameLine(ImGui.GetWindowWidth() - phase2CheckSize - 16f);
                
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));
                bool isP2 = plugin.Tracker.IsPhase2;
                if (ImGui.Checkbox("Phase 2", ref isP2))
                {
                    plugin.Tracker.TogglePhase(isP2);
                }
                ImGui.PopStyleVar();

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

                var hpText = $"Health: {plugin.Tracker.CurrentHpPercent:F2}%";
                float hpFraction = (float)(plugin.Tracker.CurrentHpPercent / 100.0);

                Vector4 barColor;
                if (plugin.Tracker.LastResult == null)
                {
                    barColor = new Vector4(0.1f, 0.5f, 0.8f, 1.0f);
                }
                else
                {
                    var tol = plugin.Tracker.CurrentTargetConfig?.TolerancePercent ?? 0.0;
                    var diff = plugin.Tracker.LastResult.Difference;
                    var behind = plugin.Configuration.BehindColor.ToVector4();
                    var tooFast = plugin.Configuration.TooFastColor.ToVector4();
                    var onTrack = plugin.Configuration.OnTrackColor.ToVector4();

                    if (diff > tol)
                    {
                        barColor = behind;
                    }
                    else if (diff < -tol)
                    {
                        barColor = tooFast;
                    }
                    else
                    {
                        barColor = onTrack;
                    }
                }

                ImGui.PushStyleColor(ImGuiCol.PlotHistogram, barColor);
                ImGui.ProgressBar(hpFraction, new Vector2(-1, 30), hpText);
                ImGui.PopStyleColor();

                ImGui.Spacing();

                bool canPrint = plugin.Tracker.LastResult != null;
                if (!canPrint) ImGui.BeginDisabled();
                
                if (ImGui.Button("Print Status to Chat", new Vector2(0, 24)))
                {
                    plugin.ChatManager.PrintStatusToChat(false);
                }
                
                if (!canPrint) ImGui.EndDisabled();
                
                if (!canPrint && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.SetTooltip("Cannot print to chat until the first threshold has been evaluated.");
                }

                ImGui.Spacing();

                if (plugin.Tracker.NextThreshold != null)
                {
                    ImGui.Spacing();

                    var next = plugin.Tracker.NextThreshold;
                    var goalText = $"Next Threshold: {next.TimeMinutes}:{next.TimeSeconds:D2} at {next.TargetHpPercent}%";
                    var goalSize = ImGui.CalcTextSize(goalText);
                    var goalPadding = new Vector2(8f, 4f);
                    var goalWidth = goalSize.X + goalPadding.X * 2f;
                    var goalHeight = goalSize.Y + goalPadding.Y * 2f;

                    ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.08f, 0.45f, 0.78f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1.0f, 1.0f, 1.0f, 0.08f));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
                    ImGui.BeginChild("NextGoalInline", new Vector2(goalWidth, goalHeight), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                    var curPos = ImGui.GetCursorPos();
                    ImGui.SetCursorPos(new Vector2(curPos.X + goalPadding.X, curPos.Y + goalPadding.Y));
                    ImGui.TextColored(new Vector4(1.0f, 1.0f, 1.0f, 1.0f), goalText);
                    ImGui.EndChild();
                    ImGui.PopStyleVar();
                    ImGui.PopStyleColor(2);

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
                ImGui.Text($"Combat Time: {plugin.Tracker.CombatDuration:mm\\:ss}");
            }
            else
            {
                ImGui.TextColored(new Vector4(1.0f, 0.85f, 0.0f, 1.0f), "⚠ Not currently tracking a boss.");
                ImGui.Spacing();
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "How to use:");
                ImGui.BulletText("Click the Config button and create a threshold configuration.");
                ImGui.BulletText("Target the boss that you want tracked and click the button below.");
                ImGui.BulletText("Enter combat and the tracker will monitor pace.");
            }

            ImGui.Separator();
            ImGui.Spacing();

            var buttonText = plugin.Tracker.IsTracking ? "Stop Tracking" : "Start Tracking Target";
            if (ImGui.Button(buttonText, new Vector2(-1, 30)))
            {
                plugin.Tracker.ToggleTracking();
            }

            if (!string.IsNullOrEmpty(plugin.Tracker.ErrorMessage))
            {
                ImGui.Spacing();
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                ImGui.TextWrapped(plugin.Tracker.ErrorMessage);
                ImGui.PopStyleColor();
            }
        }
    }
}
