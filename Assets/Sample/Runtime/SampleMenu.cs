using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NestedDropdownMenuSystem.Sample.Runtime
{
    public static class SampleMenu
    {
        public static Button CreateGenericDropdownMenuButton(
            string text = "Right click to open [Unity's GenericDropdownMenu]")
        {
            return CreateButton(text, (evt, button) =>
            {
                ShowGenericDropdownMenu(new Rect(evt.mousePosition, Vector2.zero), button);
            });
        }
        
        public static Button CreateNestedDropdownMenuButton(
            string text = "Right click to open [NestedDropdownMenu]")
        {
            return CreateButton(text, (evt, button) =>
            {
                ShowNestedDropdownMenu(new Rect(evt.mousePosition, Vector2.zero), button);
            });
        }
        
        private static Button CreateButton(string text, Action<MouseDownEvent, VisualElement> callback)
        {
            var button = new Button { text = text };
            button.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.RightMouse) callback?.Invoke(evt, button);
            });
            return button;
        }
        
        public static void ShowGenericDropdownMenu(Rect rect, VisualElement targetElement, bool anchored = false)
        {
            var menu = new GenericDropdownMenu();
            menu.AddItem("Item 1", false, () => Debug.Log("Item 1 clicked"));
            menu.AddItem("Item 2(Checked)", true, () => Debug.Log("Item 2 clicked"));
            menu.AddSeparator("");
            menu.AddDisabledItem("Item3 (Disabled)", false);
            menu.AddSeparator("");
            menu.AddItem("Sub/Item 1", false, () => Debug.Log("Sub Item 1 clicked"));
            menu.AddItem("Sub/Item 2", false, () => Debug.Log("Sub Item 2 clicked"));

            menu.DropDown(rect, targetElement, anchored);
        }

        public static void ShowNestedDropdownMenu(Rect rect, VisualElement targetElement, bool anchored = false)
        {
            var menu = new NestedDropdownMenu();
            menu.AddItem("Item 1", false, () => Debug.Log("Item 1 clicked"));
            menu.AddItem("Item 2(Checked)", true, () => Debug.Log("Item 2 clicked"));
            menu.AddSeparator("");
            menu.AddDisabledItem("Item3 (Disabled)", false);
            menu.AddSeparator("");
            menu.AddItem("Sub/Item 1", false, () => Debug.Log("Sub Item 1 clicked"));
            menu.AddItem("Sub/Item 2(Checked)", false, () => Debug.Log("Sub Item 2 clicked"));
            menu.AddSeparator("Sub/");
            menu.AddDisabledItem("Sub/Item3 (Disabled)", false);
            menu.AddSeparator("Sub/");
            menu.AddItem("Sub/Sub/Item 1", false, () => Debug.Log("Sub/Sub Item 1 clicked"));
            menu.AddItem("Sub/Sub/Item 2", false, () => Debug.Log("Sub/Sub Item 2 clicked"));
            menu.AddSeparator("Sub/Sub/");
            menu.AddItem("Sub/Sub/Sub/Item 1", false, () => Debug.Log("Sub/Sub/Sub Item 1 clicked"));
            menu.AddItem("Sub/Sub/Sub/Item 2", false, () => Debug.Log("Sub/Sub/Sub Item 2 clicked"));
            
            

            menu.DropDown(rect, targetElement, anchored);
        }
    }
}