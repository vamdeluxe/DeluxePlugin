using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SimpleJSON;
using MeshVR;

namespace DeluxePlugin
{
    public class PresetRandomizer : MVRScript
    {
        JSONStorableBool randomizeMorphs;
        JSONStorableBool randomizeHair;
        JSONStorableBool randomizeSkin;

        public override void Init()
        {
            try
            {
                randomizeMorphs = new JSONStorableBool("Randomize Morph Preset", true);
                CreateToggle(randomizeMorphs);
                RegisterBool(randomizeMorphs);
                randomizeHair = new JSONStorableBool("Randomize Hair Preset", true);
                CreateToggle(randomizeHair);
                RegisterBool(randomizeHair);
                randomizeSkin = new JSONStorableBool("Randomize Skin Preset", true);
                CreateToggle(randomizeSkin);
                RegisterBool(randomizeSkin);

                CreateButton("Randomize").button.onClick.AddListener(() =>
                {
                    List<string> morphPresets = GetPresets("Morphs");
                    List<string> hairPresets = GetPresets("Hair");
                    List<string> skinPresets = GetPresets("Skin");

                    if (randomizeMorphs.val) {
                        LoadPreset("MorphPresets", RandomFromList(morphPresets));
                    }

                    if (randomizeHair.val)
                    {
                        LoadPreset("HairPresets", RandomFromList(hairPresets));
                    }

                    if (randomizeSkin.val)
                    {
                        LoadPreset("SkinPresets", RandomFromList(skinPresets));
                    }
                });
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void LoadPreset(string presetType, string path)
        {
            PresetManager pm = containingAtom.GetComponentInChildren<PresetManager>(includeInactive: true);
            pm.itemType = PresetManager.ItemType.Atom;

            JSONStorable js = containingAtom.GetStorableByID(presetType);
            JSONStorableUrl presetPathJSON = js.GetUrlJSONParam("presetBrowsePath");
            presetPathJSON.val = SuperController.singleton.NormalizePath(path);
            js.CallAction("LoadPreset");
        }

        string RandomFromList(List<string> list)
        {
            int index = UnityEngine.Random.Range(0, list.Count);
            return list[index];
        }

        List<string> GetPresets(string storeName)
        {
            PresetManager pm = containingAtom.GetComponentInChildren<PresetManager>(includeInactive: true);
            pm.itemType = PresetManager.ItemType.Atom;
            pm.creatorName = null;
            pm.storeFolderName = storeName;
            pm.storeName = storeName;

            List<string> list = new List<string>();

            string storeFolderPath = pm.GetStoreFolderPath();
            if (String.IsNullOrEmpty(storeFolderPath)==false && String.IsNullOrEmpty(storeName)==false)
            {
                list = GetFilesAtPathRecursive(storeFolderPath, "*.vap");
            }

            return list;
        }

        List<string> GetFilesAtPathRecursive(string path, string pattern)
        {
            List<string> combined = new List<string>();
            string[] files = SuperController.singleton.GetFilesAtPath(path, pattern);
            string[] directories = SuperController.singleton.GetDirectoriesAtPath(path);

            files.ToList().ForEach(file =>
            {
                combined.Add(file);
            });

            directories.ToList().ForEach(directory =>
            {
                combined.AddRange(GetFilesAtPathRecursive(directory, pattern));
            });

            return combined;
        }
    }
}