using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace ThresholdChecker.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private readonly Plugin _plugin;

        public MainWindow(Plugin plugin)
            : base("Threshold Checker###Threshold Checker", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            _plugin = plugin;
            
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(300, 320),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
        }

        public void Dispose() { }

        public override void PreDraw()
        {
            WindowName = _plugin.IsTracking 
                ? $"Threshold Checker - {_plugin.TrackedTargetName}###Threshold Checker" 
                : "Threshold Checker###Threshold Checker";
        }

        public override void Draw()
        {
            Vector4 behind = new Vector4(1.0f, 0.35f, 0.1f, 1.0f);
            Vector4 tooFast = new Vector4(0.6f, 0.5f, 0.0f, 1.0f);
            Vector4 onTrack = new Vector4(0.1f, 0.7f, 0.2f, 1.0f);

            ImGui.Text("Status: ");
            ImGui.SameLine();

            if (_plugin.IsTracking)
            {
                ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), "Tracking");
            }
            else
            {
                ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), "Not Tracking");
            }

            var buttonWidth = ImGui.CalcTextSize("Config").X + ImGui.GetStyle().FramePadding.X * 2;
            ImGui.SameLine(ImGui.GetWindowWidth() - buttonWidth - 10f);
            if (ImGui.Button("Config", new Vector2(buttonWidth, 24)))
            {
                _plugin.ToggleConfigUi();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (_plugin.IsTracking)
            {
                ImGui.Text($"Target: {_plugin.TrackedTargetName}");

                ImGui.SameLine(ImGui.GetWindowWidth() - ImGui.CalcTextSize("Phase 2").X - 35f);
                
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));
                bool isP2 = _plugin.IsPhase2;
                if (ImGui.Checkbox("Phase 2", ref isP2))
                {
                    _plugin.TogglePhase(isP2);
                }
                ImGui.PopStyleVar();

                if (_plugin.LastResult != null)
                {
                    ImGui.Spacing();
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "--- Previous Threshold ---");
                    ImGui.Text($"Goal: {_plugin.LastResult.Threshold.TimeMinutes}:{_plugin.LastResult.Threshold.TimeSeconds:D2} at {_plugin.LastResult.Threshold.TargetHpPercent}%");

                    if (_plugin.LastResult.Difference > _plugin.CurrentTargetConfig?.TolerancePercent)
                    {
                        ImGui.TextColored(new Vector4(1.0f, 0.35f, 0.1f, 1.0f), $"Actual: {_plugin.LastResult.ActualHpAtThreshold:F2}% (Behind by {_plugin.LastResult.Difference:F2}%)");
                    }
                    else if (_plugin.LastResult.Difference < -_plugin.CurrentTargetConfig?.TolerancePercent)
                    {
                        var aheadAmount = Math.Abs(_plugin.LastResult.Difference);
                        ImGui.TextColored(new Vector4(0.9f, 0.8f, 1.0f, 1.0f), $"Actual: {_plugin.LastResult.ActualHpAtThreshold:F2}% (Fast by {aheadAmount:F2}%)");
                    }
                    else
                    {
                        ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), $"Actual: {_plugin.LastResult.ActualHpAtThreshold:F2}% (On Track within {_plugin.CurrentTargetConfig?.TolerancePercent}%)");
                    }
                    ImGui.Spacing();
                }

                ImGui.Spacing();

                var hpText = $"Health: {_plugin.CurrentHpPercent:F2}%";
                float hpFraction = (float)(_plugin.CurrentHpPercent / 100.0);

                Vector4 barColor;
                if (_plugin.NextThreshold == null)
                {
                    barColor = new Vector4(0.1f, 0.5f, 0.8f, 1.0f);
                }
                else
                {
                    if (_plugin.CurrentPace == PacingState.Behind)
                    {
                        barColor = behind; 
                    }
                    else if (_plugin.CurrentPace == PacingState.TooFast)
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

                bool canPrint = _plugin.LastResult != null;
                if (!canPrint) ImGui.BeginDisabled();
                
                if (ImGui.Button("Print Status to Chat", new Vector2(0, 24)))
                {
                    _plugin.PrintStatusToChat(false);
                }
                
                if (!canPrint) ImGui.EndDisabled();
                
                if (!canPrint && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.SetTooltip("Cannot print to chat until the first threshold has been evaluated.");
                }

                ImGui.Spacing();

                if (_plugin.NextThreshold != null)
                {
                    ImGui.Spacing();
                    ImGui.TextColored(new Vector4(1.0f, 1.0f, 1.0f, 1.0f), "--- Next Threshold ---");
                    ImGui.Text($"Goal: {_plugin.NextThreshold.TimeMinutes}:{_plugin.NextThreshold.TimeSeconds:D2} at {_plugin.NextThreshold.TargetHpPercent}%");

                    if (_plugin.CurrentPace == PacingState.Behind)
                    {
                        ImGui.TextColored(behind, $"[BEHIND]\nProjected Health: {_plugin.ProjectedHpPercent:F2}%");
                    }
                    else if (_plugin.CurrentPace == PacingState.TooFast)
                    {
                        ImGui.TextColored(tooFast, $"[TOO FAST]\nProjected Health: {_plugin.ProjectedHpPercent:F2}%");
                    }
                    else
                    {
                        ImGui.TextColored(onTrack, $"[ON TRACK]\nProjected Health: {_plugin.ProjectedHpPercent:F2}%");
                    }
                }

                ImGui.Spacing();
            }
            else
            {
                ImGui.Text("Target: None");
            }

            ImGui.Spacing();
            ImGui.Text($"Combat Time: {_plugin.CombatDuration:mm\\:ss}");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            var buttonText = _plugin.IsTracking ? "Stop Tracking" : "Start Tracking Target";
            if (ImGui.Button(buttonText, new Vector2(-1, 30)))
            {
                _plugin.ToggleTracking();
            }

            if (!string.IsNullOrEmpty(_plugin.ErrorMessage))
            {
                ImGui.Spacing();
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                ImGui.TextWrapped(_plugin.ErrorMessage);
                ImGui.PopStyleColor();
            }
        }
    }
}
