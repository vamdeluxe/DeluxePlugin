using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace DeluxePlugin.Dollmaster
{
    public class PluginState
    {
        public JSONStorableStringChooser personalityChoice;
        public string defaultPersonalityName = "Celeste";
        public PluginState(DollmasterPlugin dm)
        {
            personalityChoice = new JSONStorableStringChooser("personality", dm.personas.personalityNames, defaultPersonalityName, "Personality", (string choice)=>
            {
                dm.SetPersonality(choice);
            });

            dm.RegisterStringChooser(personalityChoice);
            UIDynamicPopup personalityPopup = dm.CreatePopup(personalityChoice);
            personalityPopup.popup.topButton.image.color = new Color(0.8f, 0.65f, 0.13f);

            dm.CreateSpacer();
        }
    }
}
