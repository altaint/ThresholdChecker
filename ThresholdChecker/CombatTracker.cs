using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Linq;

namespace ThresholdChecker
{
    public class ThresholdResult
    {
        public ThresholdPhase Threshold { get; set; } = null!;
        public double ActualHpAtThreshold { get; set; }
        public double Difference => ActualHpAtThreshold - Threshold.TargetHpPercent;
    }

    public sealed class CombatTracker
    {
        private readonly Configuration configuration;

        public bool IsTracking => isTracking;
        public string TrackedTargetName { get; private set; } = "None";
        public double CurrentHpPercent { get; private set; } = 0.0;
        public TimeSpan CombatDuration { get; private set; } = TimeSpan.Zero;

        public TargetConfig? CurrentTargetConfig { get; private set; }

        public bool IsPhase2 { get; private set; } = false;

        public ThresholdPhase? NextThreshold { get; private set; }
        public double ProjectedHpPercent { get; private set; } = 100.0;
        public PacingState CurrentPace { get; private set; } = PacingState.OnTrack;

        public ThresholdResult? LastResult { get; private set; }
        public ThresholdPhase? LastEvaluatedThreshold { get; private set; }

        public string ErrorMessage { get; private set; } = string.Empty;

        private bool isTracking = false;
        private ulong trackedObjectId = 0;
        private DateTime? combatStartTime = null;

        public CombatTracker(Configuration configuration)
        {
            this.configuration = configuration;
        }

        public void ToggleTracking()
        {
            ErrorMessage = string.Empty;

            if (isTracking)
            {
                isTracking = false;
                TrackedTargetName = "None";
                CurrentHpPercent = 0;
                NextThreshold = null;
                LastResult = null;
                LastEvaluatedThreshold = null;
                CurrentTargetConfig = null;
                return;
            }

            var target = Service.TargetManager.Target;
            if (target == null)
            {
                ErrorMessage = "You don't have a target to track!";
                return;
            }

            var currentTerritoryId = Service.ClientState.TerritoryType;
            var currentDutyConfig = configuration.Duties.FirstOrDefault(d => d.TerritoryId == currentTerritoryId);

            if (currentDutyConfig == null)
            {
                ErrorMessage = "Current duty is not configured in the settings!";
                return;
            }

            trackedObjectId = target.GameObjectId;
            TrackedTargetName = target.Name.ToString();

            CurrentTargetConfig = currentDutyConfig.Targets.FirstOrDefault(t => string.Equals(t.TargetName, TrackedTargetName, StringComparison.OrdinalIgnoreCase));

            if (CurrentTargetConfig == null)
            {
                ErrorMessage = $"The target '{TrackedTargetName}' is not configured for this duty!";
                return;
            }

            isTracking = true;
            LastResult = null;
            LastEvaluatedThreshold = null;
            ProjectedHpPercent = 100.0;
            CurrentPace = PacingState.OnTrack;
        }

        public void TogglePhase(bool isPhase2)
        {
            if (IsPhase2 != isPhase2)
            {
                IsPhase2 = isPhase2;
                LastResult = null;
                LastEvaluatedThreshold = null;
                NextThreshold = null;
                ProjectedHpPercent = 100.0;
                CurrentPace = PacingState.OnTrack;
            }
        }

        public void OnFrameworkUpdate(IFramework framework)
        {
            bool inCombat = Service.Condition[ConditionFlag.InCombat];
            if (inCombat)
            {
                if (combatStartTime == null)
                {
                    combatStartTime = DateTime.Now;
                }

                CombatDuration = DateTime.Now - combatStartTime.Value;
            }
            else
            {
                combatStartTime = null;
                CombatDuration = TimeSpan.Zero;

                if (isTracking)
                {
                    NextThreshold = null;
                    LastEvaluatedThreshold = null;

                    LastResult = null;
                    ProjectedHpPercent = 100.0;
                    CurrentPace = PacingState.OnTrack;
                    CurrentHpPercent = 100.0;
                }
            }

            if (!isTracking) { return; }

            var target = Service.TargetManager.Target;

            if (target != null)
            {
                if (target.GameObjectId == trackedObjectId)
                {
                    if (target is Dalamud.Game.ClientState.Objects.Types.IBattleChara battleChara)
                    {
                        var maxHp = battleChara.MaxHp;
                        var currentHp = battleChara.CurrentHp;

                        if (maxHp > 0)
                        {
                            CurrentHpPercent = ((double)currentHp / maxHp) * 100.0;
                        }
                    }
                }
                else
                {
                    if (string.Equals(target.Name.ToString(), TrackedTargetName, StringComparison.OrdinalIgnoreCase))
                    {
                        trackedObjectId = target.GameObjectId;
                        if (target is Dalamud.Game.ClientState.Objects.Types.IBattleChara battleChara)
                        {
                            var maxHp = battleChara.MaxHp;
                            var currentHp = battleChara.CurrentHp;

                            if (maxHp > 0)
                            {
                                CurrentHpPercent = ((double)currentHp / maxHp) * 100.0;
                            }
                        }
                    }
                    else if (!inCombat)
                    {
                        CurrentHpPercent = 100.0;
                    }
                }
            }
            else if (!inCombat)
            {
                CurrentHpPercent = 100.0;
            }

            if (isTracking && inCombat && CombatDuration.TotalSeconds > 3 && CurrentTargetConfig != null)
            {
                var activeThresholds = IsPhase2 ? CurrentTargetConfig.P2Thresholds : CurrentTargetConfig.Thresholds;
                var thresholds = activeThresholds.OrderBy(t => t.TotalSeconds).ToList();
                var currentNext = thresholds.FirstOrDefault(t => t.TotalSeconds > CombatDuration.TotalSeconds);

                if (LastEvaluatedThreshold != null && currentNext != LastEvaluatedThreshold)
                {
                    if (CombatDuration.TotalSeconds >= LastEvaluatedThreshold.TotalSeconds)
                    {
                        LastResult = new ThresholdResult
                        {
                            Threshold = LastEvaluatedThreshold,
                            ActualHpAtThreshold = CurrentHpPercent
                        };
                    }
                }

                LastEvaluatedThreshold = currentNext;
                NextThreshold = currentNext;

                if (NextThreshold != null)
                {
                    double totalSecs = Math.Max(0.1, CombatDuration.TotalSeconds);

                    double hpDropped = 100.0 - CurrentHpPercent;
                    double dropRatePerSec = hpDropped / totalSecs;

                    ProjectedHpPercent = 100.0 - (dropRatePerSec * NextThreshold.TotalSeconds);

                    if (ProjectedHpPercent > NextThreshold.TargetHpPercent + CurrentTargetConfig.TolerancePercent)
                    {
                        CurrentPace = PacingState.Behind;
                    }
                    else if (ProjectedHpPercent < NextThreshold.TargetHpPercent - CurrentTargetConfig.TolerancePercent)
                    {
                        CurrentPace = PacingState.TooFast;
                    }
                    else
                    {
                        CurrentPace = PacingState.OnTrack;
                    }
                }
            }
        }

        public void OnTerritoryChanged(uint territoryId)
        {
            if (isTracking)
            {
                isTracking = false;
                TrackedTargetName = "None";
                CurrentHpPercent = 0;
                NextThreshold = null;
                LastResult = null;
                LastEvaluatedThreshold = null;
                CurrentTargetConfig = null;
                ErrorMessage = string.Empty;
                ProjectedHpPercent = 100.0;
                CurrentPace = PacingState.OnTrack;
                IsPhase2 = false;
            }
        }
    }
}
