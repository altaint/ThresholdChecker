using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace ThresholdChecker.Windows.MainUI
{
    public static class MainWindowHelper
    {
        public static void DrawConfigurationHeader(Plugin plugin)
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
        }

        public static void DrawHealthBar(Plugin plugin)
        {
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
        }

        public static void DrawNextThreshold(Plugin plugin)
        {
            if (plugin.Tracker.NextThreshold == null)
                return;

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
        }

        public static void DrawPrintToChatButton(Plugin plugin, Vector2 buttonSize)
        {
            bool canPrint = plugin.Tracker.LastResult != null;
            if (!canPrint) ImGui.BeginDisabled();
            
            ImGui.PushFont(UiBuilder.IconFont);
            var buttonText = FontAwesomeIcon.CommentDots.ToIconString();
            
            if (ImGui.Button(buttonText, buttonSize))
            {
                ImGui.PopFont();
                plugin.ChatManager.PrintStatusToChat(false);
            }
            else
            {
                ImGui.PopFont();
            }
            
            if (!canPrint) ImGui.EndDisabled();
            
            if (!canPrint && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip("Cannot print to chat until the first threshold has been evaluated. \nYou can use \"/trackboss print\" to make a macro.");
            }
        }

        public static void DrawCombatTimer(Plugin plugin)
        {
            ImGui.Text($"Combat Time: {plugin.Tracker.CombatDuration:mm\\:ss}");
        }

        public static void DrawConfigButton(Plugin plugin, Vector2 buttonSize)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 2));
            ImGui.PushFont(UiBuilder.IconFont);
            var configButtonText = $"{FontAwesomeIcon.Cog.ToIconString()}";
            ImGui.Button(configButtonText, buttonSize);
            ImGui.PopFont();
            ImGui.PopStyleVar();
            
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Open config window");
            }
            
            if (ImGui.IsItemClicked())
            {
                plugin.ToggleConfigUi();
            }
        }

        public static bool DrawViewToggleButton(Vector2 buttonSize)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 2));
            ImGui.PushFont(UiBuilder.IconFont);
            var viewButtonText = $"{FontAwesomeIcon.ChevronDown.ToIconString()}";
            bool clicked = ImGui.Button(viewButtonText, buttonSize);
            ImGui.PopFont();
            ImGui.PopStyleVar();
            return clicked;
        }

        public static bool DrawViewDropdown(Vector2 buttonSize, ref bool useSimplifiedView)
        {
            string[] viewOptions = { "Detailed", "Simplified" };
            int currentView = useSimplifiedView ? 1 : 0;
            
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 2));
            ImGui.PushFont(UiBuilder.IconFont);
            var dropdownText = $"{FontAwesomeIcon.ChevronDown.ToIconString()}";
            ImGui.Button(dropdownText, buttonSize);
            ImGui.PopFont();
            ImGui.PopStyleVar();
            
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Click to select a layout");
            }
            
            bool viewChanged = false;
            
            if (ImGui.IsItemClicked())
            {
                ImGui.OpenPopup("ViewDropdown");
            }
            
            if (ImGui.BeginPopup("ViewDropdown"))
            {
                for (int i = 0; i < viewOptions.Length; i++)
                {
                    if (ImGui.Selectable(viewOptions[i], currentView == i))
                    {
                        useSimplifiedView = i == 1;
                        viewChanged = true;
                    }
                }
                ImGui.EndPopup();
            }
            
            return viewChanged;
        }
    }
}
