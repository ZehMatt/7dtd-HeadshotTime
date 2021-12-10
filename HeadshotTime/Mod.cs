using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeadshotTime
{
    static class Config
    {
        public static float HeadshotMultiplier = 3f;

        internal static bool Load(string path)
        {
            Log.Out($"Loading configuration {path}");

            var doc = new System.Xml.XmlDocument();
            try
            {
                doc.Load(path);

                var root = doc.DocumentElement;
                var settingsNode = root.SelectSingleNode("Settings");
                for (int i = 0; i < settingsNode.ChildNodes.Count; i++)
                {
                    var child = settingsNode.ChildNodes[i];
                    if (child.Name == "HeadshotMultiplier")
                    {
                        var attribValue = child.Attributes.GetNamedItem("value");
                        HeadshotMultiplier = float.Parse(attribValue.Value);
                        Log.Out($"Headshot Multiplier: {HeadshotMultiplier}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Out($"Failed to read ModInfo XML: {ex.Message}");
                return false;
            }

            return true;
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(EntityAlive))]
    [HarmonyLib.HarmonyPatch("DamageEntity")]
    public class DamageHook
    {
        static void Prefix(EntityAlive __instance, DamageSource _damageSource, ref int _strength, bool _criticalHit, float _impulseScale = 1f)
        {
            var entity = __instance;

            // Check if piercing damage type, this means bullets (unsure if spears apply, not tested).
            if (_damageSource.GetDamageType() != EnumDamageTypes.Piercing)
                return;

            // Only care if the head is hit.
            if (_damageSource.GetEntityDamageBodyPart(entity) != EnumBodyPartHit.Head)
                return;

            // Check if the entity has armor.
            var attacker = GameManager.Instance.World.GetEntity(_damageSource.getEntityId()) as EntityAlive;
            if (entity.equipment != null && attacker != null)
            {
                if (entity.equipment.GetTotalPhysicalArmorRating(attacker, _damageSource.AttackingItem) > 0f)
                {
                    return;
                }
            }

            // Scale the headshot damage up.
            _strength += (int)(_strength * Config.HeadshotMultiplier);
        }
    }

    public class HeadshotTime : IModApi
    {
        void IModApi.InitMod(Mod _modInstance)
        {
            Config.Load(System.IO.Path.Combine(_modInstance.Path, "ModInfo.xml"));

            var harmony = new HarmonyLib.Harmony("HeadshotTime.Hooks");
            harmony.PatchAll();
        }
    }
}
