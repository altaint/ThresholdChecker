using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace ThresholdChecker.Windows.MainUI
{
    public class MainWindowInactive
    {
        public void Draw()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.85f, 0.0f, 1.0f));
            ImGui.TextWrapped("⚠ Not currently tracking a boss.");
            ImGui.PopStyleColor();
            
            ImGui.Spacing();
            
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.7f, 0.7f, 1.0f));
            ImGui.TextWrapped("How to use:");
            ImGui.PopStyleColor();
            
            ImGui.Bullet();
            ImGui.TextWrapped("Click the Config button and create a threshold configuration.");
            ImGui.Bullet();
            ImGui.TextWrapped("Target the boss that you want tracked and click the button below.");
            ImGui.Bullet();
            ImGui.TextWrapped("Enter combat and the tracker will monitor pace.");
        }
    }
}
