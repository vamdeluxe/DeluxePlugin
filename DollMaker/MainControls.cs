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
    public class MainControls : BaseModule
    {
        GridLayoutGroup moduleSelectLayout;
        GridLayoutGroup sharedControlsLayout;
        List<Transform> UITabs = new List<Transform>();
        List<BaseModule> modules = new List<BaseModule>();
        public MainControls(DollMaker dm) : base(dm)
        {
            float buttonHeight = 60;

            sharedControlsLayout = ui.CreateGridLayout(1200, buttonHeight);
            sharedControlsLayout.transform.localPosition = new Vector3(0, -100, 0);
            sharedControlsLayout.cellSize = new Vector2(300, buttonHeight);
            sharedControlsLayout.constraintCount = 1;
            sharedControlsLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            sharedControlsLayout.GetComponent<RectTransform>().pivot = new Vector2(0, 0);

            UIDynamicButton selectButton = ui.CreateButton("Select", 200, buttonHeight, sharedControlsLayout.transform);
            selectButton.button.onClick.AddListener(() =>
            {
                SuperController.singleton.editModeToggle.isOn = true;
                SuperController.singleton.ShowMainHUD();
                SuperController.singleton.ClearSelection();
                SuperController.singleton.SelectController(atom.mainController);
            });

            UIDynamicButton loadButton = ui.CreateButton("Load Appearance", 200, buttonHeight, sharedControlsLayout.transform);
            loadButton.button.onClick.AddListener(() =>
            {
                SuperController.singleton.editModeToggle.isOn = true;
                SuperController.singleton.ShowMainHUD();
                atom.LoadAppearancePresetDialog();
                dm.RestartModules();
            });

            UIDynamicButton loadPresetButton = ui.CreateButton("Load Preset", 200, buttonHeight, sharedControlsLayout.transform);
            loadPresetButton.button.onClick.AddListener(() =>
            {
                PresetManager pm = atom.GetComponentInChildren<PresetManager>(includeInactive: true);
                PresetManagerControlUI pmcui = atom.GetComponentInChildren<PresetManagerControlUI>(includeInactive: true);
                if (pm != null && pmcui != null)
                {
                    pm.itemType = PresetManager.ItemType.Custom;
                    pm.customPath = "Atom/Person/";
                    pmcui.browsePresetsButton.onClick.Invoke();
                    dm.RestartModules();
                }
            });

            UIDynamicButton saveButton = ui.CreateButton("Save Appearance", 200, buttonHeight, sharedControlsLayout.transform);
            saveButton.button.onClick.AddListener(() =>
            {
                SuperController.singleton.editModeToggle.isOn = true;
                SuperController.singleton.ShowMainHUD();
                atom.SavePresetDialog(false, true);
            });

            moduleSelectLayout = ui.CreateGridLayout(1200, 80);
            moduleSelectLayout.transform.localPosition = new Vector3(0, -200, 0);
            moduleSelectLayout.cellSize = new Vector2(300, 80);
            moduleSelectLayout.constraintCount = 1;
            moduleSelectLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            moduleSelectLayout.GetComponent<RectTransform>().pivot = new Vector2(0, 0);
        }

        public void RegisterTab(string name, Transform tab, BaseModule module)
        {
            UIDynamicButton tabButton = ui.CreateButton(name, 150, 80, moduleSelectLayout.transform);
            tabButton.buttonText.fontSize = 48;
            UI.ColorButton(tabButton, Color.white, new Color(0.5f, 0.8f, 0.4f));

            UITabs.Add(tab);

            tab.gameObject.SetActive(false);

            tabButton.button.onClick.AddListener(() =>
            {
                UITabs.ForEach((uiTab) =>
                {
                    uiTab.gameObject.SetActive(false);
                });
                tab.gameObject.SetActive(true);
                module.OnModuleActivate();
            });


        }
    }
}
