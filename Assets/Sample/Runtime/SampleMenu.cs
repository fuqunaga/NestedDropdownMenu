using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NestedDropdownMenuSystem.Sample.Runtime
{
    public static class SampleMenu
    {
        public static VisualElement CreateElement()
        {
            var container = new VisualElement()
            {
                style =
                {
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 10,
                    paddingBottom = 10
                }
            };
            
            var title = new Label("<b>Nested Dropdown Menu</b>")
            {
                style =
                {
                    fontSize = 20,
                    marginBottom = 10
                }
            };

            var detail = new Label("NestedDropdownMenu is Unity's GenericDropdownMenu-based hierarchical menu.\n"+
                                   "Right-click each button to display the menu.");
            
            var textField = new TextField
            {
                value = MenuCodeString,
                isReadOnly = true,
                style =
                {
                    marginTop = 10,
                    marginBottom = 10,
                }
            };
            
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginTop = 20,
                    marginBottom = 20,
                }
            };

            row.Add(CreateGenericDropdownMenuButton());
            row.Add(CreateNestedDropdownMenuButton());
            
            container.Add(title);
            container.Add(detail);
            container.Add(row);
            container.Add(new Label("<b>Source code</b>"));
            container.Add(new Label("NestedDropdownMenu has the same interface as GenericDropdownMenu\n"));
            container.Add(textField);

            return container;
        }
        
        
        public static Button CreateGenericDropdownMenuButton(
            string text = "GenericDropdownMenu")
        {
            return CreateButton(text, (evt, button) =>
            {
                ShowGenericDropdownMenu(new Rect(evt.mousePosition, Vector2.zero), button);
            });
        }
        
        public static Button CreateNestedDropdownMenuButton(
            string text = "NestedDropdownMenu")
        {
            return CreateButton(text, (evt, button) =>
            {
                ShowNestedDropdownMenu(new Rect(evt.mousePosition, Vector2.zero), button);
            });
        }
        
        private static Button CreateButton(string text, Action<MouseDownEvent, VisualElement> callback)
        {
            var button = new Button
            {
                text = text,
                style =
                {
                    width = 200f,
                    height = 50f
                }
            };
            button.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.RightMouse) callback?.Invoke(evt, button);
            });
            return button;
        }
        
        public static void ShowGenericDropdownMenu(Rect rect, VisualElement targetElement, bool anchored = false)
        {
            var menu = new GenericDropdownMenu();
            menu.AddItem("Item1", false, () => Debug.Log("Item1 clicked"));
            menu.AddItem("Item2(Checked)", true, () => Debug.Log("Item2 clicked"));
            menu.AddDisabledItem("Item3 (Disabled)", false);
            menu.AddSeparator("");
            menu.AddItem("Sub0/Item1", false, () => Debug.Log("Sub0/Item1 clicked"));
            menu.AddItem("Sub0/Item2(Checked)", false, () => Debug.Log("Sub0/Item2 clicked"));
            menu.AddDisabledItem("Sub0/Item3 (Disabled)", false);
            menu.AddSeparator("Sub0/");
            menu.AddItem("Sub0/Sub1/Item1", false, () => Debug.Log("Sub0/Sub1/Item1 clicked"));
            menu.AddItem("Sub0/Sub1/Item2(Checked)", false, () => Debug.Log("Sub0/Sub1/Item2 clicked"));
            menu.AddDisabledItem("Sub0/Sub1/Item3 (Disabled)", false);

            menu.DropDown(rect, targetElement, anchored);
        }

        public static void ShowNestedDropdownMenu(Rect rect, VisualElement targetElement, bool anchored = false)
        {
            var menu = new NestedDropdownMenu();
            menu.AddItem("Item1", false, () => Debug.Log("Item1 clicked"));
            menu.AddItem("Item2(Checked)", true, () => Debug.Log("Item2 clicked"));
            menu.AddDisabledItem("Item3 (Disabled)", false);
            menu.AddSeparator("");
            menu.AddItem("Sub0/Item1", false, () => Debug.Log("Sub0/Item1 clicked"));
            menu.AddItem("Sub0/Item2(Checked)", false, () => Debug.Log("Sub0/Item2 clicked"));
            menu.AddDisabledItem("Sub0/Item3 (Disabled)", false);
            menu.AddSeparator("Sub0/");
            menu.AddItem("Sub0/Sub1/Item1", false, () => Debug.Log("Sub0/Sub1/Item1 clicked"));
            menu.AddItem("Sub0/Sub1/Item2(Checked)", false, () => Debug.Log("Sub0/Sub1/Item2 clicked"));
            menu.AddDisabledItem("Sub0/Sub1/Item3 (Disabled)", false);

            menu.DropDown(rect, targetElement, anchored);
        }


        public const string MenuCodeString = @"menu.AddItem(""Item1"", false, () => Debug.Log(""Item1 clicked""));
menu.AddItem(""Item2(Checked)"", true, () => Debug.Log(""Item2 clicked""));
menu.AddDisabledItem(""Item3 (Disabled)"", false);
menu.AddSeparator("""");
menu.AddItem(""Sub0/Item1"", false, () => Debug.Log(""Sub0/Item1 clicked""));
menu.AddItem(""Sub0/Item2(Checked)"", false, () => Debug.Log(""Sub0/Item2 clicked""));
menu.AddDisabledItem(""Sub0/Item3 (Disabled)"", false);
menu.AddSeparator(""Sub0/"");
menu.AddItem(""Sub0/Sub1/Item1"", false, () => Debug.Log(""Sub0/Sub1/Item1 clicked""));
menu.AddItem(""Sub0/Sub1/Item2(Checked)"", false, () => Debug.Log(""Sub0/Sub1/Item2 clicked""));
menu.AddDisabledItem(""Sub0/Sub1/Item3 (Disabled)"", false);
";
    }
}