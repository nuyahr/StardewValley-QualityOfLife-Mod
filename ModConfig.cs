using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace QualityOfLife
{
    public class ModConfig
    {
        // Time & Clock
        public int RealSecondsPer10Minutes { get; set; } = 45;
        public bool FastForwardAtTables { get; set; } = true;
        public int TableTimeSpeed { get; set; } = 100;

        // Stamina - Cooldown & Recovery Rates (per second)
        public int StaminaRegenCooldownSeconds { get; set; } = 3;
        public float StaminaRegenTable { get; set; } = 25.0f;
        public float StaminaRegenSitting { get; set; } = 3.0f;
        public float StaminaRegenStanding { get; set; } = 1.5f;
        public float StaminaRegenWalking { get; set; } = 1.0f;
        public float StaminaRegenFishing { get; set; } = 0.15f;
        public bool ZeroCastingCost { get; set; } = true;

        // Health
        public bool EnableHealthRegen { get; set; } = true;
        public int SecondsPerHealthPoint { get; set; } = 3;

        // Movement & Sprint (PC Keybind + Mobile Auto-Sprint)
        public bool EnableAutoSprint { get; set; } = true;
        public KeybindList SprintKey { get; set; } = KeybindList.Parse("LeftAlt");
        public int SecondsMovingToSprint { get; set; } = 10;
        public int SprintSpeedBonus { get; set; } = 1;
        public float SprintStaminaCostPerTick { get; set; } = 0.02f;

        // Weather
        public bool EnableWeatherVariety { get; set; } = true;
        public double WinterSnowChance { get; set; } = 0.35;
        public double StormChance { get; set; } = 0.05;
        public double RainChance { get; set; } = 0.22;
    }
}