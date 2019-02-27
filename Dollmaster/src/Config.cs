using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace DeluxePlugin.Dollmaster
{
    public class Config : BaseModule
    {
        public JSONClass configJSON;

        public Config(DollmasterPlugin dm) : base(dm)
        {
            string CONFIG_PATH = DollmasterPlugin.PLUGIN_PATH + "/config.json";
            configJSON = JSON.Parse(SuperController.singleton.ReadFileIntoString(CONFIG_PATH)).AsObject;
            Apply();
        }

        public void Apply()
        {
            if(configJSON == null)
            {
                return;
            }

            JSONClass personDefaults = configJSON["personDefaults"].AsObject;

            if (personDefaults["autoExpression"] != null)
            {
                atom.GetStorableByID("AutoExpressions").SetBoolParamValue("enabled", personDefaults["autoExpression"].AsBool);
            }

            if (personDefaults["lookMode"] != null)
            {
                atom.GetStorableByID("Eyes").SetStringChooserParamValue("lookMode", personDefaults["lookMode"].Value);
            }
        }
    }
}
