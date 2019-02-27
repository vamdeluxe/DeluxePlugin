using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace DeluxePlugin.Dollmaster
{
    public class TopButtons : BaseModule
    {
        public TopButtons(DollmasterPlugin dm) : base(dm)
        {
            Color accessButtonColor = new Color(0.05f, 0.15f, 0.08f);
            Color accessTextColor = new Color(0.4f, 0.6f, 0.45f);

            float xSpacing = 0.22f;

            UIDynamicButton selectButton = ui.CreateButton("Select Person", 100, 40);
            selectButton.button.onClick.AddListener(() =>
            {
                SuperController.singleton.editModeToggle.isOn = true;
                SuperController.singleton.ShowMainHUD();
                SuperController.singleton.ClearSelection();
                SuperController.singleton.SelectController(atom.mainController);
            });
            selectButton.transform.Translate(0, 0.3f, 0, Space.Self);
            UI.ColorButton(selectButton, accessTextColor, accessButtonColor);

            UIDynamicButton loadLookButton = ui.CreateButton("Change Look", 100, 40);
            loadLookButton.button.onClick.AddListener(() =>
            {
                SuperController.singleton.ShowMainHUD();
                atom.LoadAppearancePresetDialog();
            });
            loadLookButton.transform.Translate(xSpacing, 0.3f, 0, Space.Self);
            UI.ColorButton(loadLookButton, accessTextColor, accessButtonColor);

            UIDynamicButton toggleDressButton = ui.CreateButton("Dress/Undress", 100, 40);
            toggleDressButton.transform.Translate(xSpacing * 2, 0.3f, 0, Space.Self);
            toggleDressButton.button.onClick.AddListener(() =>
            {
                dm.dressController.ToggleDressed();
            });
            UI.ColorButton(toggleDressButton, accessTextColor, accessButtonColor);

            UIDynamicButton exposeDressButton = ui.CreateButton("Expose Dress", 100, 40);
            exposeDressButton.button.onClick.AddListener(() =>
            {
                dm.dressController.CycleDressExposed();
            });
            exposeDressButton.transform.Translate(xSpacing * 3, 0.3f, 0, Space.Self);
            UI.ColorButton(exposeDressButton, accessTextColor, accessButtonColor);

            bool minimized = false;

            UIDynamicButton minimizeUIButton = ui.CreateButton("Minimize UI", 100, 40);
            minimizeUIButton.transform.Translate(0.1f, -0.25f, 0, Space.Self);
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

    }
}
