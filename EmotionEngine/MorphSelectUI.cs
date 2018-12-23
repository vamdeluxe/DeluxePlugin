using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine.UI;

namespace DeluxePlugin.EmotionEngine
{
    public class MorphSelectUI
    {
        public DAZMorph selectedMorph;
        public JSONStorableFloat morphValue;
        public JSONStorableStringChooser morphString;

        JSONStorableStringChooser groupString;
        UIDynamicPopup groupPopup;
        UIDynamicPopup morphPopup;
        ExpressionCreator script;
        UIDynamicSlider morphSlider;
        UIDynamicButton removeButton;
        UIDynamic spacer;

        static int colorIndex = 0;
        static Color[] colors = new Color[8]
        {
            new Color(0.349f, 0.192f, 0.192f),
            new Color(0.349f, 0.294f, 0.192f),
            new Color(0.235f, 0.349f, 0.192f),
            new Color(0.192f, 0.349f, 0.286f),
            new Color(0.192f, 0.349f, 0.345f),
            new Color(0.192f, 0.258f, 0.349f),
            new Color(0.215f, 0.192f, 0.349f),
            new Color(0.333f, 0.192f, 0.349f)
        };

        static Color GetNextColor()
        {
            int pickedColorIndex = colorIndex % colors.Length;
            colorIndex++;

            return colors[pickedColorIndex];
        }

        public MorphSelectUI(ExpressionCreator script)
        {
            this.script = script;

            Color color = GetNextColor();

            groupString = new JSONStorableStringChooser("group", script.morphGroupMap.Keys.ToList(), "", "Morph Group", (string groupPopupName) =>
            {
                string groupName = script.morphGroupMap[groupPopupName];
                GenerateDAZMorphsControlUI morphControl = script.GetMorphControl();
                List<string> acceptedMorphs = new List<string>();
                script.GetMorphControl().GetMorphDisplayNames().ForEach((name) =>
                {
                    DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                    if (morph.region == groupName)
                    {
                        acceptedMorphs.Add(morph.displayName);
                    }
                });

                if (selectedMorph != null)
                {
                    morphValue.SetVal(selectedMorph.startValue);
                    selectedMorph = null;
                }

                List<string> existing = script.GetExistingMorphNames();

                morphString.choices = acceptedMorphs.ToList().Where((name)=>
                {
                    return existing.Contains(name) == false;
                }).ToList();
                morphString.SetVal(morphString.choices[0]);

                morphPopup.gameObject.SetActive(true);
            });

            groupPopup = script.CreatePopup(groupString, true);
            groupString.popup = groupPopup.popup;

            morphString = new JSONStorableStringChooser("morph", new List<string>(), "", "Morph", (string morphName) =>
            {
                GenerateDAZMorphsControlUI morphControl = script.GetMorphControl();
                DAZMorph morph = morphControl.GetMorphByDisplayName(morphName);
                if (morph == null)
                {
                    return;
                }

                if (selectedMorph != null)
                {
                    script.OnMorphRemoved(selectedMorph);

                    morphValue.SetVal(selectedMorph.startValue);
                    selectedMorph = null;
                }

                selectedMorph = morph;
                morphValue.min = morph.min;
                morphValue.max = morph.max;
                morphValue.SetVal(morph.startValue);
                morphValue.defaultVal = morphValue.defaultVal;

                removeButton.label = "Remove " + selectedMorph.displayName;
                morphSlider.label = selectedMorph.displayName;

                script.OnMorphSelected(selectedMorph);

                morphSlider.gameObject.SetActive(true);
                groupPopup.gameObject.SetActive(false);
            });
            morphPopup = script.CreateScrollablePopup(morphString, true);
            morphString.popup = morphPopup.popup;

            morphPopup.gameObject.SetActive(false);

            morphValue = new JSONStorableFloat("morphValue", 0, (float value) =>
            {
                if (selectedMorph == null)
                {
                    return;
                }

                selectedMorph.SetValue(value);

                script.OnMorphValueChanged(selectedMorph, value);

            }, 0, 1, false, true);

            morphSlider = script.CreateSlider(morphValue, true);
            morphValue.slider = morphSlider.slider;
            morphSlider.gameObject.SetActive(false);
            morphSlider.quickButtonsEnabled = false;

            removeButton = script.CreateButton("Remove Morph", true);
            removeButton.button.onClick.AddListener(() =>
            {
                Remove();
            });

            spacer = script.CreateSpacer(true);
        }

        public void OnSelect(ExpressionStep step)
        {
            //morphPopup.gameObject.SetActive(true);
            //groupPopup.gameObject.SetActive(true);
            //morphSlider.gameObject.SetActive(true);

            if (selectedMorph != null)
            {
                selectedMorph.SetValue(morphValue.val);
            }

            step.morphKeyframe.Keys.ToList().ForEach((morph) =>
            {
                if (morph == selectedMorph)
                {
                    float value = step.morphKeyframe[morph];
                    morphValue.val = value;
                }
            });
        }

        public void OnDeselect()
        {
            //morphPopup.gameObject.SetActive(false);
            //groupPopup.gameObject.SetActive(false);
            //morphSlider.gameObject.SetActive(false);
            Reset();
        }

        public void Remove()
        {
            script.RemoveButton(removeButton);
            script.RemovePopup(groupString);
            script.RemovePopup(morphString);
            script.RemoveSlider(morphValue);
            script.RemoveSpacer(spacer);

            if (selectedMorph != null)
            {
                selectedMorph.SetValue(selectedMorph.startValue);
                script.OnMorphRemoved(selectedMorph);
            }

            script.OnMorphSelectorRemoved(this);
        }

        public void Reset()
        {
            if (selectedMorph != null)
            {
                selectedMorph.SetValue(selectedMorph.startValue);
            }
            //morphValue.SetVal(selectedMorph.startValue);
        }

        public void SetCollapse(bool collapsed)
        {
            groupPopup.gameObject.SetActive(!collapsed && groupPopup.gameObject.activeSelf);
            morphPopup.gameObject.SetActive(!collapsed);
            removeButton.gameObject.SetActive(!collapsed);
            spacer.gameObject.SetActive(!collapsed);
        }
    }
}
