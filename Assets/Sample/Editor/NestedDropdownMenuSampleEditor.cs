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
            GetWindow<NestedDropdownMenuSampleEditor>("Nested Dropdown Menu Sample");
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/NestedDropdownMenu/NestedDropdownMenu.uss"));
            root.Add(SampleMenu.CreateGenericDropdownMenuButton());
            root.Add(SampleMenu.CreateNestedDropdownMenuButton());
        }
    }
}