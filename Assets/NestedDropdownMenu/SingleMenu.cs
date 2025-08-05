#nullable  enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
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
        
        private static readonly Func<GenericDropdownMenu, KeyboardNavigationOperation, bool> ApplyFunc;
        private static readonly Action<GenericDropdownMenu, PointerDownEvent> OnPointerDownFunc;
        private static readonly Action<GenericDropdownMenu, PointerMoveEvent> OnPointerMoveFunc;
        private static readonly Action<GenericDropdownMenu, PointerUpEvent> OnPointerUpFunc;
        
        private static readonly Action<GenericDropdownMenu, int, int> ChangeSelectedIndexFunc;
        private static readonly Func<GenericDropdownMenu, int> GetSelectedIndexFunc;
        private static readonly Action<GenericDropdownMenu, bool> HideFunc;

        static SingleMenu()
        {
            var methodInfos = typeof(GenericDropdownMenu).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            
            RegisterPrivateMethodToDelegate("Apply", out ApplyFunc);
            RegisterPrivateMethodToDelegate("OnPointerDown", out OnPointerDownFunc);
            RegisterPrivateMethodToDelegate("OnPointerMove", out OnPointerMoveFunc);
            RegisterPrivateMethodToDelegate("OnPointerUp", out OnPointerUpFunc);
            RegisterPrivateMethodToDelegate("ChangeSelectedIndex", out ChangeSelectedIndexFunc);
            RegisterPrivateMethodToDelegate("GetSelectedIndex", out GetSelectedIndexFunc);
            RegisterPrivateMethodToDelegate("Hide", out HideFunc);
            
            return;


            void RegisterPrivateMethodToDelegate<TFunc>(string methodName, out TFunc func) 
                where TFunc : Delegate
            {
                using var _ = ListPool<Type>.Get(out var typeList);
                
                // TFuncの2番目以降の引数の型を取得
                // ただしFunc<T>の場合は最後の一つは返り値なので無視
                typeList.AddRange(typeof(TFunc).GetGenericArguments().Skip(1));
                if (typeof(TFunc).Name.StartsWith("Func`"))
                {
                    typeList.RemoveAt(typeList.Count - 1);
                }
                
                //TFuncの2番目以降とMethodInfoのParametersの型が一致するものを選ぶ
                var methodInfo = methodInfos.Where(m => m.Name == methodName)
                    .FirstOrDefault(m => m.GetParameters().Select(p => p.ParameterType).SequenceEqual(typeList));

                Assert.IsNotNull(methodInfo, $"Method '{methodName}' not found in GenericDropdownMenu with parameters {string.Join(", ", typeList.Select(t => t.Name))}.");
                
                func = (TFunc)methodInfo.CreateDelegate(typeof(TFunc));
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
        
        private readonly Dictionary<VisualElement, SingleMenu> _itemToSubMenuTable = new();
        private readonly VisualElement _outerContainer;
        private readonly VisualElement _scrollView;
        private KeyboardNavigationManipulator? _keyboardNavigationManipulator;

        private SingleMenu? _parentMenu;
        
        private bool IsRootMenu => _parentMenu == null;
        private SingleMenu RootMenu => _parentMenu?.RootMenu ?? this;
        private VisualElement RootMenuContainer => GetFirstAncestorByClassName(RootMenu._outerContainer, ussClassName);
        private bool IsCurrentLeafMenu => _itemToSubMenuTable.Values.All(subMenu => subMenu._parentMenu == null);
        
        
        public SingleMenu()
        {
            _outerContainer = GetFirstAncestorByClassName(contentContainer, containerOuterUssClassName);
            _scrollView = GetFirstAncestorByClassName(contentContainer, containerInnerUssClassName);
            
            _outerContainer.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            _outerContainer.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            // var myMenuContainer = GetFirstAncestorByClassName(_outerContainer, ussClassName);
            // var onAttachToPanelMethodInfo = typeof(GenericDropdownMenu).GetMethod("OnAttachToPanel", BindingFlags.NonPublic | BindingFlags.Instance);
            // var onAttachToPanelDelegate = (EventCallback<AttachToPanelEvent>)onAttachToPanelMethodInfo.CreateDelegate(typeof(EventCallback<AttachToPanelEvent>), this);
            // myMenuContainer.UnregisterCallback(onAttachToPanelDelegate);
        }

        #region Callbacks
        
        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            _keyboardNavigationManipulator = new KeyboardNavigationManipulator(OnNavigation);
            contentContainer.AddManipulator(_keyboardNavigationManipulator);
            
            _outerContainer.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _outerContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _outerContainer.RegisterCallback<PointerUpEvent>(OnPointerUp);
            
            var rootMenuContainer = RootMenuContainer;
            rootMenuContainer.RegisterCallback<PointerDownEvent>(OnPointerDownOnRoot);
            rootMenuContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);

            if (IsRootMenu)
            {
                // Unregister GenericDropdownMenu's callbacks
                var navigationManipulatorField = typeof(GenericDropdownMenu)
                    .GetField("m_NavigationManipulator", BindingFlags.NonPublic | BindingFlags.Instance);
                var navigationManipulator = navigationManipulatorField?.GetValue(this) as KeyboardNavigationManipulator;
                contentContainer.RemoveManipulator(navigationManipulator);
                
                var onFocusOutMethodInfo = typeof(GenericDropdownMenu)
                    .GetMethod("OnFocusOut", BindingFlags.NonPublic | BindingFlags.Instance);
                var onFocusOutDelegate = (EventCallback<FocusOutEvent>)onFocusOutMethodInfo?.CreateDelegate(typeof(EventCallback<FocusOutEvent>), this);
                _scrollView.UnregisterCallback(onFocusOutDelegate);
                
                rootMenuContainer.RegisterCallback<FocusInEvent>(e => Debug.Log($"Root FocusInEvent called in {e.target}"));
            }

            _scrollView.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (IsCurrentLeafMenu && GetSelectedIndexFunc(this) >= 0)
                {
                    Debug.Log("FocusOut guard called in " + contentContainer.name);
                    _scrollView.schedule.Execute(() => contentContainer.Focus());
                }
            });
            
            _scrollView.RegisterCallback<FocusInEvent>(e => Debug.Log($"FocusInEvent called in {contentContainer.name}"));
            _scrollView.RegisterCallback<FocusOutEvent>(e => Debug.Log($"FocusOutEvent called in {contentContainer.name}"));
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            contentContainer.RemoveManipulator(_keyboardNavigationManipulator);
            
            _outerContainer.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _outerContainer.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _outerContainer.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            
            var rootMenuContainer = RootMenuContainer;
            rootMenuContainer.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            rootMenuContainer.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        }

        private void OnNavigation(KeyboardNavigationOperation operation, EventBase evt)
        {
            Debug.Log("OnNavigation called with operation: " + operation);
            var eventUsed = ApplyFunc(this, operation);
            
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (operation)
            {
                // サブメニュー時、GenericDropdownMenuのApplyメソッドではルートメニューは閉じないのでここで閉じる
                case KeyboardNavigationOperation.Cancel:
                case KeyboardNavigationOperation.Submit:
                    HideRootMenu(true);
                    break;
                
                case KeyboardNavigationOperation.MoveRight:
                    // サブメニューを表示する
                    var selectedItem = GetSelectedItem();
                    if (selectedItem != null && ShowSubMenu(selectedItem, true))
                    {
                        eventUsed = true;
                    }
                    break;
                
                case KeyboardNavigationOperation.MoveLeft:
                    // サブメニューを閉じる
                    if (!IsRootMenu)
                    {
                        HideAsSubMenu();
                        _parentMenu?.contentContainer.Focus();
                        eventUsed = true;
                    }
                    break;
            }

            if (eventUsed)
            {
                evt.StopPropagation();
            }
        }
        
        private void OnPointerDown(PointerDownEvent evt)
        {
            OnPointerDownFunc(this, evt);
            HideSubMenusForUnselectedItems();
        }
        
        private void OnPointerDownOnRoot(PointerDownEvent evt)
        {
            HideFunc(this, true);
        }


        private void OnPointerMove(PointerMoveEvent evt)
        {
            OnPointerMoveFunc(this, evt);
            HideSubMenusForUnselectedItems();
        }
        
        private void OnPointerUp(PointerUpEvent evt)
        {
            var selectedItem = GetSelectedItem();
            
            // OnPointerUpFuncはSelectedItemがあればそのアクションを行ってHide()するが、
            // サブメニューアイテムが選択されている場合は閉じないで欲しいのでOnPointerUpFuncを呼ばない
            var isSelectedItemSubMenuItem = selectedItem != null && _itemToSubMenuTable.ContainsKey(selectedItem);
            if (!isSelectedItemSubMenuItem)
            {
                OnPointerUpFunc(this, evt);

                if (!IsRootMenu)
                {
                    HideRootMenu(true);
                }
            }
            
            evt.StopPropagation();
        }

        #endregion
        
        
        private void HideRootMenu(bool giveFocusBack = false)
        {
            HideFunc(RootMenu, giveFocusBack);
        }
        
        private void HideSubMenusForUnselectedItems()
        {
            var selectedItem = GetSelectedItem();
            if (selectedItem == null) return;
            
            foreach(var (item, submenu) in _itemToSubMenuTable)
            {
                if ( item != selectedItem )
                {
                    submenu.HideAsSubMenu();
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
        

        public void AddSubMenuItem(string itemName, long delayMs, SingleMenu subMenu)
        {
            AddItem(itemName, false, null);
            var item = contentContainer.Children().Last();
            
            _itemToSubMenuTable[item] = subMenu;
            
            // PointerEnterして一定時間経過後にサブメニューを表示する
            // PointerLeaveしたら時間計測をストップ
            IVisualElementScheduledItem? scheduledItem = null;

            item.RegisterCallback<PointerEnterEvent>(_ =>
            {
                scheduledItem ??= item.schedule.Execute(() => ShowSubMenu(item));
                scheduledItem.ExecuteLater(delayMs);
            });

            item.RegisterCallback<PointerLeaveEvent>(_ => { scheduledItem?.Pause(); });
        }

        private bool ShowSubMenu(VisualElement targetElement, bool selectFirstItem = false)
        {
            if (!_itemToSubMenuTable.TryGetValue(targetElement, out var submenu))
            {
                Debug.LogWarning($"No submenu found for target element: {targetElement.name}");
                return false;
            }
            
            submenu.ShowAsSubMenu(this, targetElement, selectFirstItem);
            return true;
        }

        private void ShowAsSubMenu(SingleMenu parentMenu, VisualElement targetElement, bool selectFirstItem = false)
        {
            _parentMenu = parentMenu;
            
            _outerContainer.RegisterCallbackOnce<GeometryChangedEvent>(_ => UpdateSubMenuPosition(targetElement));
            
            RootMenuContainer.Add(_outerContainer);

            contentContainer.name = "SubMenuContentContainer";
            contentContainer.schedule.Execute(() => contentContainer.Focus());

            if (selectFirstItem)
            {
                ChangeSelectedIndexFunc(this, 0, GetSelectedIndexFunc(this));
            }
        }
        
        /// <summary>
        /// RootMenuContainerローカル座標系ではみ出ないようにサブメニューの位置を調整する
        /// </summary>
        /// <param name="targetElement"></param>
        private void UpdateSubMenuPosition(VisualElement targetElement)
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
        

        private void HideAsSubMenu()
        {
            if (IsRootMenu) return;
            
            foreach(var submenu in _itemToSubMenuTable.Values)
            {
                submenu.HideAsSubMenu();
            }
            
            _outerContainer.RemoveFromHierarchy();
            _parentMenu = null;
        }
    }
}
        
