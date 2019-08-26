using System.Reflection;
using UnityModManagerNet;
using Harmony12;
using Kingmaker.UI.SettingsUI;
using ModMaker;
using ModMaker.Utility;

// ReSharper disable InconsistentNaming

namespace Autoheal
{
    public class Settings : UnityModManager.ModSettings
    {
        public bool miser;
        public SerializableDictionary<string, BindingKeysData> hotkeys = new SerializableDictionary<string, BindingKeysData>();
    }

    internal class Main
    {
        internal static HotkeyController hotkeys = new HotkeyController();
        internal static ModManager<Main, Settings> mod;
        private static MenuManager menu;
        private static readonly HarmonyInstance harmony = HarmonyInstance.Create("ca.gnivler.pathfinder.autoheal");

        internal static void Load(UnityModManager.ModEntry modEntry)
        {
            Log(new string('─', 80) + " START");
            modEntry.OnToggle = OnToggle;
            modEntry.OnUnload = Unload;
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
            mod = new ModManager<Main, Settings>(modEntry, assembly);
            menu = new MenuManager(modEntry, assembly);
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value)
            {
                mod.Enable(modEntry);
                menu.Enable(modEntry);
                HotkeyHelper.Bind("Autoheal all characters with magic", Helpers.HealMagic);
                HotkeyHelper.Bind("Autoheal all characters with consumables", Helpers.HealAllConsumables);
                HotkeyHelper.Bind("Autoheal selected character with consumables", Helpers.HealOneConsumables);
            }
            else
            {
                menu.Disable(modEntry);
                mod.Disable(modEntry, false);
                ReflectionCache.Clear();
            }

            return true;
        }

        private static bool Unload(UnityModManager.ModEntry mod_Entry)
        {
            menu = null;
            mod.Disable(mod_Entry, true);
            mod = null;
            return true;
        }

        internal static void Log(object input)
        {
            if (input == null)
            {
                input = "null";
            }

            //FileLog.Log($"[Autoheal] {input}");
        }
    }
}
