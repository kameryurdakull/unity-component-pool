using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Pooling.Editor
{
    [CustomEditor(typeof(PoolCatalog))]
    internal sealed class PoolCatalogEditor : UnityEditor.Editor
    {
        private const string CatalogFilter = "t:PoolCatalog";
        private const string ReservedName = "None";
        private const string GeneratedDirectory = "Assets/PoolService/Generated";
        private const string GeneratedSourcePath = GeneratedDirectory + "/PoolId.g.cs";
        private const string GeneratedAssemblyPath = GeneratedDirectory + "/PoolService.Generated.asmdef";
        private const string GeneratedAssemblySource =
            "{\n  \"name\": \"PoolService.Generated\",\n  \"rootNamespace\": \"Pooling\",\n" +
            "  \"references\": [\"PoolService\"],\n  \"autoReferenced\": true\n}\n";
        private static readonly Regex IdentifierPattern =
            new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while"
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ResolveDraggedPrefabs();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Names must be unique C# identifiers. Generate PoolId after changing any catalog. " +
                "The enum is written to Assets/PoolService/Generated and uses all PoolCatalog assets.",
                MessageType.Info);

            if (GUILayout.Button("Generate PoolId enum"))
            {
                if (TryGenerate(out var error))
                {
                    Debug.Log("PoolId enum generated from all PoolCatalog assets.");
                }
                else
                {
                    EditorUtility.DisplayDialog("Pool Catalog", error, "OK");
                }
            }
        }

        private void ResolveDraggedPrefabs()
        {
            var entries = serializedObject.FindProperty("entries");

            for (var index = 0; index < entries.arraySize; index++)
            {
                var prefab = entries.GetArrayElementAtIndex(index).FindPropertyRelative("prefab");
                var selected = prefab.objectReferenceValue as Component;
                if (selected == null || selected is IPoolable) continue;

                var resolved = PoolCatalog.Entry.ResolvePoolable(selected);
                if (resolved != null)
                {
                    prefab.objectReferenceValue = resolved;
                }
            }
        }

        private static bool TryGenerate(out string error)
        {
            var names = new SortedSet<string>(StringComparer.Ordinal);
            var keys = new Dictionary<int, string>();
            var guids = AssetDatabase.FindAssets(CatalogFilter);

            for (var assetIndex = 0; assetIndex < guids.Length; assetIndex++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[assetIndex]);
                var catalog = AssetDatabase.LoadAssetAtPath<PoolCatalog>(path);
                if (catalog == null) continue;

                var localNames = new HashSet<string>(StringComparer.Ordinal);
                var entries = catalog.Entries;

                for (var entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    var entry = entries[entryIndex];
                    if (entry == null || string.IsNullOrEmpty(entry.Name) ||
                        !IdentifierPattern.IsMatch(entry.Name) || entry.Name == ReservedName)
                    {
                        error = $"{path}: entry {entryIndex} needs a valid name other than None.";
                        return false;
                    }

                    if (entry.Prefab == null || entry.Prefab is not IPoolable || entry.PrewarmCount < 0)
                    {
                        error = $"{path}: '{entry.Name}' needs a prefab with exactly one IPoolable " +
                                "component and a nonnegative prewarm count. If it has several, " +
                                "select the intended script component directly.";
                        return false;
                    }

                    if (!localNames.Add(entry.Name))
                    {
                        error = $"{path}: '{entry.Name}' occurs more than once.";
                        return false;
                    }

                    var key = PoolKey.FromName(entry.Name);
                    if (key == 0)
                    {
                        error = $"{path}: '{entry.Name}' resolves to reserved pool key 0.";
                        return false;
                    }

                    if (keys.TryGetValue(key, out var otherName) && otherName != entry.Name)
                    {
                        error = $"{path}: '{entry.Name}' has a pool key collision with '{otherName}'.";
                        return false;
                    }

                    keys[key] = entry.Name;
                    names.Add(entry.Name);
                }
            }

            var source = new StringBuilder(128 + names.Count * 32);
            source.AppendLine("// Generated by the Pool Catalog inspector. Do not edit manually.");
            source.AppendLine("using UnityEngine;");
            source.AppendLine();
            source.AppendLine("namespace Pooling");
            source.AppendLine("{");
            source.AppendLine("    public enum PoolId");
            source.AppendLine("    {");
            source.AppendLine("        None = 0,");

            foreach (var name in names)
            {
                source.Append("        ");
                if (Keywords.Contains(name)) source.Append('@');
                source.Append(name).Append(" = ").Append(PoolKey.FromName(name)).AppendLine(",");
            }

            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    public static class PoolIdExtensions");
            source.AppendLine("    {");
            source.AppendLine("        public static T Spawn<T>(this IPoolService service, PoolId id,");
            source.AppendLine("            Vector3 position, Quaternion rotation, Transform parent = null)");
            source.AppendLine("            where T : Component, IPoolable");
            source.AppendLine("        {");
            source.AppendLine("            return service.Spawn<T>((int)id, position, rotation, parent);");
            source.AppendLine("        }");
            source.AppendLine("    }");
            source.AppendLine("}");

            var newSource = source.ToString();
            Directory.CreateDirectory(GeneratedDirectory);
            var changed = WriteIfChanged(GeneratedAssemblyPath, GeneratedAssemblySource);
            changed |= WriteIfChanged(GeneratedSourcePath, newSource);
            if (changed)
            {
                AssetDatabase.Refresh();
            }

            error = null;
            return true;
        }

        private static bool WriteIfChanged(string path, string content)
        {
            if (File.Exists(path) && File.ReadAllText(path) == content) return false;
            File.WriteAllText(path, content, new UTF8Encoding(false));
            return true;
        }
    }
}
