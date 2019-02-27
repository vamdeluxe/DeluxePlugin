using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;


namespace DeluxePlugin.Dollmaster
{
    public class MontageController : BaseModule
    {
        List<string> ignoredTypes = new List<string>()
        {
            "VRController",
            "WindowCamera",
            "CoreControl",
            "PlayerNavigationPanel",

        };

        public List<Montage> montages = new List<Montage>();

        JSONStorableStringChooser montageChoice;

        PoseController poseController;

        Montage currentMontage;

        UIDynamicButton nextMontageButton;

        UIDynamicPopup poseChoicePopup;
        UIDynamicButton addPoseButton;
        UIDynamicButton deletePoseButton;
        UIDynamicButton clearPosesButton;

        public MontageController(DollmasterPlugin dm, PoseController poseController) : base(dm)
        {
            this.poseController = poseController;

            UIDynamicButton saveButton = dm.CreateButton("Create Montage", true);
            saveButton.button.onClick.AddListener(() =>
            {
                SuperController.singleton.currentSaveDir = SuperController.singleton.currentLoadDir;

                string name = "Montage " + montages.Count;
                Montage montage = new Montage(name, GetMontageAtoms());
                montages.Add(montage);

                montageChoice.SetVal(name);
                montageChoice.choices = GetMontageNamesList();

                currentMontage = montage;

                nextMontageButton.gameObject.SetActive(true);
                poseController.nextPoseButton.gameObject.SetActive(true);
            });

            montageChoice = new JSONStorableStringChooser("montage", GetMontageNamesList(), "", "Select Montage", (string montageName)=>
            {
                Montage found = FindMontageByName(montageName);
                if (found == null)
                {
                    //SuperController.LogError("montage not found " + montageName);
                    SetPoseUIActive(false);
                    return;
                }

                float prevThrustValue = dm.thrustController.slider.slider.value;

                found.Apply();
                currentMontage = found;
                Debug.Log("Applying Montage " + montageName);
                poseController.SetMontage(found);

                dm.thrustController.slider.slider.value = prevThrustValue;

                nextMontageButton.gameObject.SetActive(true);
                poseController.nextPoseButton.gameObject.SetActive(true);

                SetPoseUIActive(true);
            });
            dm.RegisterStringChooser(montageChoice);
            //montageChoice.storeType = JSONStorableParam.StoreType.Appearance;

            nextMontageButton = dm.ui.CreateButton("Next Montage", 300, 80);
            nextMontageButton.transform.Translate(0, -0.1f, 0, Space.Self);
            nextMontageButton.buttonColor = new Color(0.4f, 0.3f, 0.05f);
            nextMontageButton.textColor = new Color(1, 1, 1);
            nextMontageButton.button.onClick.AddListener(() =>
            {
                if (montages.Count == 0)
                {
                    return;
                }

                int index = montageChoice.choices.IndexOf(montageChoice.val);
                int nextIndex = index + 1;
                if (nextIndex >= montageChoice.choices.Count)
                {
                    nextIndex = 0;
                }

                string choice = montageChoice.choices[nextIndex];
                montageChoice.SetVal(choice);

                poseController.StopCurrentAnimation();
            });
            nextMontageButton.gameObject.SetActive(false);

            dm.CreateSpacer(true).height = 25;
            dm.CreatePopup(montageChoice, true);
            montageChoice.popup.onOpenPopupHandlers += () => {
                montageChoice.choices = GetMontageNamesList();
            };

            dm.CreateButton("Update Selected Montage", true).button.onClick.AddListener(() =>
            {
                if (currentMontage != null)
                {
                    currentMontage.montageJSON = GetMontageAtoms();
                }
            });

            //dm.CreateButton("Clear Montages", true).button.onClick.AddListener(() =>
            //{
            //    montages.Clear();
            //    currentMontage = null;
            //    poseController.SetMontage(null);

            //    nextMontageButton.gameObject.SetActive(false);
            //    poseController.nextPoseButton.gameObject.SetActive(false);
            //});

            dm.CreateButton("Delete Selected Montage", true).button.onClick.AddListener(()=>
            {
                if (currentMontage == null)
                {
                    return;
                }

                montages.Remove(currentMontage);
                currentMontage = null;
                poseController.SetMontage(null);
                montageChoice.SetVal("");

                if (montages.Count == 0)
                {
                    nextMontageButton.gameObject.SetActive(false);
                    poseController.nextPoseButton.gameObject.SetActive(false);
                }
                else
                {
                    montageChoice.SetVal(montages[0].name);
                }
            });


            dm.CreateSpacer(true).height = 50;

            poseChoicePopup = dm.CreatePopup(poseController.poseChoice, true);
            poseChoicePopup.label = "Select Pose";
            poseChoicePopup.popup.onOpenPopupHandlers += () =>
            {
                if (currentMontage == null)
                {
                    poseController.poseChoice.choices = new List<string>();
                    return;
                }
                poseController.poseChoice.choices = currentMontage.GetPoseNames();
            };

            dm.CreateSpacer(true).height = 25;

            addPoseButton = dm.CreateButton("Add Pose", true);
            addPoseButton.button.onClick.AddListener(() =>
            {
                if (currentMontage == null)
                {
                    return;
                }

                currentMontage.AddPose(PoseController.GetLocalPose(atom));
            });

            deletePoseButton = dm.CreateButton("Delete Selected Pose", true);
            deletePoseButton.button.onClick.AddListener(() =>
            {
                if (currentMontage == null)
                {
                    return;
                }

                JSONClass pose = poseController.GetPoseFromName(poseController.poseChoice.val);
                if (pose == null)
                {
                    return;
                }

                currentMontage.poses.Remove(pose);
            });

            //clearPosesButton = dm.CreateButton("Clear Poses", true);
            //clearPosesButton.button.onClick.AddListener(() =>
            //{
            //    if (currentMontage == null)
            //    {
            //        return;
            //    }

            //    currentMontage.poses.Clear();
            //    poseController.poseChoice.choices = new List<string>();
            //});


            SetPoseUIActive(false);
        }

        protected override void OnContainingAtomRenamed(string newName, string oldName)
        {
            montages.ForEach((montage) =>
            {
                for(int i=0; i < montage.montageJSON.Count; i++)
                {
                    JSONClass atomNode = montage.montageJSON[i].AsObject;
                    if (atomNode["id"] == oldName)
                    {
                        atomNode["id"] = newName;
                    }
                }
            });
        }

        void SetPoseUIActive(bool active)
        {
            //  hacky way to hide something since SetActive won't work here
            Vector3 activeScale = active ? Vector3.one : Vector3.zero;
            poseChoicePopup.transform.localScale = activeScale;
            addPoseButton.transform.localScale = activeScale;
            deletePoseButton.transform.localScale = activeScale;
            //clearPosesButton.transform.localScale = activeScale;
        }

        public void Load(JSONArray montageJSON)
        {
            int total = montageJSON.Count;
            for(int i=0; i<total; i++)
            {
                JSONClass montageNode = montageJSON[i].AsObject;
                string name = montageNode["name"];
                JSONArray positions = montageNode["positions"].AsArray;
                Montage montage = new Montage(name, positions);
                montages.Add(montage);

                JSONArray posesNode = montageNode["poses"].AsArray;
                if (posesNode != null)
                {
                    for (int s = 0; s < posesNode.Count; s++)
                    {
                        JSONClass poseNode = posesNode[s].AsObject;
                        montage.AddPose(poseNode);
                    }
                }
            }
        }

        public void PostRestore()
        {
            montageChoice.choices = GetMontageNamesList();

            if (montageChoice.val != montageChoice.defaultVal)
            {
                currentMontage = FindMontageByName(montageChoice.val);
                if (currentMontage != null)
                {
                    poseController.SetMontage(currentMontage);
                }
            }

            if(montageChoice.choices.Count > 0){
                nextMontageButton.gameObject.SetActive(true);
                poseController.nextPoseButton.gameObject.SetActive(true);
            }
            else
            {
                nextMontageButton.gameObject.SetActive(false);
                poseController.nextPoseButton.gameObject.SetActive(false);
            }
        }

        public List<string> GetMontageNamesList()
        {
            return montages.Select((montage) =>
            {
                return montage.name;
            }).ToList();
        }

        public JSONArray GetJSON()
        {
            JSONArray montageArray = new JSONArray();
            montages.ForEach((montage) =>
            {
                montageArray.Add(montage.GetJSON());
            });
            return montageArray;
        }

        public Montage FindMontageByName(string name)
        {
            if (montages.Count == 0)
            {
                return null;
            }

            return montages.Find((montage) =>
            {
                return montage.name == name;
            });
        }

        JSONArray GetMontageAtoms()
        {
            JSONArray atoms = new JSONArray();

            SuperController.singleton.GetAtoms()
            .Where((atom) =>
            {
                return ignoredTypes.Contains(atom.type) == false;
            })
            .ToList()
            .ForEach((atom) =>
            {
                JSONClass save = SuperController.singleton.GetSaveJSON(atom, true, false);
                JSONClass atomJSON = save["atoms"].AsArray[0].AsObject;
                atoms.Add(atomJSON);
            });
            return atoms;
        }
    }
}
