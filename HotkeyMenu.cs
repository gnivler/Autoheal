using System;
using Kingmaker.UI.SettingsUI;
using ModMaker;
using ModMaker.Utility;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;
using static ModMaker.Utility.RichTextExtensions;
using static Autoheal.Main;

namespace Autoheal
{
    public class HotkeyMenu : IMenuTopPage
    {
        private string _waitingHotkeyName;

        GUIStyle _buttonStyle;
        GUIStyle _downButtonStyle;
        GUIStyle _labelStyle;

        public string Name => "Hotkey setup";

        public int Priority => 400;

        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            try
            {
                if (_buttonStyle == null)
                {
                    _buttonStyle = new GUIStyle(GUI.skin.button) {alignment = TextAnchor.MiddleLeft};
                    _downButtonStyle = new GUIStyle(_buttonStyle)
                    {
                        focused = _buttonStyle.active,
                        normal = _buttonStyle.active,
                        hover = _buttonStyle.active
                    };
                    _downButtonStyle.active.textColor = Color.gray;
                    _labelStyle = new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleLeft, padding = _buttonStyle.padding};
                }

                if (!string.IsNullOrEmpty(_waitingHotkeyName) && HotkeyHelper.ReadKey(out BindingKeysData newKey))
                {
                    Main.hotkeys.SetHotkey(_waitingHotkeyName, newKey);
                    _waitingHotkeyName = null;
                }

                IDictionary<string, BindingKeysData> hotkeys = Main.hotkeys.BindingKeys;

                using (new GUILayout.HorizontalScope())
                {
                    using (new GUILayout.VerticalScope())
                    {
                        foreach (KeyValuePair<string, BindingKeysData> item in hotkeys)
                        {
                            GUIHelper.ToggleButton(item.Value != null,
                                item.Key.ToSentence(), _labelStyle, GUILayout.ExpandWidth(true));
                        }
                    }

                    GUILayout.Space(10f);

                    using (new GUILayout.VerticalScope())
                    {
                        foreach (BindingKeysData key in hotkeys.Values)
                        {
                            GUILayout.Label(HotkeyHelper.GetKeyText(key));
                        }
                    }

                    GUILayout.Space(10f);

                    using (new GUILayout.VerticalScope())
                    {
                        foreach (string name in hotkeys.Keys)
                        {
                            bool waitingThisHotkey = _waitingHotkeyName == name;
                            if (GUILayout.Button("Set", waitingThisHotkey ? _downButtonStyle : _buttonStyle))
                            {
                                if (waitingThisHotkey)
                                    _waitingHotkeyName = null;
                                else
                                    _waitingHotkeyName = name;
                            }
                        }
                    }

                    using (new GUILayout.VerticalScope())
                    {
                        string hotkeyToClear = default;
                        foreach (string name in hotkeys.Keys)
                        {
                            if (GUILayout.Button($"Clear", _buttonStyle))
                            {
                                hotkeyToClear = name;

                                if (_waitingHotkeyName == name)
                                    _waitingHotkeyName = null;
                            }
                        }

                        if (!string.IsNullOrEmpty(hotkeyToClear))
                            Main.hotkeys.SetHotkey(hotkeyToClear, null);
                    }

                    using (new GUILayout.VerticalScope())
                    {
                        foreach (KeyValuePair<string, BindingKeysData> item in hotkeys)
                        {
                            if (item.Value != null && !HotkeyHelper.CanBeRegistered(item.Key, item.Value))
                            {
                                GUILayout.Label($"Duplicated!!".Color(RGBA.yellow));
                            }
                            else
                            {
                                GUILayout.Label(string.Empty);
                            }
                        }
                    }

                    GUILayout.FlexibleSpace();
                }

                mod.Settings.miser = GUIHelper.ToggleButton(mod.Settings.miser, $"Miser mode (mitigates wastage)", _buttonStyle, GUILayout.ExpandWidth(false));
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }
    }
}
