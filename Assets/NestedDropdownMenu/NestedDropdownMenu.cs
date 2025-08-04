using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NestedDropdownMenuSystem
{
    /// <summary>
    /// DropdownMenu that supports hierarchical submenus based on GenericDropdownMenu.
    /// </summary>
    public class NestedDropdownMenu
    {
        private readonly Dictionary<string, SingleMenu> _menuTable = new();

        private SingleMenu RootMenu
        {
            get
            {
                if (!_menuTable.TryGetValue(string.Empty, out var menu))
                {
                    menu = new SingleMenu();
                    _menuTable[string.Empty] = menu;

                }

                return menu;
            }
        }


        #region IGenericMenu

        public void AddItem(string itemName, bool isChecked, Action action)
        {
            var (menu, label) = GetMenuAndLabel(itemName);
            menu.AddItem(label, isChecked, action);
        }

        public void AddItem(string itemName, bool isChecked, Action<object> action, object data)
        {
            var (menu, label) = GetMenuAndLabel(itemName);
            menu.AddItem(label, isChecked, () => action(data));
        }

        public void AddDisabledItem(string itemName, bool isChecked)
        {
            var (menu, label) = GetMenuAndLabel(itemName);
            menu.AddDisabledItem(label, isChecked);
        }

        public void AddSeparator(string path)
        {
            var (menu, label) = GetMenuAndLabel(path);
            menu.AddSeparator(label);
        }

        public void DropDown(Rect position, VisualElement targetElement = null, bool anchored = false)
        {
            RootMenu.DropDown(position, targetElement, anchored);
        }

        #endregion


        private (SingleMenu menu, string label) GetMenuAndLabel(string itemName)
        {
            var (path, label) = ParseItemNameToPathAndLabel(itemName);

            if (!_menuTable.TryGetValue(path, out var menu))
            {
                menu = new SingleMenu();
                _menuTable[path] = menu;

                var isSubMenu = !string.IsNullOrEmpty(path);
                if (isSubMenu)
                {
                    // InitializeSubMenu(menu);

                    // 親メニューにサブメニューアイテムを追加
                    var (parentMenu, menuLabel) = GetMenuAndLabel(path);
                    AddSubMenuItem(parentMenu, menu, menuLabel);
                }
            }

            return (menu, label);
        }

        /// <summary>
        /// サブメニュー用のメニューアイテムを追加 
        /// </summary>
        private static void AddSubMenuItem(SingleMenu menu, SingleMenu subMenu, string label)
        {
            if (menu == null || subMenu == null) return;

            const int delayMs = 500;
            menu.AddSubmenuItem(label, delayMs, subMenu);
        }


        private static (string path, string label) ParseItemNameToPathAndLabel(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
            {
                return (string.Empty, string.Empty);
            }

            var lastSlashIndex = itemName.LastIndexOf('/');
            if (lastSlashIndex == -1)
            {
                return (string.Empty, itemName);
            }

            var path = itemName[..lastSlashIndex];
            var name = itemName[(lastSlashIndex + 1)..];
            return (path, name);
        }
    }
}