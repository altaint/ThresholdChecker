using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using ThresholdChecker.Windows.MainUI;

namespace ThresholdChecker.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private readonly Plugin plugin;
        private readonly MainWindowDetailed detailedView;
        private readonly MainWindowSimplified simplifiedView;
        private readonly MainWindowInactive inactiveView;
        private bool useSimplifiedView = false;
        private bool lastViewState = false;
        private bool lastTrackingState = false;

        private const float DetailedMinHeight = 375f;
        private const float SimplifiedMinHeight = 195f;
        private const float MinWidth = 250f;

        public MainWindow(Plugin plugin)
            : base("Threshold Checker###Threshold Checker", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.plugin = plugin;
            this.detailedView = new MainWindowDetailed(plugin);
            this.simplifiedView = new MainWindowSimplified(plugin);
            this.inactiveView = new MainWindowInactive();
            this.useSimplifiedView = plugin.Configuration.UseSimplifiedView;

            UpdateWindowConstraints();
        }

        public void Dispose() { }

        private void UpdateWindowConstraints()
        {
            if (plugin.Tracker.IsTracking && useSimplifiedView)
            {
                SizeConstraints = new WindowSizeConstraints
                {
                    MinimumSize = new Vector2(MinWidth, SimplifiedMinHeight),
                    MaximumSize = new Vector2(MinWidth, SimplifiedMinHeight)
                };
            }
            else if (plugin.Tracker.IsTracking)
            {
                SizeConstraints = new WindowSizeConstraints
                {
                    MinimumSize = new Vector2(MinWidth, DetailedMinHeight),
                    MaximumSize = new Vector2(float.MaxValue, DetailedMinHeight)
                };
            }
            else
            {
                SizeConstraints = new WindowSizeConstraints
                {
                    MinimumSize = new Vector2(MinWidth, DetailedMinHeight),
                    MaximumSize = new Vector2(float.MaxValue, DetailedMinHeight)
                };
            }
        }

        public override void PreDraw()
        {
            WindowName = plugin.Tracker.IsTracking 
                ? $"Threshold Checker - {plugin.Tracker.TrackedTargetName}###Threshold Checker" 
                : "Threshold Checker###Threshold Checker";

            if (plugin.Tracker.IsTracking != lastTrackingState)
            {
                lastTrackingState = plugin.Tracker.IsTracking;
                
                if (!plugin.Tracker.IsTracking)
                {
                    Size = new Vector2(Size?.X ?? MinWidth, DetailedMinHeight);
                }
                
                UpdateWindowConstraints();
                return;
            }

            if (useSimplifiedView != lastViewState && plugin.Tracker.IsTracking)
            {
                lastViewState = useSimplifiedView;
                
                if (useSimplifiedView)
                {
                    Size = new Vector2(Size?.X ?? MinWidth, SimplifiedMinHeight);
                }
                else
                {
                    Size = new Vector2(Size?.X ?? MinWidth, DetailedMinHeight);
                }
                
                UpdateWindowConstraints();
            }
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

            var buttonSize = new Vector2(24, 24);
            var itemSpacing = ImGui.GetStyle().ItemSpacing.X;
            var windowPadding = ImGui.GetStyle().WindowPadding.X;

            float buttonsWidth = buttonSize.X + windowPadding;
            
            if (plugin.Tracker.IsTracking)
            {
                buttonsWidth += buttonSize.X * 2 + itemSpacing * 2;
            }

            ImGui.SameLine(ImGui.GetWindowWidth() - buttonsWidth);
            
            if (plugin.Tracker.IsTracking)
            {
                if (MainWindowHelper.DrawViewDropdown(buttonSize, ref useSimplifiedView))
                {
                    plugin.Configuration.UseSimplifiedView = useSimplifiedView;
                    plugin.Configuration.Save();
                    UpdateWindowConstraints();
                }
                ImGui.SameLine();
                
                MainWindowHelper.DrawPrintToChatButton(plugin, buttonSize);
                ImGui.SameLine();
            }
            
            MainWindowHelper.DrawConfigButton(plugin, buttonSize);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (plugin.Tracker.IsTracking)
            {
                if (useSimplifiedView)
                {
                    simplifiedView.Draw();
                }
                else
                {
                    detailedView.Draw();
                }
            }
            else
            {
                inactiveView.Draw();
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
