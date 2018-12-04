using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
namespace DeluxePlugin
{
    class MorphTools : MVRScript
    {
        public override void Init()
        {
            try
            {
                JSONStorable geometry = containingAtom.GetStorableByID("geometry");
                DAZCharacterSelector character = geometry as DAZCharacterSelector;
                GenerateDAZMorphsControlUI morphControl = character.morphsControlUI;

            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }
    }
}
