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
        private readonly MainWindowCompact compactView;
        private readonly MainWindowInactive inactiveView;
        private readonly MainWindowBar barView;
        private bool lastTrackingState = false;

        private MainLayout selectedLayout;
        private MainLayout lastLayoutState;

        private const float DetailedMinHeight = 300f;
        private const float CompactMinHeight = 150f;
        private const float BarMinHeight = 185f;
        private const float BarMinWidth = 400f;
        private const float MinWidth = 250f;

        private static readonly string[] LayoutOptions = { "Detailed", "Compact", "Bar" };

        public MainWindow(Plugin plugin)
            : base("Threshold Checker###Threshold Checker", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.plugin = plugin;
            this.detailedView = new MainWindowDetailed(plugin);
            this.compactView = new MainWindowCompact(plugin);
            this.barView = new MainWindowBar(plugin);
            this.inactiveView = new MainWindowInactive();

            selectedLayout = Enum.IsDefined(plugin.Configuration.SelectedMainLayout)
                ? plugin.Configuration.SelectedMainLayout
                : MainLayout.Detailed;

            lastLayoutState = selectedLayout;
            UpdateWindowConstraints();
        }

        public void Dispose() { }

        private void UpdateWindowConstraints()
        {
            if (!plugin.Tracker.IsTracking)
            {
                SizeConstraints = new WindowSizeConstraints
                {
                    MinimumSize = new Vector2(MinWidth, DetailedMinHeight),
                    MaximumSize = new Vector2(float.MaxValue, DetailedMinHeight)
                };
                return;
            }

            switch (selectedLayout)
            {
                case MainLayout.Compact:
                    SizeConstraints = new WindowSizeConstraints
                    {
                        MinimumSize = new Vector2(MinWidth, CompactMinHeight),
                        MaximumSize = new Vector2(MinWidth, CompactMinHeight)
                    };
                    break;

                case MainLayout.Bar:
                    SizeConstraints = new WindowSizeConstraints
                    {
                        MinimumSize = new Vector2(BarMinWidth, BarMinHeight),
                        MaximumSize = new Vector2(float.MaxValue, BarMinHeight)
                    };
                    break;

                case MainLayout.Detailed:
                default:
                    SizeConstraints = new WindowSizeConstraints
                    {
                        MinimumSize = new Vector2(MinWidth, DetailedMinHeight),
                        MaximumSize = new Vector2(float.MaxValue, DetailedMinHeight)
                    };
                    break;
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

            if (selectedLayout != lastLayoutState && plugin.Tracker.IsTracking)
            {
                lastLayoutState = selectedLayout;

                float targetHeight = selectedLayout switch
                {
                    MainLayout.Compact => CompactMinHeight,
                    MainLayout.Bar => BarMinHeight,
                    _ => DetailedMinHeight
                };

                Size = new Vector2(Size?.X ?? MinWidth, targetHeight);
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

            int buttonCount = plugin.Tracker.IsTracking ? 4 : 2;
            float buttonsWidth = (buttonCount * buttonSize.X) + ((buttonCount - 1) * itemSpacing) + windowPadding;

            ImGui.SameLine(ImGui.GetWindowWidth() - buttonsWidth);

            if (plugin.Tracker.IsTracking)
            {
                int currentLayoutIndex = (int)selectedLayout;
                if (MainWindowHelper.DrawViewDropdown(buttonSize, LayoutOptions, ref currentLayoutIndex))
                {
                    selectedLayout = (MainLayout)currentLayoutIndex;
                    plugin.Configuration.SelectedMainLayout = selectedLayout;
                    plugin.Configuration.Save();
                    UpdateWindowConstraints();
                }

                ImGui.SameLine();
                MainWindowHelper.DrawPrintToChatButton(plugin, buttonSize);
                ImGui.SameLine();
            }

            MainWindowHelper.DrawTrackingToggleButton(plugin, buttonSize);
            ImGui.SameLine();
            MainWindowHelper.DrawConfigButton(plugin, buttonSize);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (plugin.Tracker.IsTracking)
            {
                switch (selectedLayout)
                {
                    case MainLayout.Compact:
                        compactView.Draw();
                        break;
                    case MainLayout.Bar:
                        barView.Draw();
                        break;
                    case MainLayout.Detailed:
                    default:
                        detailedView.Draw();
                        break;
                }
            }
            else
            {
                inactiveView.Draw();
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
