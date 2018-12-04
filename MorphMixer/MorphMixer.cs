using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace DeluxePlugin {
	public class MorphMixer : MVRScript {

        Dictionary<string, float> initialMorphValues = new Dictionary<string, float>();
        private void UpdateInitialMorphs()
        {
            JSONStorable geometry = containingAtom.GetStorableByID("geometry");
            DAZCharacterSelector character = geometry as DAZCharacterSelector;
            GenerateDAZMorphsControlUI morphControl = character.morphsControlUI;
            morphControl.GetMorphDisplayNames().ForEach((name) =>
            {
                initialMorphValues[name] = morphControl.GetMorphByDisplayName(name).morphValue;
            });
        }

        public override void Init() {
			try {

                UpdateInitialMorphs();


                #region Choose Atom
                Atom otherAtom = null;
                List<string> receiverChoices = SuperController.singleton.GetAtoms()
                    .Where(atom => atom.GetStorableByID("geometry") != null && atom != containingAtom)
                    .Select(atom => atom.name).ToList();
                JSONStorableStringChooser receiverChoiceJSON = new JSONStorableStringChooser("copyFrom", receiverChoices, null, "Copy From", delegate (string otherName)
                {
                    otherAtom = GetAtomById(otherName);
                });
                receiverChoiceJSON.storeType = JSONStorableParam.StoreType.Full;
                UIDynamicPopup dp = CreateScrollablePopup(receiverChoiceJSON, false);
                dp.popupPanelHeight = 250f;
                RegisterStringChooser(receiverChoiceJSON);
                #endregion


                #region Morph Slider
                JSONStorable geometry = containingAtom.GetStorableByID("geometry");
                DAZCharacterSelector character = geometry as DAZCharacterSelector;
                GenerateDAZMorphsControlUI morphControl = character.morphsControlUI;

                JSONStorableFloat morphMixAmount = new JSONStorableFloat("morphMix", 0.0f, 0f, 1f, true);
                UIDynamicSlider morphSlider = CreateSlider(morphMixAmount, true);

                morphSlider.slider.onValueChanged.AddListener(delegate (float mixValue)
                {
                    if (otherAtom == null)
                    {
                        Debug.Log("other atom is null");
                        return;
                    }

                    JSONStorable otherGeometry = otherAtom.GetStorableByID("geometry");
                    DAZCharacterSelector otherCharacter = otherGeometry as DAZCharacterSelector;
                    GenerateDAZMorphsControlUI otherMorphControl = otherCharacter.morphsControlUI;

                    morphControl.GetMorphDisplayNames().ForEach((name) =>
                    {
                        DAZMorph otherMorph = otherMorphControl.GetMorphByDisplayName(name);
                        DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                        float initialValue = initialMorphValues[name];
                        float targetValue = otherMorph.morphValue;
                        morph.morphValue = initialValue + ( (targetValue - initialValue) * mixValue );
                    });
                });
                #endregion


                #region Apply Button
                UIDynamicButton applyButton = CreateButton("Apply As Starting Value",true);
                applyButton.button.onClick.AddListener(delegate ()
                {
                    UpdateInitialMorphs();
                });
                #endregion

                CreateSpacer();
                CreateTextField(new JSONStorableString("instructions", "\nPick a Person atom and use the slider to blend morphs."));
                CreateTextField(new JSONStorableString("instructions", "\nUse the Apply As Starting Value button to freeze morphs into place if you want to load a new target appearance."));




            }
            catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

        void Start()
        {
            UpdateInitialMorphs();
        }
	}
}