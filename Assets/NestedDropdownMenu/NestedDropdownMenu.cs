using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NestedDropdownMenuSystem
{
    /// <summary>
    /// DropdownMenu that supports hierarchical submenus based on GenericDropdownMenu.
    /// </summary>
    /// <remarks>
    /// ルートメニューはGenericDropdownMenuをほぼそのまま使用し、
    /// サブメニューはGenericDropdownMenu.DropDown()せずにOuterContainer以下のVisualElementを抜き出して、
    /// </remarks>
    public class NestedDropdownMenu
    {
        private readonly Dictionary<string, GenericMenu> _menuTable = new();

        private GenericMenu RootMenu
        {
            get
            {
                if (!_menuTable.TryGetValue(string.Empty, out var menu))
                {
                    menu = new GenericMenu();
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

        public void DropDown(Rect position, VisualElement targetElement = null, bool anchored = false) =>
            DoDropdown(position, targetElement, anchored);

        #endregion


        private (GenericMenu menu, string label) GetMenuAndLabel(string itemName)
        {
            var (path, label) = ParseItemNameToPathAndLabel(itemName);

            if (!_menuTable.TryGetValue(path, out var menu))
            {
                menu = new GenericMenu();
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
        private void AddSubMenuItem(GenericMenu menu, GenericMenu subMenu, string label)
        {
            if (menu == null || subMenu == null) return;

            const int delayMs = 500;
            menu.AddSubmenuItem(label, delayMs, subMenu);
        }

        // private void ShowSubMenu(GenericDropdownMenu subMenu, VisualElement targetElement)
        // {
        //     if (subMenu == null) return;
        //
        //     var rootMenu = RootMenu;
        //     var menuContainer = rootMenu.GetMenuContainer();
        //
        //     var rectWorld = targetElement?.worldBound ?? new Rect(Vector2.zero, Vector2.zero);
        //     var position = menuContainer.WorldToLocal(new Vector2(rectWorld.xMax, rectWorld.yMin));
        //
        //     var outerContainer = subMenu.GetOuterContainer();
        //     var style = outerContainer.style;
        //     style.left = position.x;
        //     style.top = position.y;
        //
        //     menuContainer.Add(outerContainer);
        // }


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


        private void DoDropdown(Rect rect, VisualElement targetElement = null, bool anchored = false)
        {
            var rootMenu = RootMenu;
            rootMenu.DropDown(rect, targetElement, anchored);
            // ModifyRootMenuCallbacks(rootMenu);
        }


        /// <summary>
        /// RootMenuのコールバックを付け替えて、NestedDropdownMenuの動作に合わせる
        /// </summary>
        /// <param name="menu"></param>
        private void ModifyRootMenuCallbacks(GenericDropdownMenu menu)
        {
            if (menu == null)　return;

            // OnFocusOutでscrollViewの範囲外にマウスがあるときtにHide()してしまう
            // これはサブメニュー上にマウスがある場合は閉じないようにしたい
            // そのためにRootMenuのOnFocusOutイベントを無効化し、自前のイベントを登録する
            var scrollView = menu.GetScrollView();
            scrollView.UnregisterCallback(menu.GetOnFocusOutDelegate());

            var rootMenuContainer = menu.GetMenuContainer();
            // rootMenuContainer.RegisterCallback<PointerDownEvent>(OnPointerDown);
            // rootMenuContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            // rootMenuContainer.RegisterCallback<FocusOutEvent>(OnFocusOut);
            //
            // rootMenuContainer.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

    }
}