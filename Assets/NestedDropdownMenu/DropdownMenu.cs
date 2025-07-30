using System;
using System.Linq;
using UnityEngine.UIElements;

namespace NestedDropdownMenuSystem
{
    /// <summary>
    /// Enhanced GenericDropdownMenu with support for hover-delayed submenu popup functionality.
    /// </summary>
    public class GenericMenu : GenericDropdownMenu
    {
        public void AddSubmenuItem(string itemName, long delayMs, Action<VisualElement> showSubmenuAction)
        {
            AddItem(itemName, false, null);
            var item = contentContainer.Children().Last();
            
            // PointerEnterして一定時間経過後にサブメニューを表示する
            // PointerLeaveしたら時間計測をストップ
            IVisualElementScheduledItem scheduledItem = null;

            item.RegisterCallback<PointerEnterEvent>(_ =>
            {
                scheduledItem ??= item.schedule.Execute(() => showSubmenuAction?.Invoke(item));
                scheduledItem.ExecuteLater(delayMs);
            });
            
            item.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                scheduledItem?.Pause();
            });
        }
    }
}