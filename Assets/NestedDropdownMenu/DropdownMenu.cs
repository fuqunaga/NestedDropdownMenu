using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

#nullable  enable

namespace NestedDropdownMenuSystem
{
    /// <summary>
    /// Enhanced GenericDropdownMenu with support for hover-delayed submenu popup functionality.
    /// </summary>
    public class GenericMenu : GenericDropdownMenu
    {
        private readonly Dictionary<VisualElement, GenericMenu> _itemToSubmenuTable = new();
        private readonly VisualElement _outerContainer;
        private readonly VisualElement _scrollView;

        private GenericMenu? _parentMenu;
        
        private GenericMenu RootMenu => _parentMenu?.RootMenu ?? this;
        private VisualElement RootMenuContainer => _parentMenu?.RootMenuContainer ?? contentContainer.GetFirstAncestorByClassName(ussClassName);
        private bool IsRootMenu => _parentMenu == null;

        public GenericMenu()
        {
            _outerContainer = contentContainer.GetFirstAncestorByClassName(containerOuterUssClassName);
            _scrollView = contentContainer.GetFirstAncestorByClassName(containerInnerUssClassName);
            
            _outerContainer.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            _outerContainer.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            _outerContainer.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _outerContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _outerContainer.RegisterCallback<PointerUpEvent>(OnPointerUp);
            
            RootMenuContainer.RegisterCallback<PointerDownEvent>(OnPointerDown);
            RootMenuContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            _outerContainer.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _outerContainer.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _outerContainer.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            
            RootMenuContainer.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            RootMenuContainer.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        }


        private void OnPointerDown(PointerDownEvent evt)
        {
            CallPrivateMethod(nameof(OnPointerDown), evt);
            HideSubmenuIfItemUnselected();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            CallPrivateMethod(nameof(OnPointerMove), evt);
            HideSubmenuIfItemUnselected();
        }
        
        private void OnPointerUp(PointerUpEvent evt)
        {
            CallPrivateMethod(nameof(OnPointerUp), evt);
            if (GetSelectedIndex() != -1)
            {
               RootMenu.Hide(true);
            }
        }
        
        private void HideSubmenuIfItemUnselected()
        {
            var selectedItem = GetSelectedItem();
            
            foreach(var (item, submenu) in _itemToSubmenuTable)
            {
                if ( item != selectedItem )
                {
                    submenu.HideAsSubmenu();
                }
            }
        }

        
        private object CallPrivateMethod(string methodName, params object[] parameters)
        {
            var methodInfo = GetPrivateMethodInfo(methodName);
            Assert.IsNotNull(methodInfo, $"Method '{methodName}' not found in GenericDropdownMenu");
            return methodInfo.Invoke(this, parameters);
        }
        
        
        private static MethodInfo GetPrivateMethodInfo(string methodName)
        {
            var methodInfo = typeof(GenericDropdownMenu).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(methodInfo, $"Method '{methodName}' not found in GenericDropdownMenu");
            return methodInfo;
        }

        private VisualElement? GetSelectedItem()
        {
            var index = GetSelectedIndex();
            if ( index < 0 || index >= _scrollView.childCount )
            {
                return null;
            }

            return _scrollView.Children()
                .Where(ve => ve.ClassListContains(itemUssClassName))
                .ElementAt(index);
        }
        
        private int GetSelectedIndex()
        {
            return (int)CallPrivateMethod(nameof(GetSelectedIndex));
        }

        public void Hide(bool giveFocusBack = false)
        {
            CallPrivateMethod(nameof(Hide), giveFocusBack);
        }

        public void AddSubmenuItem(string itemName, long delayMs, GenericMenu subMenu)
        {
            AddItem(itemName, false, null);
            var item = contentContainer.Children().Last();
            
            _itemToSubmenuTable[item] = subMenu;
            
            // PointerEnterして一定時間経過後にサブメニューを表示する
            // PointerLeaveしたら時間計測をストップ
            IVisualElementScheduledItem? scheduledItem = null;

            item.RegisterCallback<PointerEnterEvent>(_ =>
            {
                scheduledItem ??= item.schedule.Execute(() => subMenu.ShowAsSubmenu(this, item));
                scheduledItem.ExecuteLater(delayMs);
            });

            item.RegisterCallback<PointerLeaveEvent>(_ => { scheduledItem?.Pause(); });
        }

        private void ShowAsSubmenu(GenericMenu parentMenu, VisualElement targetElement)
        {
            _parentMenu = parentMenu;
            
            // TODO: ほかのサブメニューも考慮
            var rectWorld = targetElement.worldBound;
            var position = RootMenuContainer.WorldToLocal(new Vector2(rectWorld.xMax, rectWorld.yMin));
            
            var style = _outerContainer.style;
            style.left = position.x;
            style.top = position.y;
            
            RootMenuContainer.Add(_outerContainer);
        }

        private void HideAsSubmenu()
        {
            _outerContainer.RemoveFromHierarchy();
            _parentMenu = null;
        }
    }
}
        
