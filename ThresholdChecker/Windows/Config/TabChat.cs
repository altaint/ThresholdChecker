using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace ThresholdChecker.Windows.Config;

public class TabChat : IDisposable
{
    private readonly Configuration configuration;

    public TabChat(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public void Dispose() { }

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.Text("Chat Message Configurations:");

        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1.0f), "Tip: Use {diff} in your text to show the difference from the threshold from the last threshold.");
        ImGui.Spacing();

        var selectedChannel = (int)configuration.OutputChannel;
        var channels = Enum.GetNames(typeof(ChatChannel));

        ImGui.SetNextItemWidth(150);
        if (ImGui.Combo("Output Channel##Global", ref selectedChannel, channels, channels.Length))
        {
            configuration.OutputChannel = (ChatChannel)selectedChannel;
            configuration.Save();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var tooFast = configuration.TooFastMessage;
        if (ImGui.InputText("Too Fast Message##Global", ref tooFast, 100)) configuration.TooFastMessage = tooFast;

        var onTrack = configuration.OnTrackMessage;
        if (ImGui.InputText("On Track Message##Global", ref onTrack, 100)) configuration.OnTrackMessage = onTrack;

        var behind = configuration.BehindMessage;
        if (ImGui.InputText("Behind Message##Global", ref behind, 100)) configuration.BehindMessage = behind;
    }
}
