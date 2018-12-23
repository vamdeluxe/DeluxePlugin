using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.EmotionEngine
{
    public class ExpressionStep
    {
        public float t = 0;
        ExpressionCreator script;

        UIDynamicTextField textField;
        UIDynamicButton selectButton;

        public Dictionary<DAZMorph, float> morphKeyframe = new Dictionary<DAZMorph, float>();

        public ExpressionStep(ExpressionCreator script, float t)
        {
            this.script = script;
            this.t = t;

            CreateSelectButton();
        }

        public void OnSelect()
        {
            if (selectButton != null)
            {
                selectButton.buttonColor = Color.green;
            }
        }

        public void OnDeselect()
        {
            selectButton.buttonColor = Color.white;
        }

        public void Remove()
        {
            script.RemoveButton(selectButton);
        }

        public void ApplyMorphs()
        {
            morphKeyframe.Keys.ToList().ForEach((morph) =>
            {
                float value = morphKeyframe[morph];
                morph.SetValue(value);
            });
        }

        public void CreateSelectButton()
        {
            selectButton = script.CreateButton("Select: " + t, false);
            selectButton.button.onClick.AddListener(() =>
            {
                script.SelectStep(this);
            });
            selectButton.buttonColor = Color.white;
        }

        public void RemoveSelectButton()
        {
            if (selectButton != null)
            {
                script.RemoveButton(selectButton);
            }
        }

        public ExpressionKeyframe ToKeyFrame()
        {
            ExpressionKeyframe keyframe = new ExpressionKeyframe()
            {
                time = t
            };

            morphKeyframe.Keys.ToList().ForEach((morph) =>
            {
                //Debug.Log(morph.displayName + " " + morphKeyframe[morph]);
                keyframe.morphValues[morph.displayName] = morphKeyframe[morph];
            });
            return keyframe;
        }
    }
}
