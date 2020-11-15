using System;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(MeshRenderer)), CanEditMultipleObjects]
public class awdhawi : OdinEditor
{
    private MeshRenderer[] meshRenderers = null!;
    private MeshRenderer firstMeshRenderer = null!;
    
    protected override void OnEnable()
    {
        base.OnEnable();

        meshRenderers = targets.Cast<MeshRenderer>().ToArray();

        firstMeshRenderer = meshRenderers.First();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        EditorGUILayout.Space();

        var sortingLayerIsEqual = true;
        var sortingOrderIsEqual = true;

        foreach (var meshRenderer in meshRenderers)
        {
            if (meshRenderer.sortingLayerID != firstMeshRenderer.sortingLayerID) sortingLayerIsEqual = false;
            if (meshRenderer.sortingOrder != firstMeshRenderer.sortingOrder) sortingOrderIsEqual = false;
        }

        // Sorting Layer
        EditorGUI.showMixedValue = !sortingLayerIsEqual;

        var results = OdinSelector<ValueDropdownItem<int>>.DrawSelectorDropdown( 
            new GUIContent("Sorting Layer"), 
            firstMeshRenderer.sortingLayerName, 
            DoSelector 
        );

        if (results != null)
        {
            var newSortingLayerId = results.First().Value;

            Undo.RecordObjects(meshRenderers, "Change sorting layer");
            foreach (var meshRenderer in meshRenderers)
            {
                meshRenderer.sortingLayerID = newSortingLayerId;
            }
        }
        
        
        // Sorting Order
        EditorGUI.BeginChangeCheck();

        EditorGUI.showMixedValue = !sortingOrderIsEqual;
        var newSortingOrder = EditorGUILayout.IntField("Order in Layer", firstMeshRenderer.sortingOrder);

        if (EditorGUI.EndChangeCheck()) 
        {
            Undo.RecordObjects(meshRenderers, "Change sorting order");
            foreach(var meshRenderer in meshRenderers)
            {
                meshRenderer.sortingOrder = newSortingOrder;
            }
        }
    }
    
    private OdinSelector<ValueDropdownItem<int>> DoSelector(Rect buttonRect)
    {
        var dropdownValues = SortingLayer.layers.Select(sortingLayer => new ValueDropdownItem<int>(sortingLayer.name, sortingLayer.id));
        
        var genericSelector = new GenericSelector<ValueDropdownItem<int>>(dropdownValues);
            
        buttonRect.xMax = GUIHelper.GetCurrentLayoutRect().xMax;

        genericSelector.EnableSingleClickToSelect();
        genericSelector.ShowInPopup( buttonRect );
            
        return genericSelector;
    }
}

