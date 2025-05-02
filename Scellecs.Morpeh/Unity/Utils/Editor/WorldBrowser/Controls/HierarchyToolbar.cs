#if UNITY_EDITOR
using UnityEngine.UIElements;
using HierarchyModel = Scellecs.Morpeh.Utils.Editor.Hierarchy;
namespace Scellecs.Morpeh.Utils.Editor {
    internal sealed class HierarchyToolbar : VisualElement {
        private const string TOOLBAR = "hierarchy-toolbar";
        private const string DROPDOWN = "hierarchy-world-selector-dropdown";
        private const string WORLDS_LABEL = "hierarchy-world-selector-dropdown-world-label";

        private readonly HierarchyModel model;
        private readonly VisualElement dropdownButton;
        private readonly Label entitiesLabel;
        private readonly Label worldsLabel;

        private bool isExpanded;

        internal HierarchyToolbar(HierarchyModel model) {
            this.model = model;
            this.AddToClassList(TOOLBAR);

            this.dropdownButton = new VisualElement();
            this.dropdownButton.AddToClassList(DROPDOWN);

            this.entitiesLabel = new Label();
            this.dropdownButton.Add(entitiesLabel);

            this.Add(this.dropdownButton);
        }

        internal void Update() {
            this.entitiesLabel.text = $"Entities Found: {this.model.GetTotalEntitiesFound()}";
        }
    }
}
#endif