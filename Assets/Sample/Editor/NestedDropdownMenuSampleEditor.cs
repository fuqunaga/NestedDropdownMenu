using NestedDropdownMenuSystem.Sample.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NestedDropdownMenuSystem.Sample.Editor
{
    public class NestedDropdownMenuSampleEditor : EditorWindow
    {
        [MenuItem("NestedDropdownMenu/Sample")]
        public static void ShowWindow()
        {
            GetWindow<NestedDropdownMenuSampleEditor>("Nested Dropdown Menu Sample");
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.Add(SampleMenu.CreateGenericDropdownMenuButton());
            root.Add(SampleMenu.CreateNestedDropdownMenuButton());
        }
    }
}