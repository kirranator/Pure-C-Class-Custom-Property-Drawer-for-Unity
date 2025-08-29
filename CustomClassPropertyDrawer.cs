using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom property drawer that allows you to select and serialize
/// subclasses of a base pure C# class (not MonoBehaviour or ScriptableObject).
/// 
/// Requirements:
/// - The target class must be marked with [Serializable].
/// - The field referencing the class must use [SerializeReference].
/// - Replace `CLASSNAMEHERE` below with the base class type you want to draw.
/// </summary>
[CustomPropertyDrawer(typeof(CLASSNAMEHERE), true)]
public class CraftActionsPropertyDrawer : PropertyDrawer
{
    // Width of the type-selection dropdown button
    const float typeButtonWidth = 160f;

    /// <summary>
    /// Calculates the height needed to render this property in the inspector.
    /// Handles foldout expansion and child property rendering.
    /// </summary>
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // Base height: just one line for the foldout & type dropdown
        float height = line;

        // If this is a managed reference, has a value, and is expanded,
        // then we need to add space for all child properties
        if (property.propertyType == SerializedPropertyType.ManagedReference &&
            property.managedReferenceValue != null &&
            property.isExpanded)
        {
            var copy = property.Copy();
            var end = copy.GetEndProperty();
            if (copy.NextVisible(true))
            {
                while (!SerializedProperty.EqualContents(copy, end))
                {
                    height += EditorGUI.GetPropertyHeight(copy, true) + spacing;
                    if (!copy.NextVisible(false)) break;
                }
            }
        }

        return height;
    }

    /// <summary>
    /// Draws the property in the inspector.
    /// Adds a foldout, type-selection dropdown, and child field rendering.
    /// </summary>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float line = EditorGUIUtility.singleLineHeight;

        // Define rects: left side for label/foldout, right side for type dropdown
        Rect dropdownRect = new Rect(position.x + position.width - typeButtonWidth, position.y, typeButtonWidth, line);
        Rect labelRect = new Rect(position.x, position.y, position.width - typeButtonWidth - 4, line);

        // Draw foldout + label
        property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);

        // Draw type dropdown (shows current type)
        string currentTypeName = GetManagedReferenceTypeName(property);
        if (GUI.Button(dropdownRect, new GUIContent(currentTypeName), EditorStyles.popup))
        {
            ShowTypeSelectionMenu(property);
        }

        // If expanded, render all child fields
        if (property.isExpanded && property.managedReferenceValue != null)
        {
            EditorGUI.indentLevel++;
            var copy = property.Copy();
            var end = copy.GetEndProperty();
            float y = position.y + line + EditorGUIUtility.standardVerticalSpacing;

            if (copy.NextVisible(true))
            {
                while (!SerializedProperty.EqualContents(copy, end))
                {
                    float h = EditorGUI.GetPropertyHeight(copy, true);
                    Rect r = new Rect(position.x, y, position.width, h);
                    EditorGUI.PropertyField(r, copy, true);
                    y += h + EditorGUIUtility.standardVerticalSpacing;

                    if (!copy.NextVisible(false)) break;
                }
            }
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    /// <summary>
    /// Builds and shows a context menu of all concrete subclasses of the base type.
    /// When a type is selected, it creates an instance and assigns it to the property.
    /// </summary>
    private void ShowTypeSelectionMenu(SerializedProperty property)
    {
        var menu = new GenericMenu();

        // Get base type of the field this drawer is attached to
        Type baseType = GetBaseType(fieldInfo);

        // Find all valid subclasses (non-abstract, non-interface)
        var types = GetAllConcreteSubclasses(baseType);

        foreach (var t in types)
        {
            bool isCurrent = property.managedReferenceValue?.GetType() == t;
            string propertyPath = property.propertyPath;
            UnityEngine.Object targetObject = property.serializedObject.targetObject;

            // Add a menu item for this type
            menu.AddItem(new GUIContent(t.Name), isCurrent, () =>
            {
                // Reacquire property inside callback (serialized object may have changed)
                var so = new SerializedObject(targetObject);
                var prop = so.FindProperty(propertyPath);
                if (prop != null)
                {
                    so.Update();
                    // Assign new instance of the chosen type
                    prop.managedReferenceValue = Activator.CreateInstance(t);
                    so.ApplyModifiedProperties();
                }
            });
        }

        menu.ShowAsContext();
    }

    /// <summary>
    /// Gets the display name of the current managed reference type.
    /// Returns "Error" if type cannot be resolved.
    /// </summary>
    private static string GetManagedReferenceTypeName(SerializedProperty property)
    {
        try
        {
            var fullname = property.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(fullname)) return "Error";

            var typeName = fullname.Split(',')[0];
            var t = Type.GetType(fullname) ??
                    AppDomain.CurrentDomain.GetAssemblies()
                        .Select(a => a.GetType(typeName))
                        .FirstOrDefault(x => x != null);

            if (t != null) return t.Name;
            return typeName.Split('.').Last();
        }
        catch
        {
            return "Error";
        }
    }

    /// <summary>
    /// Determines the base type of a serialized field.
    /// Handles arrays and generic lists correctly.
    /// </summary>
    private static Type GetBaseType(FieldInfo fieldInfo)
    {
        Type baseType = fieldInfo.FieldType;
        if (baseType.IsArray)
            return baseType.GetElementType();
        if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(List<>))
            return baseType.GetGenericArguments()[0];
        return baseType;
    }

    /// <summary>
    /// Returns all non-abstract, non-interface types that inherit from the given base type.
    /// </summary>
    private static List<Type> GetAllConcreteSubclasses(Type baseType)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException e) { return e.Types.Where(x => x != null); }
            })
            .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .OrderBy(t => t.Name)
            .ToList();
    }
}
