#nullable  enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace NestedDropdownMenuSystem
{
    /// <summary>
    /// Enhanced GenericDropdownMenu with support for hover-delayed submenu popup functionality.
    /// </summary>
    /// <remarks>
    /// GenericDropdownMenuを部分的に使用してサブメニューを表示するための機能を追加したクラス
    /// ルートメニューのみMenuContainer（画面全体を覆うコンテナ）を使用し、
    /// サブメニューはOuterContainerをルートメニューのMenuContainerにAddする
    /// 
    /// GenericDropdownMenuはポインターのイベントをMenuContainerで受けるがここでは各メニューのOuterContainerで受けるようにしている
    /// また、各サブメニューはPointerDown、PointerMoveをルートMenuContainerにも登録している
    /// さらに各コールバックがStopPropagationをしてそれ以降へ伝播しないことを当て込んでいる
    ///
    /// 以上の設定で次の挙動を実現している
    /// - サブメニュー上にポインターがあればそのポインターイベントが呼ばれイベント終了
    /// - すべての子要素上にポインターがない場合、MenuContainerに最後に登録された最も子であるPointerDown,PointerMoveが呼ばれメニュー範囲外のとき選択が解除される
    /// </remarks>
    public class SingleMenu : GenericDropdownMenu
    {
        #region Static
        
        #region Access private
        
        private static readonly Action<GenericDropdownMenu, PointerDownEvent> OnPointerDownFunc;
        private static readonly Action<GenericDropdownMenu, PointerMoveEvent> OnPointerMoveFunc;
        private static readonly Action<GenericDropdownMenu, PointerUpEvent> OnPointerUpFunc;

        private static readonly Func<GenericDropdownMenu, int> GetSelectedIndexFunc;
        private static readonly Action<GenericDropdownMenu, bool> HideFunc;

        static SingleMenu()
        {
            RegisterPrivateMethodToDelegate("OnPointerDown", out OnPointerDownFunc);
            RegisterPrivateMethodToDelegate("OnPointerMove", out OnPointerMoveFunc);
            RegisterPrivateMethodToDelegate("OnPointerUp", out OnPointerUpFunc);
            RegisterPrivateMethodToDelegate("GetSelectedIndex", out GetSelectedIndexFunc);
            RegisterPrivateMethodToDelegate("Hide", out HideFunc);
            
            return;


            static void RegisterPrivateMethodToDelegate<TFunc>(string methodName, out TFunc func) 
                where TFunc : Delegate
            {
                var methodInfo = typeof(GenericDropdownMenu).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(methodInfo, $"Method '{methodName}' not found in GenericDropdownMenu");
                
                func = (TFunc)Delegate.CreateDelegate(typeof(TFunc), methodInfo);
            }
        }
        
        #endregion
        
        private static VisualElement GetFirstAncestorByClassName(VisualElement element, string className)
        {
            while (element != null)
            {
                if (element.ClassListContains(className))
                {
                    return element;
                }

                element = element.parent;
            }

            throw new InvalidOperationException($"Ancestor with class name '{className}' not found.");
        }

        #endregion
        
        private readonly Dictionary<VisualElement, SingleMenu> _itemToSubmenuTable = new();
        private readonly VisualElement _outerContainer;
        private readonly VisualElement _scrollView;

        private SingleMenu? _parentMenu;
        
        
        private SingleMenu RootMenu => _parentMenu?.RootMenu ?? this;

        private VisualElement RootMenuContainer => GetFirstAncestorByClassName(RootMenu._outerContainer, ussClassName);
        

        public SingleMenu()
        {
            _outerContainer = GetFirstAncestorByClassName(contentContainer, containerOuterUssClassName);
            _scrollView = GetFirstAncestorByClassName(contentContainer, containerInnerUssClassName);
            
            _outerContainer.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            _outerContainer.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        #region Callbacks
        
        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            _outerContainer.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _outerContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _outerContainer.RegisterCallback<PointerUpEvent>(OnPointerUp);
            
            var rootMenuContainer = RootMenuContainer;
            rootMenuContainer.RegisterCallback<PointerDownEvent>(OnPointerDown);
            rootMenuContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            _outerContainer.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _outerContainer.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _outerContainer.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            
            var rootMenuContainer = RootMenuContainer;
            rootMenuContainer.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            rootMenuContainer.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        }


        private void OnPointerDown(PointerDownEvent evt)
        {
            OnPointerDownFunc(this, evt);
            HideSubmenusForUnselectedItems();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            OnPointerMoveFunc(this, evt);
            HideSubmenusForUnselectedItems();
        }
        
        private void OnPointerUp(PointerUpEvent evt)
        {
            OnPointerUpFunc(this, evt);
            if (GetSelectedIndexFunc(this) != -1)
            {
               HideFunc(RootMenu, true);
            }
        }

        #endregion
        
        
        private void HideSubmenusForUnselectedItems()
        {
            var selectedItem = GetSelectedItem();
            if (selectedItem == null) return;
            
            foreach(var (item, submenu) in _itemToSubmenuTable)
            {
                if ( item != selectedItem )
                {
                    submenu.HideAsSubmenu();
                }
            }
        }
        
        private VisualElement? GetSelectedItem()
        {
            var selectedIndex = GetSelectedIndexFunc(this);
            if (selectedIndex < 0 || selectedIndex >= _scrollView.childCount)
            {
                return null;
            }
            
            return _scrollView.Children()
                .Where(ve => ve.ClassListContains(itemUssClassName))
                .ElementAt(selectedIndex);
        }
        

        public void AddSubmenuItem(string itemName, long delayMs, SingleMenu subMenu)
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

        
        private void ShowAsSubmenu(SingleMenu parentMenu, VisualElement targetElement)
        {
            _parentMenu = parentMenu;
            
            _outerContainer.RegisterCallbackOnce<GeometryChangedEvent>(_ => UpdateSubmenuPosition(targetElement));
            
            RootMenuContainer.Add(_outerContainer);
        }
        
        /// <summary>
        /// RootMenuContainerローカル座標系ではみ出ないようにサブメニューの位置を調整する
        /// </summary>
        /// <param name="targetElement"></param>
        private void UpdateSubmenuPosition(VisualElement targetElement)
        {
            var rectWorld = targetElement.worldBound;
            var rootMenuContainer = RootMenuContainer;
            var position = rootMenuContainer.WorldToLocal(new Vector2(rectWorld.xMax, rectWorld.yMin));

            // firstItemとtargetElementのYの位置を揃える
            var firstItem = _scrollView.Children().FirstOrDefault();
            if (firstItem != null)
            {
                var fistItemWorldPosition = firstItem.worldBound.position;
                var firstItemPositionOnOuterContainer = _outerContainer.WorldToLocal(fistItemWorldPosition);
                position.y -= firstItemPositionOnOuterContainer.y;
            }
            
            
            var rootRect = rootMenuContainer.layout;
            var outerContainerRect = _outerContainer.layout;
            
            // 右側がルートからはみ出るようなら親メニューの左側に表示する
            // ただし左側がルートからはみ出すようならルートの左側に揃える
            if (position.x + outerContainerRect.width > rootRect.width)
            {
                if (_parentMenu is { } parentMenu)
                {
                    var parentOuterContainerRect = parentMenu._outerContainer.layout;
                    position.x = Mathf.Max(0f, parentOuterContainerRect.xMin - outerContainerRect.width);
                }
            }
            
            // 下端がルートからはみ出るようならルートの下端に揃える
            // ただし上端がルートからはみ出すようならルートの上端に揃える
            if (position.y + outerContainerRect.height > rootRect.height)
            {
                position.y = Mathf.Max(0f, rootRect.height - outerContainerRect.height);
            }

            var style = _outerContainer.style;
            style.left = position.x;
            style.top = position.y;
            
            // サブメニューがルートメニューより長い場合は縮めてスクロールビューに頼る
            if (outerContainerRect.height > rootRect.height)
            {
                style.maxHeight = rootRect.height;
            }
        }
        

        private void HideAsSubmenu()
        {
            foreach(var submenu in _itemToSubmenuTable.Values)
            {
                submenu.HideAsSubmenu();
            }
            
            _outerContainer.RemoveFromHierarchy();
            _parentMenu = null;
        }
    }
}
        
