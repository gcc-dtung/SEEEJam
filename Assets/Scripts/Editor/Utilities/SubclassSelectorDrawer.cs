#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
public class SubclassSelectorDrawer : PropertyDrawer
{
    // Cache: baseType -> danh sách class con
    private static readonly Dictionary<Type, List<Type>> _typeCache = new Dictionary<Type, List<Type>>();
    private static readonly Dictionary<Type, string[]> _typeNameCache = new Dictionary<Type, string[]>();

    // Cache toàn bộ type trong project (chỉ quét 1 lần duy nhất, không phải mỗi baseType)
    private static Type[] _allTypesCache;

    private static Type[] GetAllTypes()
    {
        if (_allTypesCache == null)
        {
            _allTypesCache = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .ToArray();
        }
        return _allTypesCache;
    }

    private static List<Type> GetSubclasses(Type baseType)
    {
        if (!_typeCache.TryGetValue(baseType, out var list))
        {
            list = GetAllTypes()
                .Where(t => t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t))
                .ToList();
            _typeCache[baseType] = list;
        }
        return list;
    }

    private static string[] GetTypeNames(Type baseType, List<Type> subclasses)
    {
        if (!_typeNameCache.TryGetValue(baseType, out var names))
        {
            names = new string[] { "None (Delete)" }
                .Concat(subclasses.Select(t => t.Name))
                .ToArray();
            _typeNameCache[baseType] = names;
        }
        return names;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.ManagedReference)
        {
            Rect popupRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            string typeName = property.managedReferenceFieldTypename;
            string[] parts = typeName.Split(' ');
            Type baseType = null;
            if (parts.Length == 2)
            {
                baseType = Type.GetType($"{parts[1]}, {parts[0]}");
            }

            if (baseType != null)
            {
                List<Type> types = GetSubclasses(baseType);
                string[] typeNames = GetTypeNames(baseType, types);

                string currentTypeName = property.managedReferenceValue != null
                    ? property.managedReferenceValue.GetType().Name
                    : "None (Delete)";
                int currentIndex = Array.IndexOf(typeNames, currentTypeName);
                if (currentIndex < 0) currentIndex = 0;

                int newIndex = EditorGUI.Popup(popupRect, label.text, currentIndex, typeNames);

                if (newIndex != currentIndex)
                {
                    property.managedReferenceValue = newIndex == 0
                        ? null
                        : Activator.CreateInstance(types[newIndex - 1]);
                }

                if (property.managedReferenceValue != null)
                {
                    EditorGUI.PropertyField(position, property, GUIContent.none, true);
                }
                return;
            }
        }
        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
#endif