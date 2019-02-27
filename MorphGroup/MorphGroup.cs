using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin
{
    class MorphGroup : MVRScript
    {
        SuperController sc;
        public override void Init()
        {
            try
            {
                sc = SuperController.singleton;

                string pluginPath = GetPluginPath();
                string defaultSavePath = pluginPath + "/groups";

                JSONStorable geometry = containingAtom.GetStorableByID("geometry");
                DAZCharacterSelector character = geometry as DAZCharacterSelector;
                GenerateDAZMorphsControlUI morphControl = character.morphsControlUI;

                morphControl.GetMorphByDisplayName("Tongue In-Out").startValue = 0;
                morphControl.GetMorphByDisplayName("Tongue Narrow-Wide").startValue = 0;
                morphControl.GetMorphByDisplayName("Tongue Roll 1").startValue = 0;

                CreateButton("Zero All Morphs").button.onClick.AddListener(() =>
                {
                    ZeroMorphs(morphControl);
                });

                CreateButton("Save Morphs").button.onClick.AddListener(() =>
                {
                    sc.fileBrowserUI.defaultPath = defaultSavePath;
                    sc.fileBrowserUI.SetTextEntry(true);

                    sc.fileBrowserUI.Show((path) =>
                    {
                        //  cancel or invalid
                        if (string.IsNullOrEmpty(path))
                        {
                            return;
                        }

                        //  ensure extension
                        if (!path.EndsWith(".json"))
                        {
                            path += ".json";
                        }

                        sc.GetSaveJSON(containingAtom, true, true);

                        sc.SaveStringIntoFile(path, GetMorphJSON(morphControl).ToString(""));
                        SuperController.LogMessage("Wrote morph group file: " + path);
                    });
                });

                CreateButton("Apply Morphs").button.onClick.AddListener(() =>
                {
                    sc.fileBrowserUI.defaultPath = defaultSavePath;
                    sc.fileBrowserUI.SetTextEntry(false);
                    sc.fileBrowserUI.Show((path) =>
                    {
                        //  cancel or invalid
                        if (string.IsNullOrEmpty(path))
                        {
                            return;
                        }

                        //  ensure extension
                        if (!path.EndsWith(".json"))
                        {
                            path += ".json";
                        }

                        JSONClass node = JSON.Parse(sc.ReadFileIntoString(path)).AsObject;
                        SuperController.LogMessage("Read morph group file: " + path);
                        ApplyJSONMorphs(morphControl, node);
                    });
                });

                CreateButton("Load Morphs").button.onClick.AddListener(() =>
                {
                    sc.fileBrowserUI.defaultPath = defaultSavePath;
                    sc.fileBrowserUI.SetTextEntry(false);
                    sc.fileBrowserUI.Show((path) =>
                    {
                        //  cancel or invalid
                        if (string.IsNullOrEmpty(path))
                        {
                            return;
                        }

                        //  ensure extension
                        if (!path.EndsWith(".json"))
                        {
                            path += ".json";
                        }

                        JSONClass node = JSON.Parse(sc.ReadFileIntoString(path)).AsObject;

                        SuperController.LogMessage("Read morph group file: " + path);
                        ZeroMorphs(morphControl);
                        ApplyJSONMorphs(morphControl, node);
                    });
                });
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        JSONClass GetMorphJSON(GenerateDAZMorphsControlUI morphControl)
        {
            JSONClass node = new JSONClass();
            node["morphs"] = new JSONArray();

            morphControl.GetMorphDisplayNames().ForEach((name) =>
            {
                DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                if(morph.morphValue != morph.startValue && morph.isPoseControl)
                {
                    JSONClass morphNode = new JSONClass();
                    morphNode["name"] = morph.displayName;
                    morphNode["value"] = morph.morphValue.ToString();
                    node["morphs"].Add(morphNode);
                }
            });

            return node;
        }

        void ApplyJSONMorphs(GenerateDAZMorphsControlUI morphControl, JSONClass node)
        {
            JSONArray morphs = node["morphs"].AsArray;
            for(int i = 0; i < morphs.Count; i++)
            {
                JSONClass morphNode = morphs[i].AsObject;
                string name = morphNode["name"].Value;
                float value = morphNode["value"].AsFloat;
                DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                if (morph == null)
                {
                    SuperController.LogError("No morph named " + name);
                    continue;
                }
                if (morph.isPoseControl)
                {
                    morph.SetValue(value);
                    morph.SyncJSON();
                }
            }
        }

        void ZeroMorphs(GenerateDAZMorphsControlUI morphControl)
        {
            morphControl.GetMorphDisplayNames().ForEach((name) =>
            {
                DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                if (morph.isPoseControl)
                {
                    morph.SetValue(morph.startValue);
                    morph.SyncJSON();
                }
            });
        }


        string GetPluginPath()
        {
            string pluginId = this.storeId.Split('_')[0];
            MVRPluginManager manager = containingAtom.GetStorableByID("PluginManager") as MVRPluginManager;
            string pathToScriptFile = manager.GetJSON(true, true)["plugins"][pluginId].Value;
            string pathToScriptFolder = pathToScriptFile.Substring(0, pathToScriptFile.LastIndexOfAny(new char[] { '/', '\\' }));
            return pathToScriptFile;
        }
    }
}
