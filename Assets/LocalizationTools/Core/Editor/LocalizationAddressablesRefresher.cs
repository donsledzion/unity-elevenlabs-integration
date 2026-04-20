using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

namespace ElevenLabs.Editor
{
    /// <summary>
    /// Editor window to refresh Addressable references in Localization Asset Tables.
    /// Helps fix issues when assets were assigned to tables without being marked as Addressables.
    /// </summary>
    public class LocalizationAddressablesRefresher : EditorWindow
    {
        private AssetTableCollection _collection;
        private bool _forceRefreshAll = false;

        [MenuItem("Tools/Localization/Asset Addressables Refresher")]
        public static void ShowWindow()
        {
            var window = GetWindow<LocalizationAddressablesRefresher>("Asset Addressables Refresher");
            var icon = EditorGUIUtility.IconContent("d_AddressableAssetSettings Icon").image as Texture2D;
            window.titleContent = new GUIContent("Asset Refresher", icon);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Localization Asset Refresher", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This tool ensures that assets in the selected collection are correctly registered as Addressables (if the collection is configured to do so).", MessageType.Info);
            
            EditorGUILayout.Space();

            _collection = (AssetTableCollection)EditorGUILayout.ObjectField("Table Collection", _collection, typeof(AssetTableCollection), false);
            _forceRefreshAll = EditorGUILayout.Toggle(new GUIContent("Force Refresh All", "If checked, every entry will be re-registered even if it seems correct."), _forceRefreshAll);

            EditorGUILayout.Space();

            GUI.enabled = _collection != null;
            if (GUILayout.Button("Refresh Addressables", GUILayout.Height(30)))
            {
                RefreshCollection();
            }
            GUI.enabled = true;

            if (_collection == null)
            {
                EditorGUILayout.HelpBox("Please select an Asset Table Collection to continue.", MessageType.Warning);
            }
        }

        private void RefreshCollection()
        {
            var sharedData = _collection.SharedData;
            var tables = _collection.AssetTables;
            var totalEntries = sharedData.Entries.Count * tables.Count;
            var processedCount = 0;
            var fixedCount = 0;

            try
            {
                for (var i = 0; i < sharedData.Entries.Count; i++)
                {
                    var sharedEntry = sharedData.Entries[i];
                    var key = sharedEntry.Key;

                    foreach (var table in tables)
                    {
                        processedCount++;
                        var progress = (float)processedCount / totalEntries;
                        if (EditorUtility.DisplayCancelableProgressBar("Refreshing Addressables", $"Processing key: {key} in {table.name}", progress))
                        {
                            Debug.Log("Operation cancelled by user.");
                            return;
                        }

                        var entry = table.GetEntry(sharedEntry.Id);
                        
                        // Decide if we should refresh
                        var shouldRefresh = _forceRefreshAll;
                        
                        if (!shouldRefresh && entry != null)
                        {
                            // If it's not a GUID-based reference (AssetReference), it might be an old direct reference
                            if (string.IsNullOrEmpty(entry.Guid))
                            {
                                shouldRefresh = true;
                            }
                        }

                        if (shouldRefresh || entry == null)
                        {
                            var tableEntry = table.GetEntry(sharedEntry.Id);
                            if (tableEntry != null && !string.IsNullOrEmpty(tableEntry.Guid))
                            {
                                var path = AssetDatabase.GUIDToAssetPath(tableEntry.Guid);
                                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                                
                                if (asset != null)
                                {
                                    // Professional approach: call AddAssetToTable which handles Addressables registration
                                    _collection.AddAssetToTable(table, key, asset);
                                    fixedCount++;
                                }
                            }
                        }
                    }
                }

                AssetDatabase.SaveAssets();
                Debug.Log($"<color=green>Localization Refresh Completed!</color> Processed {processedCount} entries, updated {fixedCount} bindings in collection '{_collection.TableCollectionName}'.");
                EditorUtility.DisplayDialog("Localization Refresh", $"Successfully updated {fixedCount} asset bindings in collection '{_collection.TableCollectionName}'.", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.SetDirty(_collection);
                foreach (var table in tables)
                {
                    EditorUtility.SetDirty(table);
                }
            }
        }
    }
}
