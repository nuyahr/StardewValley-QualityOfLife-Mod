using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Objects;
using StardewValley.Tools;

namespace QualityOfLife
{
    public class ModEntry : Mod
    {
        private ModConfig Config;

        private bool waitingAtTable = false;
        private int realSecondsAccumulated = 0;
        
        private float lastRecordedStamina = -1f;
        private int ticksWithoutStaminaCost = 0;
        private int healthSecondsCounter = 0;
        private int regenSecondsCounter = 0;

        private int continuousMovingTicks = 0;

        public override void Entry(IModHelper helper)
        {
            this.Config = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdateTicked;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            // --- Section: Time ---
            configMenu.AddSectionTitle(this.ModManifest, () => "Time & Clock");
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => this.Config.RealSecondsPer10Minutes,
                setValue: value => this.Config.RealSecondsPer10Minutes = value,
                name: () => "Real Seconds per 10 Mins",
                tooltip: () => "Default is 45. Higher values make the day longer.",
                min: 5, max: 300, interval: 5
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                getValue: () => this.Config.FastForwardAtTables,
                setValue: value => this.Config.FastForwardAtTables = value,
                name: () => "Fast Forward at Tables"
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => this.Config.TableTimeSpeed,
                setValue: value => this.Config.TableTimeSpeed = value,
                name: () => "Table Time Speed",
                min: 20, max: 300, interval: 10
            );

            // --- Section: Stamina & Health ---
            configMenu.AddSectionTitle(this.ModManifest, () => "Stamina & Health");
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => this.Config.StaminaRegenCooldownSeconds,
                setValue: value => this.Config.StaminaRegenCooldownSeconds = value,
                name: () => "Stamina Regen Cooldown (Sec)",
                min: 0, max: 15, interval: 1
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => this.Config.StaminaRegenTable,
                setValue: value => this.Config.StaminaRegenTable = value,
                name: () => "Regen: At Table",
                min: 1f, max: 50f, interval: 1f
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => this.Config.StaminaRegenSitting,
                setValue: value => this.Config.StaminaRegenSitting = value,
                name: () => "Regen: Sitting",
                min: 0f, max: 15f, interval: 0.5f
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => this.Config.StaminaRegenStanding,
                setValue: value => this.Config.StaminaRegenStanding = value,
                name: () => "Regen: Standing Still",
                min: 0f, max: 10f, interval: 0.5f
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => this.Config.StaminaRegenWalking,
                setValue: value => this.Config.StaminaRegenWalking = value,
                name: () => "Regen: Walking",
                min: 0f, max: 10f, interval: 0.5f
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => this.Config.StaminaRegenFishing,
                setValue: value => this.Config.StaminaRegenFishing = value,
                name: () => "Regen: Fishing",
                min: 0f, max: 5f, interval: 0.05f
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                getValue: () => this.Config.ZeroCastingCost,
                setValue: value => this.Config.ZeroCastingCost = value,
                name: () => "Zero Casting Cost"
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                getValue: () => this.Config.EnableHealthRegen,
                setValue: value => this.Config.EnableHealthRegen = value,
                name: () => "Enable Health Regeneration"
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => this.Config.SecondsPerHealthPoint,
                setValue: value => this.Config.SecondsPerHealthPoint = value,
                name: () => "Seconds per Health Point",
                min: 1, max: 20, interval: 1
            );

            // --- Section: Movement ---
            configMenu.AddSectionTitle(this.ModManifest, () => "Movement & Sprint");
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                getValue: () => this.Config.EnableAutoSprint,
                setValue: value => this.Config.EnableAutoSprint = value,
                name: () => "Auto-Sprint (Mobile / General)"
            );
            configMenu.AddKeybindList(
                mod: this.ModManifest,
                getValue: () => this.Config.SprintKey,
                setValue: value => this.Config.SprintKey = value,
                name: () => "Sprint Key (PC)"
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => this.Config.SecondsMovingToSprint,
                setValue: value => this.Config.SecondsMovingToSprint = value,
                name: () => "Seconds Moving to Auto-Sprint",
                min: 1, max: 30, interval: 1
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => this.Config.SprintSpeedBonus,
                setValue: value => this.Config.SprintSpeedBonus = value,
                name: () => "Sprint Speed Bonus",
                min: 1, max: 5, interval: 1
            );

            // --- Section: Weather ---
            configMenu.AddSectionTitle(this.ModManifest, () => "Weather & Atmosphere");
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                getValue: () => this.Config.EnableWeatherVariety,
                setValue: value => this.Config.EnableWeatherVariety = value,
                name: () => "Enable Extra Weather Variety"
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => (float)this.Config.WinterSnowChance,
                setValue: value => this.Config.WinterSnowChance = value,
                name: () => "Winter Snow Chance",
                min: 0f, max: 1f, interval: 0.05f
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => (float)this.Config.StormChance,
                setValue: value => this.Config.StormChance = value,
                name: () => "Storm Chance",
                min: 0f, max: 1f, interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => (float)this.Config.RainChance,
                setValue: value => this.Config.RainChance = value,
                name: () => "Rain Chance",
                min: 0f, max: 1f, interval: 0.01f
            );
        }

        private bool IsTableNearby()
        {
            GameLocation location = Game1.currentLocation;
            if (location == null) return false;

            Vector2 pos = Game1.player.Tile;
            Vector2[] adjacentTiles = new Vector2[]
            {
                new Vector2(pos.X + 1, pos.Y),
                new Vector2(pos.X - 1, pos.Y),
                new Vector2(pos.X, pos.Y + 1),
                new Vector2(pos.X, pos.Y - 1)
            };

            foreach (Vector2 tile in adjacentTiles)
            {
                Furniture furniture = location.GetFurnitureAt(tile);
                if (furniture != null && furniture.furniture_type.Value == Furniture.table)
                {
                    return true;
                }
            }
            return false;
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.paused)
                return;

            bool sittingAtTableNow = this.Config.FastForwardAtTables && Game1.player.IsSitting() && IsTableNearby();

            if (sittingAtTableNow && !waitingAtTable)
            {
                waitingAtTable = true;
                Game1.addHUDMessage(new HUDMessage("Passing time...", HUDMessage.newQuest_type));
            }
            else if (!sittingAtTableNow && waitingAtTable)
            {
                waitingAtTable = false;
                Game1.addHUDMessage(new HUDMessage("Time back to normal.", HUDMessage.newQuest_type));
            }

            if (Game1.shouldTimePass())
            {
                if (waitingAtTable && Game1.timeOfDay < 2400)
                {
                    Game1.gameTimeInterval += this.Config.TableTimeSpeed;
                }
                else
                {
                    Game1.gameTimeInterval = 0; 
                }
            }

            bool isSprinting = false;
            int requiredSprintTicks = this.Config.SecondsMovingToSprint * 60;
            bool keyPressed = this.Config.SprintKey.IsDown();

            if (Game1.player.isMoving() && !Game1.player.IsSitting())
            {
                continuousMovingTicks++;

                bool activateByTime = this.Config.EnableAutoSprint && continuousMovingTicks >= requiredSprintTicks;

                if ((keyPressed || activateByTime) && Game1.player.Stamina > 2f)
                {
                    isSprinting = true;
                    if (!Game1.player.hasBuff("Nuya.QualityOfLife.Sprint"))
                    {
                        Buff sprintBuff = new Buff(
                            id: "Nuya.QualityOfLife.Sprint",
                            displayName: "Sprinting",
                            duration: 150,
                            effects: new BuffEffects { Speed = { this.Config.SprintSpeedBonus } }
                        );
                        Game1.player.applyBuff(sprintBuff);
                    }

                    Game1.player.Stamina = Math.Max(0f, Game1.player.Stamina - this.Config.SprintStaminaCostPerTick);
                }
            }
            else
            {
                continuousMovingTicks = 0;
            }

            bool holdingRod = Game1.player.CurrentTool is FishingRod;
            if (holdingRod && this.Config.ZeroCastingCost)
            {
                if (lastRecordedStamina > 0f && Game1.player.Stamina < lastRecordedStamina)
                {
                    Game1.player.Stamina = lastRecordedStamina;
                }
            }

            float currentStamina = Game1.player.Stamina;

            if (lastRecordedStamina < 0f)
                lastRecordedStamina = currentStamina;

            bool ignoreRodCost = holdingRod && this.Config.ZeroCastingCost;

            if ((currentStamina < lastRecordedStamina - 0.001f && !ignoreRodCost) || isSprinting || Game1.player.UsingTool)
            {
                ticksWithoutStaminaCost = 0;
            }
            else
            {
                ticksWithoutStaminaCost++;
            }

            lastRecordedStamina = Game1.player.Stamina;
        }

        private void OnOneSecondUpdateTicked(object sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.paused)
                return;

            if (Game1.shouldTimePass() && !waitingAtTable)
            {
                realSecondsAccumulated++;
                if (realSecondsAccumulated >= this.Config.RealSecondsPer10Minutes)
                {
                    realSecondsAccumulated = 0;
                    
                    int minutes = Game1.timeOfDay % 100;
                    int hours = Game1.timeOfDay - minutes;
                    minutes += 10;
                    if (minutes >= 60)
                    {
                        hours += 100;
                        minutes -= 60;
                    }
                    Game1.timeOfDay = hours + minutes;
                }
            }

            if (this.Config.EnableHealthRegen)
            {
                healthSecondsCounter++;
                if (healthSecondsCounter >= this.Config.SecondsPerHealthPoint)
                {
                    healthSecondsCounter = 0;
                    if (Game1.player.health < Game1.player.maxHealth)
                    {
                        Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + 1);
                    }
                }
            }

            regenSecondsCounter++;
            if (regenSecondsCounter >= 1)
            {
                regenSecondsCounter = 0;

                bool holdingRod = Game1.player.CurrentTool is FishingRod;
                int cooldownTicks = this.Config.StaminaRegenCooldownSeconds * 60;

                if (waitingAtTable || holdingRod || ticksWithoutStaminaCost >= cooldownTicks)
                {
                    float gain = 0f;
                    if (waitingAtTable)
                    {
                        gain = this.Config.StaminaRegenTable; 
                    }
                    else if (holdingRod)
                    {
                        gain = this.Config.StaminaRegenFishing;
                    }
                    else if (Game1.player.IsSitting())
                    {
                        gain = this.Config.StaminaRegenSitting; 
                    }
                    else if (Game1.player.isMoving())
                    {
                        gain = this.Config.StaminaRegenWalking; 
                    }
                    else
                    {
                        gain = this.Config.StaminaRegenStanding; 
                    }

                    if (gain > 0f)
                    {
                        Game1.player.Stamina = Math.Min(Game1.player.MaxStamina, Game1.player.Stamina + gain);
                        lastRecordedStamina = Game1.player.Stamina;
                    }
                }
            }
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            waitingAtTable = false;
            realSecondsAccumulated = 0;
            lastRecordedStamina = Game1.player.Stamina;
            ticksWithoutStaminaCost = this.Config.StaminaRegenCooldownSeconds * 60;

            if (this.Config.EnableWeatherVariety && Game1.weatherForTomorrow == Game1.weather_sunny)
            {
                double extraChance = Game1.random.NextDouble();

                if (Game1.currentSeason == "winter")
                {
                    if (extraChance < this.Config.WinterSnowChance)
                        Game1.weatherForTomorrow = Game1.weather_snow;
                }
                else
                {
                    if (extraChance < this.Config.StormChance)
                        Game1.weatherForTomorrow = Game1.weather_lightning;
                    else if (extraChance < this.Config.RainChance)
                        Game1.weatherForTomorrow = Game1.weather_rain;
                }
            }
        }
    }

    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddSectionTitle(IManifest mod, Func<string> text, Func<string> tooltip = null);
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);
        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string> tooltip = null, int? min = null, int? max = null, int? interval = null, string fieldId = null);
        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string> tooltip = null, float? min = null, float? max = null, float? interval = null, string fieldId = null);
        void AddKeybindList(IManifest mod, Func<KeybindList> getValue, Action<KeybindList> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);
    }
}