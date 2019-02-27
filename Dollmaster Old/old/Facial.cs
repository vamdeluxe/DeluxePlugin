using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using UnityEngine;

namespace DeluxePlugin
{
    public class Facial
    {
        Dictionary<string, float> morphGoalsTargets = new Dictionary<string, float>();
        Dictionary<string, float> morphGoals = new Dictionary<string, float>();
        Dictionary<string, DAZMorph> morphTargets = new Dictionary<string, DAZMorph>();
        string[] morphNames;

        public float activeRate = 6.2f;
        public float decayRate = 0.5f;

        public bool hold = false;
        public float holdDuration = 6.0f;
        private float holdFinishedTime = 0.0f;

        public bool shouldFade = false;

        public Facial(JSONStorable geometryStorable, JSONNode node)
        {
            foreach (KeyValuePair<string, JSONNode> kvp in node.AsObject)
            {
                string morphName = kvp.Key;
                morphGoals.Add(morphName, float.Parse(node[morphName]));
                morphGoalsTargets.Add(morphName, 0.0f);

                JSONStorable js = geometryStorable;
                if (js != null)
                {
                    DAZCharacterSelector dcs = js as DAZCharacterSelector;
                    GenerateDAZMorphsControlUI morphUI = dcs.morphsControlUI;
                    if (morphUI != null)
                    {
                        morphTargets.Add(morphName, morphUI.GetMorphByDisplayName(morphName) );
                    }
                }
            }

            morphNames = morphTargets.Keys.ToList().ToArray();
        }

        public void Trigger(float holdDuration)
        {
            foreach (string key in morphNames)
            {
                morphGoalsTargets[key] = 1.0f;
            }
            this.holdDuration = holdDuration;

            if (holdDuration > 0)
            {
                this.hold = true;
            }
            holdFinishedTime = Time.fixedTime + holdDuration;

            shouldFade = true;
        }

        public void Update()
        {
            if(shouldFade == false)
            {
                return;
            }

            foreach (string key in morphNames)
            {
                DAZMorph morph = morphTargets[key];
                float goal = morphGoals[key];
                float goalTarget = morphGoalsTargets[key];
                if (morph != null)
                {
                    float currentValue = morph.morphValue;
                    float targetValue = goal * goalTarget;
                    float newValue = currentValue + (targetValue - currentValue) * activeRate * Time.deltaTime;
                    morph.SetValue(newValue);
                }

                if (hold)
                {
                    if(Time.fixedTime >= holdFinishedTime)
                    {
                        hold = false;
                    }
                }
                else
                {

                    morphGoalsTargets[key] -= decayRate * Time.deltaTime;
                }

                morphGoalsTargets[key] = Mathf.Clamp01(morphGoalsTargets[key]);

            }
        }

        public void Stop()
        {
            shouldFade = true;
            foreach (string key in morphNames)
            {
                morphGoalsTargets[key] = 0.0f;
            }
        }
    }
}
