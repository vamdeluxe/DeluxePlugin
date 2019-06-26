using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using MeshVR;

namespace DeluxePlugin.DollMaker
{
    class Appearance : BaseModule
    {
        MorphSearch morphSearch;
        List<BoneAttachedUI> baUI = new List<BoneAttachedUI>();

        public Appearance(DollMaker dm) : base(dm)
        {
            dm.mainControls.RegisterTab("Morphs", moduleUI, this);

            morphSearch = new MorphSearch(this);

            JSONArray bodyUI = DollMaker.CONFIG_JSON["bodyUI"].AsArray;
            for(int i=0;i< bodyUI.Count; i++)
            {
                JSONClass uiPart = bodyUI[i].AsObject;
                string title = uiPart["title"].Value;
                string boneName = uiPart["bone"].Value;
                string searchTerm = uiPart["search"].Value;
                float x = uiPart["offset"]["x"].AsFloat;
                float y = uiPart["offset"]["y"].AsFloat;
                float z = uiPart["offset"]["z"].AsFloat;

                DAZBone bone = atom.GetStorableByID(boneName) as DAZBone;

                BoneAttachedUI boneUI = new BoneAttachedUI(title, bone, ui, new Vector3(x, y, z), moduleUI);
                boneUI.button.button.onClick.AddListener(() =>
                {
                    morphSearch.searchBox.text = searchTerm;
                });
                baUI.Add(boneUI);
            }

            UIDynamicButton loadMorphsButton = CreateModuleButton("Load Morph Preset");
            loadMorphsButton.button.onClick.AddListener(() =>
            {
                SuperController.singleton.editModeToggle.isOn = true;
                SuperController.singleton.ShowMainHUD();
                PresetManager pm = atom.GetComponentInChildren<PresetManager>(includeInactive: true);
                PresetManagerControlUI pmcui = atom.GetComponentInChildren<PresetManagerControlUI>(includeInactive: true);
                if (pm != null && pmcui != null)
                {
                    pm.itemType = PresetManager.ItemType.Custom;
                    pm.customPath = "Atom/Person/Morphs/";
                    pmcui.browsePresetsButton.onClick.Invoke();
                }
            });
        }

        public override void Update()
        {
            baUI.ForEach((ba) =>
            {
                ba.Update();
            });
        }
    }
}
