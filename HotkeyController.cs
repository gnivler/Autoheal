using Kingmaker.PubSubSystem;
using Kingmaker.UI;
using Kingmaker.UI.SettingsUI;
using ModMaker;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Autoheal
{
    public class HotkeyController :
        IModEventHandler,
        ISceneHandler
    {
        public IDictionary<string, BindingKeysData> BindingKeys => Main.mod.Settings.hotkeys;

        internal void Initialize()
        {
            Dictionary<string, BindingKeysData> hotkeys = new Dictionary<string, BindingKeysData>()
            {
                {"Autoheal all characters with magic", new BindingKeysData() {IsCtrlDown = true, Key = KeyCode.H}},
                {"Autoheal all characters with consumables", new BindingKeysData() {IsAltDown = true, IsCtrlDown = true, Key = KeyCode.T}},
                {"Autoheal selected character with consumables", new BindingKeysData() {IsCtrlDown = true, Key = KeyCode.T}},
            };

            // remove invalid keys from the settings
            foreach (string name in BindingKeys.Keys.ToList())
                if (!hotkeys.ContainsKey(name))
                    BindingKeys.Remove(name);

            // add missing keys to the settings
            foreach (KeyValuePair<string, BindingKeysData> item in hotkeys)
                if (!BindingKeys.ContainsKey(item.Key))
                    BindingKeys.Add(item.Key, item.Value);
        }

        public void Update()
        {
            Initialize();
            RegisterAll();
        }

        public void SetHotkey(string name, BindingKeysData value)
        {
            BindingKeys[name] = value;
            TryRegisterHotkey(name, value);
        }

        private void RegisterAll()
        {
            foreach (KeyValuePair<string, BindingKeysData> item in BindingKeys)
                TryRegisterHotkey(item.Key, item.Value);
        }

        private void UnregisterAll()
        {
            foreach (string name in BindingKeys.Keys)
                TryRegisterHotkey(name, null);
        }

        private void TryRegisterHotkey(string name, BindingKeysData value)
        {
            if (value != null)
            {
                HotkeyHelper.RegisterKey(name, value, KeyboardAccess.GameModesGroup.World);
            }
            else
            {
                HotkeyHelper.UnregisterKey(name);
            }
        }

        public void HandleModEnable()
        {
            Main.hotkeys = this;
            Initialize();
            RegisterAll();

            EventBus.Subscribe(this);
        }

        public void HandleModDisable()
        {
            EventBus.Unsubscribe(this);

            UnregisterAll();
            Main.hotkeys = null;
        }

        public void OnAreaBeginUnloading()
        {
        }

        public void OnAreaDidLoad()
        {
            RegisterAll();
        }
    }
}
