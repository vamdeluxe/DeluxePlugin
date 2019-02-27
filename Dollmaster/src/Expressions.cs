using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace DeluxePlugin.Dollmaster
{
    public class Expressions : BaseModule
    {
        GenerateDAZMorphsControlUI morphControl;

        JSONClass manifest;

        public ExpressionGroup mouthOpenGroup;
        public ExpressionGroup mouthClosedGroup;
        public ExpressionGroup eyesGroup;
        public ExpressionGroup idleGroup;
        public ExpressionGroup kissGroup;
        public ExpressionGroup climaxGroup;

        public static string EXPRESSIONS_PATH;

        public Expressions(DollmasterPlugin dm) : base(dm)
        {
            EXPRESSIONS_PATH = DollmasterPlugin.ASSETS_PATH + "/Expressions";
            string MANIFEST_PATH = EXPRESSIONS_PATH + "/expression_manifest.json";
            Debug.Log(MANIFEST_PATH);
            manifest = JSON.Parse(SuperController.singleton.ReadFileIntoString(MANIFEST_PATH)).AsObject;

            mouthOpenGroup = new ExpressionGroup(manifest["mouth open"].AsObject, dm);
            mouthClosedGroup = new ExpressionGroup(manifest["mouth closed"].AsObject, dm);
            eyesGroup = new ExpressionGroup(manifest["eyes"].AsObject, dm);
            idleGroup = new ExpressionGroup(manifest["idle"].AsObject, dm);
            kissGroup = new ExpressionGroup(manifest["kiss"].AsObject, dm);
            climaxGroup = new ExpressionGroup(manifest["climax"].AsObject, dm);

            JSONStorable geometry = atom.GetStorableByID("geometry");
            DAZCharacterSelector character = geometry as DAZCharacterSelector;
            morphControl = character.morphsControlUI;
        }

        public float GetStay(string category, string name, float defaultValue)
        {
            JSONClass node = manifest[category][name].AsObject;
            if (node == null)
            {
                return defaultValue;
            }
            else
            {
                return node["stay"].AsFloat;
            }
        }

        void ClearExpressionMorphs()
        {
            morphControl.GetMorphDisplayNames().ForEach((name) =>
            {
                DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                if (morph.isPoseControl || morph.region.Contains("Expressions"))
                {
                    morph.SetValue(morph.startValue);
                    morph.SyncJSON();
                }
            });
        }

        void ApplyJSONMorphs(JSONClass node)
        {
            JSONArray morphs = node["morphs"].AsArray;
            for (int i = 0; i < morphs.Count; i++)
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
                if (morph.isPoseControl || morph.region.Contains("Expressions"))
                {
                    morph.SetValue(value);
                    morph.SyncJSON();
                }
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            ClearExpressionMorphs();
        }

    }
}
