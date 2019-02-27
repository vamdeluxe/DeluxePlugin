using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
namespace DeluxePlugin.DollMaker
{
    public class Blend : BaseModule
    {
        List<JSONClass> appearances = new List<JSONClass>();
        List<UIDynamicSlider> sliders = new List<UIDynamicSlider>();
        Dictionary<JSONClass, Dictionary<string, float>> computedWeightedMorphs = new Dictionary<JSONClass, Dictionary<string, float>>();

        GridLayoutGroup appearancesLayout;

        GenerateDAZMorphsControlUI morphControl;

        public Blend(DollMaker dm) : base(dm)
        {
            dm.mainControls.RegisterTab("Blend", moduleUI);
            CreateModuleButton("Add Appearances").button.onClick.AddListener(() =>
            {
                SuperController.singleton.editModeToggle.isOn = true;
                SuperController.singleton.ShowMainHUD();

                SuperController.singleton.GetDirectoryPathDialog((string dir) =>
                {
                    if (dir == null || !(dir != string.Empty))
                    {
                        return;
                    }

                    //  have load dialog work both inside and outside folder
                    try
                    {
                        PerformLoadOnPath(dir);
                    }
                    catch
                    {
                        string folderName = "\\" + dir.Substring(dir.LastIndexOf('\\') + 1) + "\\";
                        dir = dir.Replace(folderName, "\\");
                        PerformLoadOnPath(dir);
                    }

                }, SuperController.singleton.savesDir + "Person" + "\\appearance");
            });

            appearancesLayout = ui.CreateGridLayout(1000, 500, moduleUI.transform);
            appearancesLayout.transform.localPosition = new Vector3(0, -600, 0);
            appearancesLayout.GetComponent<RectTransform>().pivot = new Vector2(0, 0);
            appearancesLayout.childAlignment = TextAnchor.UpperLeft;
            appearancesLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            appearancesLayout.constraintCount = 3;
            appearancesLayout.cellSize = new Vector2(Mathf.FloorToInt(1000 / 3), 80);

            SuperController.singleton.currentSaveDir = SuperController.singleton.currentLoadDir;
            JSONClass initialAppearance = SuperController.singleton.GetSaveJSON(atom, false, true).AsObject["atoms"].AsArray[0].AsObject;

            AddAppearance(initialAppearance, "Current", 1);

            CreateModuleButton("Clear List").button.onClick.AddListener(() =>
            {
                sliders.ForEach((slider) =>
                {
                    GameObject.Destroy(slider.gameObject);
                });
                sliders.Clear();
                appearances.Clear();
                computedWeightedMorphs.Clear();

                initialAppearance = SuperController.singleton.GetSaveJSON(atom, false, true).AsObject["atoms"].AsArray[0].AsObject;
                AddAppearance(initialAppearance, "Current", 1);
            });

            CreateModuleButton("Randomize").button.onClick.AddListener(() =>
            {
                sliders.ForEach((slider) =>
                {
                    slider.slider.value = UnityEngine.Random.Range(0.0f, 1.0f);
                });
            });

            CreateModuleButton("Average").button.onClick.AddListener(() =>
            {
                sliders.ForEach((slider) =>
                {
                    slider.slider.value = 0.5f;
                });
            });


            JSONStorable geometry = atom.GetStorableByID("geometry");
            DAZCharacterSelector character = geometry as DAZCharacterSelector;
            morphControl = character.morphsControlUI;
        }

        void PerformLoadOnPath(string dir)
        {
            List<string> files = SuperController.singleton.GetFilesAtPath(dir, "*.json").ToList();
            files.ForEach((file) =>
            {
                JSONClass person = ReadAppearance(file);
                if (person == null)
                {
                    return;
                }

                AddAppearance(person, PathExt.GetFileNameWithoutExtension(file));
            });
        }

        JSONClass ReadAppearance(string path)
        {
            JSONClass scene = JSON.Parse(SuperController.singleton.ReadFileIntoString(path)).AsObject;
            JSONClass atom = scene["atoms"].AsArray[0].AsObject;
            if (atom["type"].Value == "Person")
            {
                return atom;
            }
            return null;
        }

        void AddAppearance(JSONClass person, string fileName, float initialWeight = 0)
        {
            appearances.Add(person);

            UIDynamicSlider slider = ui.CreateMorphSlider(fileName, 500, 80, appearancesLayout.transform);
            sliders.Add(slider);

            slider.slider.minValue = 0;
            slider.slider.maxValue = 1;
            slider.slider.value = initialWeight;

            slider.slider.onValueChanged.AddListener((float value) =>
            {
                computedWeightedMorphs[person] = ComputeWeightedMorphsForPerson(person, value);

                ResetMorphs();
                AverageMorphs();
            });

            computedWeightedMorphs[person] = ComputeWeightedMorphsForPerson(person, initialWeight);
        }

        void ResetMorphs()
        {
            morphControl.morphBank1.morphs.ForEach((morph) =>
            {
                morph.SetValue(morph.startValue);
            });

            morphControl.morphBank2.morphs.ForEach((morph) =>
            {
                morph.SetValue(morph.startValue);
            });

        }

        void AverageMorphs()
        {
            float computedTotalWeight = totalWeight;

            if (computedTotalWeight <= 0.001f)
            {
                computedTotalWeight = 0.001f;
            }

            if (computedWeightedMorphs.Keys.Count <= 1)
            {
                computedTotalWeight = 1;
            }


            Dictionary<string, float> weightList = new Dictionary<string, float>();

            computedWeightedMorphs.Values.ToList().ForEach((morphWeights) =>
            {
                morphWeights.ToList().ForEach((morphAndWeight) =>
                {
                    string morphName = morphAndWeight.Key;
                    float morphValue = morphAndWeight.Value;
                    if (weightList.ContainsKey(morphName) == false)
                    {
                        weightList[morphName] = morphValue * 1.0f / computedTotalWeight;
                    }
                    else
                    {
                        weightList[morphName] += morphValue * 1.0f / computedTotalWeight;
                    }
                });
            });

            weightList.ToList().ForEach((morphAndWeight) =>
            {
                string morphName = morphAndWeight.Key;
                float morphValue = morphAndWeight.Value;
                DAZMorph morph = morphControl.GetMorphByDisplayName(morphName);
                if (morph != null && morph.isPoseControl == false)
                {
                    morph.SetValue(morphValue);
                }
            });
        }

        Dictionary<string, float> ComputeWeightedMorphsForPerson(JSONClass person, float weight)
        {
            Dictionary<string, float> weightedMorphs = new Dictionary<string, float>();
            JSONArray morphs = FindMorphsArray(person);
            for (int i = 0; i < morphs.Count; i++)
            {
                JSONClass node = morphs[i].AsObject;
                string morphName = node["name"].Value;
                float morphValue = node["value"].AsFloat;
                weightedMorphs[morphName] = morphValue * weight;
            }
            return weightedMorphs;
        }

        JSONArray FindMorphsArray(JSONClass person)
        {
            JSONArray storables = person["storables"].AsArray;
            for (int i = 0; i < storables.Count; i++)
            {
                JSONClass storable = storables[i].AsObject;
                string id = storable["id"];
                if (id == "geometry")
                {
                    return storable["morphs"].AsArray;
                }
            }
            return null;
        }

        float totalWeight
        {
            get
            {
                return sliders.Sum((slider) =>
                {
                    return slider.slider.value;
                });
            }
        }
    }
}
