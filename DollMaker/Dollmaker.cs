using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace DeluxePlugin.DollMaker
{
    public class DollMaker : MVRScript
    {
        public static string CONFIG_PATH = Application.dataPath + "/../Saves/Scripts/VAMDeluxe/DollMaker/config.json";
        public static JSONClass CONFIG_JSON;

        public static Color BG_COLOR = new Color(0.15f, 0.15f, 0.15f);
        public static Color FG_COLOR = new Color(1, 1, 1);

        public UI ui;

        List<Module> modules = new List<Module>();

        public Atom person;

        public override void Init()
        {
            try
            {
                CONFIG_JSON = JSON.Parse(SuperController.singleton.ReadFileIntoString(CONFIG_PATH)) as JSONClass;

                CreatePeopleChooser((atom) =>
                {
                    if (atom == null)
                    {
                        return;
                    }
                    person = atom;
                });

                ui = new UI(this, 0.001f);
                ui.canvas.transform.SetParent(containingAtom.mainController.transform, false);

                modules.Add(new Appearance(this));
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void Start()
        {

        }

        void Update()
        {
            if (ui != null)
            {
                ui.Update();
            }

            modules.ForEach((module) =>
            {
                module.Update();
            });
        }

        void OnDestroy()
        {
            if (ui != null)
            {
                ui.OnDestroy();
            }

            modules.ForEach((module) =>
            {
                module.OnDestroy();
            });
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
