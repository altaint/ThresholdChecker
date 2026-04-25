using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using System;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;

namespace ThresholdChecker.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private int selectedDutyIndex = 0;
    private int selectedTargetIndex = 0;
    private int editingPhase = 0; // 0 = P1, 1 = P2
    
    private uint newSelectedDutyId = 0;
    private string dutySearchFilter = "";
    
    private uint newSelectedTargetId = 0;
    private string targetSearchFilter = "";

    private string addDutyError = "";
    private DateTime? addDutyErrorTime = null;

    private string addTargetError = "";
    private DateTime? addTargetErrorTime = null;

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
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("ConfigTabBar"))
        {
            if (ImGui.BeginTabItem("Encounters"))
            {
                DrawEncountersTab();
                ImGui.EndTabItem();
            }
            
            if (ImGui.BeginTabItem("Chat Settings"))
            {
                DrawChatSettingsTab();
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

    private void DrawChatSettingsTab()
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

    private void DrawEncountersTab()
    {
        ImGui.Spacing();

        if (selectedDutyIndex >= configuration.Duties.Count)
        {
            selectedDutyIndex = Math.Max(0, configuration.Duties.Count - 1);
            selectedTargetIndex = 0; 
        }

        ImGui.Text("Select Duty:");
        
        if (configuration.Duties.Count > 0)
        {
            var dutyNames = configuration.Duties.Select(d => d.DutyName).ToArray();
            ImGui.Combo("##dutyCombo", ref selectedDutyIndex, dutyNames, dutyNames.Length);

            ImGui.SameLine();
            if (ImGui.Button("Delete Duty"))
            {
                configuration.Duties.RemoveAt(selectedDutyIndex);
                configuration.Save();
                if (selectedDutyIndex >= configuration.Duties.Count) 
                    selectedDutyIndex = Math.Max(0, configuration.Duties.Count - 1);
                selectedTargetIndex = 0;
            }
        }
        else
        {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), "No Duties configured.");
        }

        ImGui.Spacing();

        ImGui.SetNextItemWidth(250f);

        var dutySheet = Service.DataManager.GetExcelSheet<ContentFinderCondition>();
        string dutyPreviewName = "Search for a duty...";

        if (newSelectedDutyId != 0 && dutySheet != null)
        {
            var row = dutySheet.GetRow(newSelectedDutyId);
            dutyPreviewName = row.Name.ToString(); 
        }

        if (ImGui.BeginCombo("##addDutyCombo", dutyPreviewName, ImGuiComboFlags.HeightLarge))
        {
            ImGui.InputTextWithHint("##searchDuty", "Filter by duty name...", ref dutySearchFilter, 100);
            
            if (dutySheet != null)
            {
                foreach (var row in dutySheet)
                {
                    var name = row.Name.ToString();
                    if (string.IsNullOrEmpty(name)) continue;
                    
                    if (!string.IsNullOrEmpty(dutySearchFilter) && !name.Contains(dutySearchFilter, StringComparison.OrdinalIgnoreCase)) 
                        continue;

                    var terId = row.TerritoryType.RowId;
                    if (terId == 0) continue;

                    if (ImGui.Selectable($"{name} ({terId})", newSelectedDutyId == row.RowId))
                    {
                        newSelectedDutyId = row.RowId;
                    }
                }
            }
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        if (ImGui.Button("Select Current Duty"))
        {
            if (dutySheet != null && Service.ClientState != null)
            {
                var currentTerritoryId = Service.ClientState.TerritoryType;
                
                var activeDuty = dutySheet.FirstOrDefault(r => r.TerritoryType.RowId == currentTerritoryId);
                
                if (activeDuty.RowId != 0)
                {
                    newSelectedDutyId = activeDuty.RowId;
                    dutySearchFilter = "";
                }
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Add Duty") && newSelectedDutyId != 0 && dutySheet != null)
        {
            var row = dutySheet.GetRow(newSelectedDutyId);
            var territoryId = row.TerritoryType.RowId;

            if (configuration.Duties.Any(d => d.TerritoryId == territoryId))
            {
                addDutyError = "Duty already added!";
                addDutyErrorTime = DateTime.Now;
            }
            else
            {
                var newDuty = new DutyConfig
                {
                    DutyName = row.Name.ToString(),
                    TerritoryId = territoryId
                };

                configuration.Duties.Add(newDuty);

                selectedDutyIndex = configuration.Duties.Count - 1;
                selectedTargetIndex = 0;
                newSelectedDutyId = 0;
                dutySearchFilter = "";
                configuration.Save();
            }
        }

        if (addDutyErrorTime.HasValue && DateTime.Now - addDutyErrorTime.Value < saveMessageDuration)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), addDutyError);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (configuration.Duties.Count == 0)
        {
            return;
        }

        var currentDuty = configuration.Duties[selectedDutyIndex];

        if (selectedTargetIndex >= currentDuty.Targets.Count)
        {
            selectedTargetIndex = Math.Max(0, currentDuty.Targets.Count - 1);
        }

        ImGui.Text($"Select Target for {currentDuty.DutyName}:");
        
        if (currentDuty.Targets.Count > 0)
        {
            var targetNames = currentDuty.Targets.Select(t => t.TargetName).ToArray();

            ImGui.Combo("##targetCombo", ref selectedTargetIndex, targetNames, targetNames.Length);
            
            ImGui.SameLine();
            if (ImGui.Button("Delete Target"))
            {
                currentDuty.Targets.RemoveAt(selectedTargetIndex);
                configuration.Save();
                if (selectedTargetIndex >= currentDuty.Targets.Count) 
                    selectedTargetIndex = Math.Max(0, currentDuty.Targets.Count - 1);
            }
        }
        else
        {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), "No Targets configured.");
        }

        ImGui.Spacing();
        
        ImGui.SetNextItemWidth(250f);

        var npcSheet = Service.DataManager.GetExcelSheet<BNpcName>();
        string targetPreviewName = "Search for a Battle NPC...";

        if (newSelectedTargetId != 0 && npcSheet != null)
        {
            var row = npcSheet.GetRow(newSelectedTargetId);
            var previewName = row.Singular.ToString();
            
            if (!string.IsNullOrEmpty(previewName) && char.IsLower(previewName[0]))
            {
                previewName = char.ToUpper(previewName[0]) + previewName.Substring(1);
            }
            
            targetPreviewName = previewName; 
        }

        if (ImGui.BeginCombo("##addTargetCombo", targetPreviewName, ImGuiComboFlags.HeightLarge))
        {
            ImGui.InputTextWithHint("##searchTarget", "Filter by NPC name...", ref targetSearchFilter, 100);
            
            if (npcSheet != null)
            {
                var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var row in npcSheet)
                {
                    var name = row.Singular.ToString();
                    if (string.IsNullOrEmpty(name)) continue;
                    
                    if (char.IsLower(name[0]))
                    {
                        name = char.ToUpper(name[0]) + name.Substring(1);
                    }
                    
                    if (!string.IsNullOrEmpty(targetSearchFilter) && !name.Contains(targetSearchFilter, StringComparison.OrdinalIgnoreCase)) 
                        continue;

                    if (!seenNames.Add(name)) continue;

                    if (ImGui.Selectable(name, newSelectedTargetId == row.RowId))
                    {
                        newSelectedTargetId = row.RowId;
                    }
                }
            }
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        if (ImGui.Button("Select Current Target"))
        {
            if (npcSheet != null && Service.TargetManager != null)
            {
                var activeTarget = Service.TargetManager.Target;
                if (activeTarget != null && activeTarget is Dalamud.Game.ClientState.Objects.Types.IBattleNpc bnpc)
                {
                    var bnpcNameId = bnpc.NameId;
                    if (bnpcNameId != 0)
                    {
                        newSelectedTargetId = bnpcNameId;
                        targetSearchFilter = ""; 
                    }
                }
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Add Target") && newSelectedTargetId != 0 && npcSheet != null)
        {
            var row = npcSheet.GetRow(newSelectedTargetId);
            var addName = row.Singular.ToString();

            if (!string.IsNullOrEmpty(addName) && char.IsLower(addName[0]))
            {
                addName = char.ToUpper(addName[0]) + addName.Substring(1);
            }

            if (currentDuty.Targets.Any(t => t.TargetName.Equals(addName, StringComparison.OrdinalIgnoreCase)))
            {
                addTargetError = "Target already added!";
                addTargetErrorTime = DateTime.Now;
            }
            else
            {
                currentDuty.Targets.Add(new TargetConfig { TargetName = addName });

                selectedTargetIndex = currentDuty.Targets.Count - 1;
                newSelectedTargetId = 0;
                targetSearchFilter = "";
                configuration.Save();
            }
        }

        if (addTargetErrorTime.HasValue && DateTime.Now - addTargetErrorTime.Value < saveMessageDuration)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), addTargetError);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (currentDuty.Targets.Count == 0)
        {
            return;
        }

        var currentConfig = currentDuty.Targets[selectedTargetIndex];

        ImGui.Text($"Configure Health Thresholds for {currentConfig.TargetName}:");
        ImGui.Separator();

        ImGui.RadioButton("Phase 1", ref editingPhase, 0);
        ImGui.SameLine();
        ImGui.RadioButton("Phase 2", ref editingPhase, 1);
        ImGui.Spacing();

        var activeThresholds = editingPhase == 0 ? currentConfig.Thresholds : currentConfig.P2Thresholds;

        for (int i = 0; i < activeThresholds.Count; i++)
        {
            var phase = activeThresholds[i];

            ImGui.PushID($"phase_{i}");

            ImGui.SetNextItemWidth(50);
            int mins = phase.TimeMinutes;
            if (ImGui.InputInt("m", ref mins, 0)) phase.TimeMinutes = Math.Max(0, mins);

            ImGui.SameLine();
            ImGui.SetNextItemWidth(50);
            int secs = phase.TimeSeconds;
            if (ImGui.InputInt("s", ref secs, 0)) phase.TimeSeconds = Math.Clamp(secs, 0, 59);

            ImGui.SameLine();
            ImGui.SetNextItemWidth(75);
            double hp = phase.TargetHpPercent;
            if (ImGui.InputDouble("% HP", ref hp, 0, 0, "%.2f")) phase.TargetHpPercent = Math.Clamp(hp, 0, 100);

            ImGui.SameLine();
            if (ImGui.Button("Remove"))
            {
                activeThresholds.RemoveAt(i);
                configuration.Save();
                ImGui.PopID();
                break;
            }

            ImGui.PopID();
        }

        ImGui.Spacing();
        if (ImGui.Button("Add New Threshold"))
        {
            activeThresholds.Add(new ThresholdPhase { TimeMinutes = 1, TimeSeconds = 0, TargetHpPercent = 80 });
            configuration.Save();
        }

        ImGui.Spacing();
        ImGui.Separator();

        double tol = currentConfig.TolerancePercent;
        ImGui.SetNextItemWidth(100);
        if (ImGui.InputDouble("Tolerance % (+/-)", ref tol, 0, 0, "%.2f"))
        {
            currentConfig.TolerancePercent = Math.Max(0, tol);
        }
    }

    public override void OnOpen()
    {
        var currentTerritoryId = Service.ClientState?.TerritoryType ?? 0;
        if (currentTerritoryId == 0)
        {
            return;
        }

        var dutyIndex = configuration.Duties.FindIndex(d => d.TerritoryId == currentTerritoryId);

        if (dutyIndex >= 0)
        {
            selectedDutyIndex = dutyIndex;
            selectedTargetIndex = 0;
        }
    }
}
