using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;

namespace ThresholdChecker.Windows.Config;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly TabEncounters encountersTab;
    private readonly TabChat chatTab;

    private DateTime? lastSaveTime = null;
    private readonly TimeSpan saveMessageDuration = TimeSpan.FromSeconds(2);

    public ConfigWindow(Plugin plugin) : base("Threshold Configuration")
    {
        Flags = ImGuiWindowFlags.NoCollapse;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 450),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        configuration = plugin.Configuration;

        this.encountersTab = new TabEncounters(configuration);
        this.chatTab = new TabChat(configuration);
    }

    public void Dispose()
    {
        this.encountersTab.Dispose();
        this.chatTab.Dispose();
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("ConfigTabBar"))
        {
            if (ImGui.BeginTabItem("Encounters"))
            {
                encountersTab.Draw();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Chat Settings"))
            {
                chatTab.Draw();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Save Settings"))
        {
            configuration.Save();
            lastSaveTime = DateTime.Now;
        }

        if (lastSaveTime.HasValue && DateTime.Now - lastSaveTime.Value < saveMessageDuration)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), "Settings Saved!");
        }
    }

    public override void OnOpen()
    {
        encountersTab.OnOpen();
    }
}
