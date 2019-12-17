using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace DeluxePlugin.Qualia
{
    public class SharedBehaviors : MVRScript
    {
        List<IQualiaModule> modules = new List<IQualiaModule>();

        public Modules.GazeModule GazeModule = new Modules.GazeModule();
        public Modules.BlinkModule BlinkModule = new Modules.BlinkModule();
        public Modules.BodyLanguageModule BodyLanguageModule = new Modules.BodyLanguageModule();

        public override void Init()
        {
            #region Setup person defaults.
            var eyes = containingAtom.GetStorableByID("Eyes");
            eyes.SetStringChooserParamValue("lookMode", "Target");

            var autoExp = containingAtom.GetStorableByID("AutoExpressions");
            autoExp.SetBoolParamValue("enabled", false);
            #endregion

            modules.Add(BodyLanguageModule);
            modules.Add(GazeModule);
            modules.Add(BlinkModule);

            foreach(var module in modules)
            {
                module.Init(this);
            }
        }

        void OnDestroy()
        {
            foreach (var module in modules)
            {
                module.Destroy(this);
            }
        }

        void Update()
        {
            foreach (var module in modules)
            {
                module.Update(this);
            }
        }
    }
}
