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
    public class MainControls : BaseModule
    {
        GridLayoutGroup mainLayout;
        GridLayoutGroup tabLayout;
        List<Transform> UITabs = new List<Transform>();
        public MainControls(DollMaker dm) : base(dm)
        {
            float buttonHeight = 60;

            mainLayout = ui.CreateGridLayout(1200, buttonHeight);
            mainLayout.transform.localPosition = new Vector3(0, -100, 0);
            mainLayout.cellSize = new Vector2(300, buttonHeight);
            mainLayout.constraintCount = 1;
            mainLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            mainLayout.GetComponent<RectTransform>().pivot = new Vector2(0, 0);

            UIDynamicButton selectButton = ui.CreateButton("Select", 200, buttonHeight, mainLayout.transform);
            selectButton.button.onClick.AddListener(() =>
            {
                SuperController.singleton.editModeToggle.isOn = true;
                SuperController.singleton.ShowMainHUD();
                SuperController.singleton.ClearSelection();
                SuperController.singleton.SelectController(atom.mainController);
            });

            UIDynamicButton loadButton = ui.CreateButton("Load Appearance", 200, buttonHeight, mainLayout.transform);
            loadButton.button.onClick.AddListener(() =>
            {
                SuperController.singleton.editModeToggle.isOn = true;
                SuperController.singleton.ShowMainHUD();
                atom.LoadAppearancePresetDialog();
            });

            UIDynamicButton saveButton = ui.CreateButton("Save Appearance", 200, buttonHeight, mainLayout.transform);
            saveButton.button.onClick.AddListener(() =>
            {
                SuperController.singleton.editModeToggle.isOn = true;
                SuperController.singleton.ShowMainHUD();
                atom.SavePresetDialog(false, true);
            });

            tabLayout = ui.CreateGridLayout(1200, 80);
            tabLayout.transform.localPosition = new Vector3(0, -200, 0);
            tabLayout.cellSize = new Vector2(300, 80);
            tabLayout.constraintCount = 1;
            tabLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            tabLayout.GetComponent<RectTransform>().pivot = new Vector2(0, 0);
        }

        public void RegisterTab(string name, Transform tab)
        {
            UIDynamicButton tabButton = ui.CreateButton(name, 150, 80, tabLayout.transform);
            tabButton.buttonText.fontSize = 48;
            UI.ColorButton(tabButton, Color.white, new Color(0.5f, 0.8f, 0.4f));
        }
    }
}
