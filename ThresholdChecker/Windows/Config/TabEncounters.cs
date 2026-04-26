using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using System;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;

namespace ThresholdChecker.Windows.Config;

public class TabEncounters : IDisposable
{
    private readonly Configuration configuration;
    private int selectedDutyIndex = -1;
    private int selectedTargetIndex = -1;
    private int editingPhase = 0; // 0 = P1, 1 = P2

    private uint newSelectedDutyId = 0;
    private string dutySearchFilter = "";

    private uint newSelectedTargetId = 0;
    private string targetSearchFilter = "";

    private string addDutyError = "";
    private DateTime? addDutyErrorTime = null;

    private string addTargetError = "";
    private DateTime? addTargetErrorTime = null;

    private readonly TimeSpan saveMessageDuration = TimeSpan.FromSeconds(2);

    public TabEncounters(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public void Dispose() { }

    public void Draw()
    {
        ImGui.Spacing();

        ImGui.Text("Select Duty:");

        if (configuration.Duties.Count > 0)
        {
            var dutyNames = new string[configuration.Duties.Count + 1];
            dutyNames[0] = "-- Select a Duty --";
            for (int i = 0; i < configuration.Duties.Count; i++)
                dutyNames[i + 1] = configuration.Duties[i].DutyName;

            int displaySelectedDuty = selectedDutyIndex >= 0 ? selectedDutyIndex + 1 : 0;
            ImGui.Combo("##dutyCombo", ref displaySelectedDuty, dutyNames, dutyNames.Length);

            if (displaySelectedDuty == 0)
                selectedDutyIndex = -1;
            else
                selectedDutyIndex = displaySelectedDuty - 1;

            if (selectedDutyIndex >= 0)
            {
                ImGui.SameLine();
                if (ImGui.Button("Delete Duty"))
                {
                    configuration.Duties.RemoveAt(selectedDutyIndex);
                    configuration.Save();
                    if (configuration.Duties.Count == 0)
                    {
                        selectedDutyIndex = -1;
                        selectedTargetIndex = -1;
                    }
                    else
                    {
                        selectedDutyIndex = Math.Clamp(selectedDutyIndex, 0, configuration.Duties.Count - 1);
                        selectedTargetIndex = -1;
                    }
                }
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
                selectedTargetIndex = -1;
                newSelectedDutyId = 0;
                dutySearchFilter = "";
                configuration.Save();
            }
        }

        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.75f, 0.75f, 0.75f, 1.0f), " / ");

        ContentFinderCondition activeDuty = default;
        var inDuty = false;
        if (dutySheet != null && Service.ClientState != null)
        {
            var currentTerritoryId = Service.ClientState.TerritoryType;
            activeDuty = dutySheet.FirstOrDefault(r => r.TerritoryType.RowId == currentTerritoryId);
            inDuty = activeDuty.RowId != 0;
        }

        ImGui.SameLine();
        if (!inDuty)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
        }

        var addCurrentPressed = ImGui.Button("Add Current Duty");

        if (!inDuty)
        {
            ImGui.PopStyleVar();
        }

        if (!inDuty && ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("You must be in a duty.");
        }

        if (addCurrentPressed && inDuty)
        {
            var territoryId = activeDuty.TerritoryType.RowId;

            if (configuration.Duties.Any(d => d.TerritoryId == territoryId))
            {
                addDutyError = "Duty already added!";
                addDutyErrorTime = DateTime.Now;
            }
            else
            {
                var newDuty = new DutyConfig
                {
                    DutyName = activeDuty.Name.ToString(),
                    TerritoryId = territoryId
                };

                configuration.Duties.Add(newDuty);

                selectedDutyIndex = configuration.Duties.Count - 1;
                selectedTargetIndex = -1;
                newSelectedDutyId = 0;
                dutySearchFilter = "";
                configuration.Save();
            }
        }

        ImGui.SameLine();
        if (addDutyErrorTime.HasValue && DateTime.Now - addDutyErrorTime.Value < saveMessageDuration)
        {
            ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), addDutyError);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (selectedDutyIndex < 0)
        {
            ImGui.TextColored(new Vector4(1.0f, 0.85f, 0.0f, 1.0f), "⚠ Select a duty to configure targets.");
            return;
        }

        var currentDuty = configuration.Duties[selectedDutyIndex];

        if (selectedTargetIndex >= currentDuty.Targets.Count)
        {
            selectedTargetIndex = Math.Max(-1, currentDuty.Targets.Count - 1);
        }

        ImGui.Text($"Select Target for {currentDuty.DutyName}:");

        if (currentDuty.Targets.Count > 0)
        {
            var targetNames = new string[currentDuty.Targets.Count + 1];
            targetNames[0] = "-- Select a Target --";
            for (int i = 0; i < currentDuty.Targets.Count; i++)
                targetNames[i + 1] = currentDuty.Targets[i].TargetName;

            int displaySelectedTarget = selectedTargetIndex >= 0 ? selectedTargetIndex + 1 : 0;
            ImGui.Combo("##targetCombo", ref displaySelectedTarget, targetNames, targetNames.Length);

            if (displaySelectedTarget == 0)
                selectedTargetIndex = -1;
            else
                selectedTargetIndex = displaySelectedTarget - 1;

            if (selectedTargetIndex >= 0)
            {
                ImGui.SameLine();
                if (ImGui.Button("Delete Target"))
                {
                    currentDuty.Targets.RemoveAt(selectedTargetIndex);
                    configuration.Save();
                    if (currentDuty.Targets.Count == 0)
                    {
                        selectedTargetIndex = -1;
                    }
                    else
                    {
                        selectedTargetIndex = Math.Clamp(selectedTargetIndex, 0, currentDuty.Targets.Count - 1);
                    }
                }
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

        ImGui.SameLine();

        ImGui.TextColored(new Vector4(0.75f, 0.75f, 0.75f, 1.0f), " / ");

        var canAddCurrentTarget = false;
        Dalamud.Game.ClientState.Objects.Types.IBattleNpc activeBnpc = null;
        if (npcSheet != null && Service.TargetManager != null && Service.ClientState != null && selectedDutyIndex >= 0)
        {
            var clientTerritory = Service.ClientState.TerritoryType;
            var inSelectedDuty = clientTerritory == currentDuty.TerritoryId;
            var t = Service.TargetManager.Target;
            if (inSelectedDuty && t is Dalamud.Game.ClientState.Objects.Types.IBattleNpc bnpc && bnpc.NameId != 0)
            {
                canAddCurrentTarget = true;
                activeBnpc = bnpc;
            }
        }

        ImGui.SameLine();
        if (!canAddCurrentTarget) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);

        var addCurrentTargetPressed = ImGui.Button("Add Current Target");

        if (!canAddCurrentTarget) ImGui.PopStyleVar();

        if (!canAddCurrentTarget && ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("You must be in the same duty as the selected duty and have a current target.");
        }

        if (addCurrentTargetPressed && canAddCurrentTarget && activeBnpc != null)
        {
            var bnpcNameId = activeBnpc.NameId;
            var row = npcSheet.GetRow(bnpcNameId);
            var addName = row.Singular.ToString();

            if (!string.IsNullOrEmpty(addName) && char.IsLower(addName[0]))
                addName = char.ToUpper(addName[0]) + addName.Substring(1);

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

        ImGui.SameLine();


        if (addTargetErrorTime.HasValue && DateTime.Now - addTargetErrorTime.Value < saveMessageDuration)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), addTargetError);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (selectedTargetIndex < 0)
        {
            ImGui.TextColored(new Vector4(1.0f, 0.85f, 0.0f, 1.0f), "⚠ Select a target to configure thresholds.");
            return;
        }

        var currentConfig = currentDuty.Targets[selectedTargetIndex];

        ImGui.Text($"Configure Health Thresholds for {currentConfig.TargetName}:");

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

    public void OnOpen()
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
            selectedTargetIndex = -1;
        }
    }
}
