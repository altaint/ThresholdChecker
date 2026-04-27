using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace ThresholdChecker.Windows.Config;

public class TabColour : IDisposable
{
    private readonly Configuration configuration;
    private readonly ImGuiColorEditFlags pickerFlags = ImGuiColorEditFlags.PickerHueBar | ImGuiColorEditFlags.NoSidePreview;

    public TabColour(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public void Dispose() { }

    public override string ToString() => "TabColour";

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.Text("Configure status colours:");
        ImGui.Spacing();

        void DrawPreview(Vector4 colour, string exampleText, float exampleFraction = 0.5f)
        {
            ImGui.Text("Example:");
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, colour);
            ImGui.ProgressBar(exampleFraction, new Vector2(150, 18), string.Empty);
            ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.TextColored(colour, exampleText);
            ImGui.Spacing();
        }

        var tooFast = configuration.TooFastColor.ToVector4();
        ImGui.SetNextItemWidth(300f);
        if (ImGui.ColorEdit4("Too Fast##Colour", ref tooFast, pickerFlags))
        {
            configuration.TooFastColor.FromVector4(tooFast);
            configuration.Save();
        }

        DrawPreview(tooFast, "Actual: 48.00% (Fast by 4.00%)", 0.48f);

        ImGui.Separator();
        ImGui.Spacing();

        var onTrack = configuration.OnTrackColor.ToVector4();
        ImGui.SetNextItemWidth(300f);
        if (ImGui.ColorEdit4("On Track##Colour", ref onTrack, pickerFlags))
        {
            configuration.OnTrackColor.FromVector4(onTrack);
            configuration.Save();
        }

        DrawPreview(onTrack, "Actual: 52.00% (On Track within 3%)", 0.52f);

        ImGui.Separator();
        ImGui.Spacing();

        var behind = configuration.BehindColor.ToVector4();
        ImGui.SetNextItemWidth(300f);
        if (ImGui.ColorEdit4("Behind##Colour", ref behind, pickerFlags))
        {
            configuration.BehindColor.FromVector4(behind);
            configuration.Save();
        }

        DrawPreview(behind, "Actual: 60.00% (Behind by 5.00%)", 0.60f);

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1.0f), "Tip: Click the colour rectangle to open the colour picker. Previews show a sample bar and text.");
    }
}
