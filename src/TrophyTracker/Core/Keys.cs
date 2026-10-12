using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace TrophyTracker.Core
{
    /// <summary>
    /// KeyboardShortcut.IsDown() fails while any other key is held (W while running); these only check the
    /// shortcut's own keys. InputBlocked: chat, console, menus and text fields have focus.
    /// </summary>
    internal static class Keys
    {
        public static bool Down(KeyboardShortcut s)
        {
            if (s.MainKey == KeyCode.None || !UnityInput.Current.GetKeyDown(s.MainKey))
                return false;
            foreach (KeyCode m in s.Modifiers)
            {
                if (!UnityInput.Current.GetKey(m))
                    return false;
            }
            return true;
        }

        public static bool Held(KeyCode key)
        {
            return key != KeyCode.None && UnityInput.Current.GetKey(key);
        }

        public static bool InputBlocked()
        {
            if (Console.IsVisible()) return true;
            if (Menu.IsVisible()) return true;
            if (TextInput.IsVisible()) return true;
            if (Minimap.InTextInput()) return true;
            if (Chat.instance != null && Chat.instance.HasFocus()) return true;
            return false;
        }
    }
}
