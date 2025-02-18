using System.ComponentModel;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Generic;

namespace InvisibilityMolotovPlugin
{
    public class InvisibilityMolotovConfig : BasePluginConfig
    {
        [Description("Duration of invisibility in seconds.")]
        public int InvisibilityDuration { get; set; } = 5;

        [Description("Cooldown duration in seconds.")]
        public int CooldownDuration { get; set; } = 15;

        [Description("Message sent when a player becomes invisible.")]
        public string InvisibilityMessage { get; set; } = "🫥 You are now invisible!";

        [Description("Message sent when a player becomes visible again.")]
        public string VisibilityMessage { get; set; } = "✨ You are visible again!";

        [Description("List of weapon names that trigger invisibility (e.g., weapon_molotov, weapon_incgrenade).")]
        public List<string> TriggerWeapons { get; set; } = new List<string> { "weapon_molotov", "weapon_incgrenade" };
    }

    public class InvisibilityMolotov : BasePlugin, IPluginConfig<InvisibilityMolotovConfig>
    {
        public override string ModuleName => "InvisibilityMolotov";
        public override string ModuleVersion => "1.2.0";
        public override string ModuleAuthor => "Vsat";

        public InvisibilityMolotovConfig Config { get; set; } = new();

        private readonly Dictionary<int, DateTime> _cooldowns = new();

        public override void Load(bool hotReload)
        {
            RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
        }

        public void OnConfigParsed(InvisibilityMolotovConfig config)
        {
            if (config.InvisibilityDuration < 1) config.InvisibilityDuration = 5;
            if (config.CooldownDuration < 1) config.CooldownDuration = 15;

            if (config.TriggerWeapons == null || config.TriggerWeapons.Count == 0)
            {
                config.TriggerWeapons = new List<string> { "weapon_molotov", "weapon_incgrenade" };
            }

            Config = config;
        }

        private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player.IsValid) SetVisibility(player.PlayerPawn.Value, true);
            return HookResult.Continue;
        }

        private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (!player.IsValid || !Config.TriggerWeapons.Contains(@event.Weapon)) return HookResult.Continue;

            int playerIndex = (int)player.Index;

            if (_cooldowns.TryGetValue(playerIndex, out var lastUsed) &&
                DateTime.Now < lastUsed.AddSeconds(Config.CooldownDuration))
            {
                player.PrintToChat("⚠️ Invisibility is on cooldown!");
                return HookResult.Continue;
            }

            SetVisibility(player.PlayerPawn.Value, false);
            _cooldowns[playerIndex] = DateTime.Now;

            AddTimer(Config.InvisibilityDuration, () =>
            {
                if (player.IsValid) SetVisibility(player.PlayerPawn.Value, true);
            });

            return HookResult.Continue;
        }

        private void SetVisibility(CCSPlayerPawn? pawn, bool visible)
        {
            if (pawn == null || !pawn.IsValid) return;

            pawn.Render = visible ?
                Color.FromArgb(255, 255, 255, 255) :
                Color.FromArgb(0, 255, 255, 255);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

            if (pawn.WeaponServices != null)
            {
                foreach (var weaponHandle in pawn.WeaponServices.MyWeapons)
                {
                    if (weaponHandle.IsValid && weaponHandle.Value != null && weaponHandle.Value.IsValid)
                    {
                        weaponHandle.Value.Render = visible ?
                            Color.FromArgb(255, 255, 255, 255) :
                            Color.FromArgb(0, 255, 255, 255);
                        Utilities.SetStateChanged(weaponHandle.Value, "CBaseModelEntity", "m_clrRender");
                    }
                }
            }

            var wearables = Utilities.FindAllEntitiesByDesignerName<CEconWearable>("wearable_item");
            foreach (var wearable in wearables)
            {
                if (wearable.IsValid && wearable.OwnerEntity?.Value?.Handle == pawn.Handle)
                {
                    wearable.Render = visible ?
                        Color.FromArgb(255, 255, 255, 255) :
                        Color.FromArgb(0, 255, 255, 255);
                    Utilities.SetStateChanged(wearable, "CBaseModelEntity", "m_clrRender");
                }
            }

            var playerController = pawn.Controller.Value?.As<CCSPlayerController>();
            if (playerController != null && playerController.IsValid)
            {
                playerController.PrintToChat(visible ? Config.VisibilityMessage : Config.InvisibilityMessage);
            }
        }
    }
}