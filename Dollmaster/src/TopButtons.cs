using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;
using MeshVR;

namespace DeluxePlugin.Dollmaster
{
    public class TopButtons : BaseModule
    {
        int column = 0;
        int row = 0;

        public TopButtons(DollmasterPlugin dm) : base(dm)
        {
            AddButton("Select Person", () =>
            {
                SuperController.singleton.editModeToggle.isOn = true;
                SuperController.singleton.ShowMainHUDAuto();
                SuperController.singleton.ClearSelection();
                SuperController.singleton.SelectController(atom.mainController);
            });

            // Shit's broken yo.

            //AddButton("Change Look", () =>
            //{
            //    SuperController.singleton.ShowMainHUDAuto();
            //    PresetManager pm = atom.GetComponentInChildren<PresetManager>(includeInactive: true);
            //    PresetManagerControlUI pmcui = atom.GetComponentInChildren<PresetManagerControlUI>(includeInactive: true);
            //    if (pm != null && pmcui != null)
            //    {
            //        pm.itemType = PresetManager.ItemType.Atom;
            //        pm.creatorName = null;
            //        pm.storeFolderName = "Appearance";
            //        pm.storeName = "Appearance";
            //        pmcui.browsePresetsButton.onClick.Invoke();
            //        //atom.LoadAppearancePresetDialog();
            //    }
            //    dm.dressController.OnRestore();
            //});

            //AddButton("Change Outfit", () =>
            //{
            //    SuperController.singleton.ShowMainHUDAuto();
            //    PresetManager pm = atom.GetComponentInChildren<PresetManager>(includeInactive: true);
            //    PresetManagerControlUI pmcui = atom.GetComponentInChildren<PresetManagerControlUI>(includeInactive: true);

            //    if (pm != null && pmcui != null)
            //    {
            //        pm.itemType = PresetManager.ItemType.Custom;
            //        pm.customPath = "Atom/Person/Clothing/";
            //        pmcui.browsePresetsButton.onClick.Invoke();
            //    }

            //    dm.dressController.OnRestore();
            //});

            AddButton("Toggle Clothes", () =>
            {
                dm.dressController.ToggleDressed();
            });

            AddButton("Select Pose", () =>
            {
                SuperController.singleton.ShowMainHUDAuto();
                // TODO: Preset pose-loading is not working here.
                string text = SuperController.singleton.savesDir + atom.type + "\\pose";
                SuperController.singleton.fileBrowserUI.defaultPath = text;
                SuperController.singleton.fileBrowserUI.SetTextEntry(b: false);
                SuperController.singleton.fileBrowserUI.Show((path)=> {
                    if (string.IsNullOrEmpty(path))
                    {
                        return;
                    }
                    atom.LoadPhysicalPreset(path);

                    SuperController.singleton.PauseSimulation(5, "Loading Pose");

                    AllJointsController ajc = atom.GetComponentInChildren<AllJointsController>(includeInactive: true);
                    ajc.SetOnlyKeyJointsOn();
                    dm.thrustController.Clear();
                    dm.thrustController.GenerateThrustAtoms();
                });
            });

            AddButton("VAMasutra", () =>
            {
                SuperController.singleton.ShowMainHUDAuto();
                string sutraPath = DollmasterPlugin.ASSETS_PATH + "/VAMasutra";
                SuperController.singleton.fileBrowserUI.defaultPath = sutraPath;
                SuperController.singleton.fileBrowserUI.SetTextEntry(b: false);
                SuperController.singleton.fileBrowserUI.Show((path) => {
                    var montageJSON = JSON.Parse(SuperController.singleton.ReadFileIntoString(path)) as JSONClass;
                    dm.montageController.currentMontage = null;
                    MontageController.BeginMontage(dm, montageJSON);
                });
            });

            AddButton("Cycle Thruster", () =>
            {
                dm.montageController.NextThruster();
            });

            bool minimized = false;

            UIDynamicButton minimizeUIButton = ui.CreateButton("Minimize UI", 100, 40);
            minimizeUIButton.transform.Translate(0.1f, -0.1f, 0, Space.Self);
            UI.ColorButton(minimizeUIButton, Color.white, Color.black);

            Dictionary<GameObject, bool> priorActive = new Dictionary<GameObject, bool>();
            minimizeUIButton.button.onClick.AddListener(() =>
            {
                minimized = !minimized;

                Transform t = ui.canvas.transform;
                for(int i=0; i < t.childCount; i++)
                {
                    GameObject child = t.GetChild(i).gameObject;
                    if (minimized)
                    {
                        priorActive[child] = child.activeSelf;
                        child.SetActive(false);
                    }
                    else
                    {
                        child.SetActive(priorActive[child]);
                    }
                }

                minimizeUIButton.gameObject.SetActive(true);

                minimizeUIButton.label = minimized ? "Max UI" : "Minimize UI";
            });
        }

        public void AddButton(string name, UnityAction callback)
        {
            Color accessButtonColor = new Color(0.05f, 0.15f, 0.08f);
            Color accessTextColor = new Color(0.4f, 0.6f, 0.45f);
            float xSpacing = 0.22f;
            float ySpacing = 0.05f;

            UIDynamicButton button = ui.CreateButton(name, 100, 40);
            button.button.onClick.AddListener(callback);
            button.transform.Translate(column * xSpacing, 0.45f - row * ySpacing, 0, Space.Self);
            UI.ColorButton(button, accessTextColor, accessButtonColor);

            column++;
            if(column >= 4)
            {
                column = 0;
                row++;
            }
        }

    }
}
