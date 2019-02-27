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
    class Appearance : BaseModule
    {
        MorphSearch morphSearch;
        List<BoneAttachedUI> baUI = new List<BoneAttachedUI>();

        public Appearance(DollMaker dm) : base(dm)
        {
            dm.mainControls.RegisterTab("Morphs", moduleUI);

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

            UIDynamicButton loadMorphsButton = CreateModuleButton("Load Morphs");
            loadMorphsButton.button.onClick.AddListener(() =>
            {
                string appearancePath = SuperController.singleton.savesDir + "Person" + "\\appearance";
                SuperController.singleton.fileBrowserUI.defaultPath = appearancePath;
                SuperController.singleton.fileBrowserUI.SetTextEntry(b: false);
                SuperController.singleton.fileBrowserUI.Show((string saveName)=>
                {
                    if (!(saveName != string.Empty))
                    {
                        return;
                    }
                    SuperController.singleton.SetLoadDirFromFilePath(saveName);
                    JSONClass save = JSON.Parse(SuperController.singleton.ReadFileIntoString(saveName)).AsObject;

                });
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
