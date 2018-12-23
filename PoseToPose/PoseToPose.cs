using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.PoseToPose
{
    public class PoseToPose : MVRScript
    {
        string PATH_WHEN_LOADED = "";
        Atom person = null;
        List<JSONClass> poses = new List<JSONClass>();

        public override void Init()
        {
            try
            {
                if (containingAtom == null)
                {
                    return;
                }

                

                PATH_WHEN_LOADED = SuperController.singleton.currentLoadDir;
                SuperController.singleton.currentSaveDir = PATH_WHEN_LOADED;

                AnimationPattern ap = containingAtom.GetStorableByID("AnimationPattern") as AnimationPattern;
                if (ap == null)
                {
                    SuperController.LogError("You must add this plugin to an AnimationPattern");
                    return;
                }

                CreatePeopleChooser((atom) =>
                {
                    if (atom == null)
                    {
                        return;
                    }

                    person = atom;
                });


                JSONStorableString posesStorable = new JSONStorableString("poses", "");
                RegisterString(posesStorable);


                //containingAtom.GetStorableByID()

                //SetStringParamValue("poses", posesFromLoad);

                //Debug.Log(posesStorable.val);

                CreateButton("Set Keyframe").button.onClick.AddListener(() =>
                {
                    JSONClass currentSave = SuperController.singleton.GetSaveJSON(containingAtom, true, true)["atoms"].AsArray[0] as JSONClass;
                    Debug.Log(currentSave);

                    //if (person == null)
                    //{
                    //    return;
                    //}

                    //AnimationStep step = ap.CreateStepAtPosition(ap.steps.Length);

                    //JSONClass pose = SuperController.singleton.GetSaveJSON(person, true, false)["atoms"].AsArray[0] as JSONClass;
                    //poses.Add(pose);
                    //posesStorable.SetVal(GetSaveString());
                });

                //StartCoroutine(OnPosesLoaded());
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        string GetSaveString()
        {
            return CombinePoses().ToString();
        }

        JSONArray CombinePoses()
        {
            JSONArray posesArray = new JSONArray();
            poses.ForEach((pose) =>
            {
                posesArray.Add(pose);
            });
            return posesArray;
        }

        private List<string> GetPeopleNamesFromScene()
        {
            return SuperController.singleton.GetAtoms()
                    .Where(atom => atom.GetStorableByID("geometry") != null && atom != containingAtom)
                    .Select(atom => atom.name).ToList();
        }

        private JSONStorableStringChooser CreatePeopleChooser(Action<Atom> onChange)
        {
            List<string> people = GetPeopleNamesFromScene();
            JSONStorableStringChooser personChoice = new JSONStorableStringChooser("copyFrom", people, null, "Copy From", (string id) =>
            {
                Atom atom = GetAtomById(id);
                onChange(atom);
            })
            {
                storeType = JSONStorableParam.StoreType.Full
            };

            if (people.Count > 0)
            {
                personChoice.SetVal(people[0]);
            }

            UIDynamicPopup scenePersonChooser = CreateScrollablePopup(personChoice, false);
            scenePersonChooser.popupPanelHeight = 250f;
            RegisterStringChooser(personChoice);
            scenePersonChooser.popup.onOpenPopupHandlers += () =>
            {
                personChoice.choices = GetPeopleNamesFromScene();
            };

            return personChoice;
        }
    }
}

class TempPoseHolder : MonoBehaviour
{
    public string poses;
}
