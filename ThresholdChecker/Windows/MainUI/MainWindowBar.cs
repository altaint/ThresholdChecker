using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace ThresholdChecker.Windows.MainUI
{
    public class MainWindowBar
    {
        private readonly Plugin plugin;
        private static readonly Vector4 DefaultBlueBarColor = new(0.1f, 0.5f, 0.8f, 1.0f);

        public MainWindowBar(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void Draw()
        {
            MainWindowHelper.DrawConfigurationHeader(plugin);
            ImGui.Spacing();
            MainWindowHelper.DrawCombatTimer(plugin);
            ImGui.Spacing();

            if (plugin.Tracker.CurrentKillTimeConfig == null || plugin.Tracker.CurrentHpPercent < 0)
            {
                return;
            }

            var thresholdList = plugin.Tracker.IsPhase2
                ? plugin.Tracker.CurrentKillTimeConfig.Phase2Thresholds
                : plugin.Tracker.CurrentKillTimeConfig.Phase1Thresholds;

            var barColor = GetBarColorFromLastThreshold();

            DrawProgressBarWithThresholds(
                currentValue: (float)plugin.Tracker.CurrentHpPercent,
                maxValue: 100f,
                size: new Vector2(ImGui.GetContentRegionAvail().X, 22f),
                barColor: barColor,
                backgroundColor: new Vector4(0.2f, 0.2f, 0.2f, 1f),
                notchColor: new Vector4(1f, 1f, 1f, 1f),
                thresholds: thresholdList,
                evaluatedResults: plugin.Tracker.EvaluatedThresholdResults,
                tolerance: plugin.Tracker.CurrentTargetConfig?.TolerancePercent ?? 0.0,
                onTrackColor: plugin.Configuration.OnTrackColor.ToVector4(),
                tooFastColor: plugin.Configuration.TooFastColor.ToVector4(),
                behindColor: plugin.Configuration.BehindColor.ToVector4(),
                overlayText: $"Health: {plugin.Tracker.CurrentHpPercent:F2}%"
            );

            ImGui.Spacing();
        }

        private Vector4 GetBarColorFromLastThreshold()
        {
            var lastResult = plugin.Tracker.LastResult;
            if (lastResult == null)
            {
                return DefaultBlueBarColor;
            }

            var tolerance = plugin.Tracker.CurrentTargetConfig?.TolerancePercent ?? 0.0;

            if (lastResult.Difference > tolerance)
            {
                return plugin.Configuration.BehindColor.ToVector4();
            }

            if (lastResult.Difference < -tolerance)
            {
                return plugin.Configuration.TooFastColor.ToVector4();
            }

            return plugin.Configuration.OnTrackColor.ToVector4();
        }

        public static void DrawProgressBarWithThresholds(
            float currentValue,
            float maxValue,
            Vector2 size,
            Vector4 barColor,
            Vector4 backgroundColor,
            Vector4 notchColor,
            List<ThresholdPhase> thresholds,
            IReadOnlyList<ThresholdResult> evaluatedResults,
            double tolerance,
            Vector4 onTrackColor,
            Vector4 tooFastColor,
            Vector4 behindColor,
            string overlayText)
        {
            var drawList = ImGui.GetWindowDrawList();
            var cursorScreenPos = ImGui.GetCursorScreenPos();
            var barEndPos = new Vector2(cursorScreenPos.X + size.X, cursorScreenPos.Y + size.Y);

            const float labelGap = 3f;
            const float symbolGap = 2f;
            float labelHeight = ImGui.GetTextLineHeight();
            float symbolHeight = ImGui.GetTextLineHeight();

            drawList.AddRectFilled(cursorScreenPos, barEndPos, ImGui.GetColorU32(backgroundColor));

            float fillPercentage = maxValue > 0 ? Math.Clamp(currentValue / maxValue, 0f, 1f) : 0f;
            var fillEndPos = new Vector2(cursorScreenPos.X + (size.X * fillPercentage), barEndPos.Y);
            drawList.AddRectFilled(cursorScreenPos, fillEndPos, ImGui.GetColorU32(barColor));

            foreach (var threshold in thresholds.Where(t => t.TargetHpPercent is >= 0 and <= 100).OrderBy(t => t.TargetHpPercent))
            {
                float thresholdPercent = (float)threshold.TargetHpPercent;
                float notchX = cursorScreenPos.X + (size.X * (thresholdPercent / 100f));

                float nubHeight = MathF.Max(3f, size.Y * 0.2f);
                float nubHalfWidth = 1f;
                var nubTopLeft = new Vector2(notchX - nubHalfWidth, barEndPos.Y - nubHeight);
                var nubBottomRight = new Vector2(notchX + nubHalfWidth, barEndPos.Y);
                drawList.AddRectFilled(nubTopLeft, nubBottomRight, ImGui.GetColorU32(notchColor));

                string label = $"{thresholdPercent:F0}%";
                var labelSize = ImGui.CalcTextSize(label);
                float labelX = Math.Clamp(notchX - (labelSize.X * 0.5f), cursorScreenPos.X, barEndPos.X - labelSize.X);
                float labelY = barEndPos.Y + labelGap;
                drawList.AddText(new Vector2(labelX, labelY), ImGui.GetColorU32(notchColor), label);

                var result = evaluatedResults.FirstOrDefault(r =>
                    r.Threshold.TotalSeconds == threshold.TotalSeconds &&
                    Math.Abs(r.Threshold.TargetHpPercent - threshold.TargetHpPercent) < 0.001);

                if (result != null)
                {
                    bool onTrack = Math.Abs(result.Difference) <= tolerance;
                    bool behind = result.Difference > tolerance;

                    string symbol = onTrack
                        ? FontAwesomeIcon.Check.ToIconString()
                        : FontAwesomeIcon.Times.ToIconString();

                    Vector4 symbolColor = onTrack ? onTrackColor : (behind ? behindColor : tooFastColor);

                    ImGui.PushFont(UiBuilder.IconFont);
                    var symbolSize = ImGui.CalcTextSize(symbol);
                    ImGui.PopFont();

                    float symbolX = Math.Clamp(notchX - (symbolSize.X * 0.5f), cursorScreenPos.X, barEndPos.X - symbolSize.X);
                    float symbolY = labelY + labelHeight + symbolGap;

                    var savedCursor = ImGui.GetCursorScreenPos();
                    ImGui.SetCursorScreenPos(new Vector2(symbolX, symbolY));
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(symbolColor, symbol);
                    ImGui.PopFont();
                    ImGui.SetCursorScreenPos(savedCursor);
                }
            }

            var textSize = ImGui.CalcTextSize(overlayText);
            var textPos = new Vector2(cursorScreenPos.X + ((size.X - textSize.X) * 0.5f), 
                                      cursorScreenPos.Y + ((size.Y - textSize.Y) * 0.5f));
            drawList.AddText(textPos, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)), overlayText);

            drawList.AddRect(cursorScreenPos, barEndPos, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.5f)), 0f, ImDrawFlags.None, 1f);

            ImGui.Dummy(new Vector2(size.X, size.Y + labelGap + labelHeight + symbolGap + symbolHeight));
        }
    }
}
