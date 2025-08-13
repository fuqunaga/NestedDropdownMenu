using NestedDropdownMenuSystem.Sample.Runtime;
using UnityEditor;
using UnityEngine.UIElements;

namespace NestedDropdownMenuSystem.Sample.Editor
{
    public class NestedDropdownMenuSampleEditor : EditorWindow
    {
        [MenuItem("NestedDropdownMenu/Sample")]
        public static void ShowWindow()
        {
            var window = GetWindow<NestedDropdownMenuSampleEditor>("Nested Dropdown Menu Sample");
            var pos = window.position;
            pos.width = 600;
            pos.height = 400;
            window.position = pos;
        }
        
        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/ga.fuquna.nested-dropdown-menu/Runtime/NestedDropdownMenu.uss"));
            root.Add(SampleMenu.CreateElement());
        }
    }
}