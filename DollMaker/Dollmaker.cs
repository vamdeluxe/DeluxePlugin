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
        public static string PLUGIN_PATH;
        public static string LOAD_PATH;

        public static string CONFIG_PATH = Application.dataPath + "/../Saves/Scripts/VAMDeluxe/DollMaker/config.json";
        public static JSONClass CONFIG_JSON;

        public static Color BG_COLOR = new Color(0.15f, 0.15f, 0.15f);
        public static Color FG_COLOR = new Color(1, 1, 1);

        public UI ui;

        public List<BaseModule> modules = new List<BaseModule>();

        public Atom person;

        public MainControls mainControls;

        public override void Init()
        {
            try
            {
                PLUGIN_PATH = GetPluginPath();
                LOAD_PATH = SuperController.singleton.currentLoadDir;

                CONFIG_PATH = PLUGIN_PATH + "/config.json";
                CONFIG_JSON = JSON.Parse(SuperController.singleton.ReadFileIntoString(CONFIG_PATH)) as JSONClass;

                ui = new UI(this, 0.001f);
                ui.canvas.transform.SetParent(containingAtom.mainController.transform, false);

                new WorldUI(this);

                mainControls = new MainControls(this);

                modules.Add(new Appearance(this));
                modules.Add(new Blend(this));

            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void Start()
        {

        }

        public void RestartModules()
        {
            modules.ForEach((module) =>
            {
                module.OnModuleActivate();
            });
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

        string GetPluginPath()
        {
            SuperController.singleton.currentSaveDir = SuperController.singleton.currentLoadDir;
            string pluginId = this.storeId.Split('_')[0];
            MVRPluginManager manager = containingAtom.GetStorableByID("PluginManager") as MVRPluginManager;
            string pathToScriptFile = manager.GetJSON(true, true)["plugins"][pluginId].Value;
            string pathToScriptFolder = pathToScriptFile.Substring(0, pathToScriptFile.LastIndexOfAny(new char[] { '/', '\\' }));
            return pathToScriptFolder;
        }

    }
}
