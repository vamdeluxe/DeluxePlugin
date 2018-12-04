using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;


namespace DeluxePlugin
{
    public class EmotionEngine : MVRScript
    {
        private string facialFile = "./SharedAnimations/facial.json";
        private string sharedExpressionsFile = "./SharedAnimations/shared_expressions.json";

        public float nextPlayTime = 0.0f;

        Shuffler<Expression> expressions;
        Shuffler<Expression> climaxes;
        Expression lastPlayedExpression;

        Dictionary<string, Facial> facial = new Dictionary<string, Facial>();

        public Expression breathingIdle;
        public Expression breathingActive;
        public Expression panting;

        string[] facialKeys;

        AudioSourceControl headAudioSource;

        public AdjustJoints jawAdjust;
        public DAZMeshEyelidControl eyelidControl;


        public override void Init()
        {
            try
            {
                LoadFacial();
                LoadSharedExpressions();

                Dictionary<string, string> personalitiesMap = SuperController.singleton
                    .GetFilesAtPath(Utils.AbsPath("./personalities"), "expression_*.json")
                    .ToList()
                    .Aggregate(new Dictionary<string, string>(), (dict,file) => {
                        string name = FirstLetterToUpper(file.Substring(file.LastIndexOf("\\")+1).Replace(".json","").Replace("expression_",""));
                        dict[name] = file;
                        return dict;
                    });

                JSONStorableStringChooser personalityChooser = new JSONStorableStringChooser("personality", personalitiesMap.Keys.ToList(), personalitiesMap.Keys.ToArray()[0], "personality", (string personality)=>{
                    LoadExpressions(personalitiesMap[personality]);
                });
                CreatePopup(personalityChooser).popupPanelHeight = 400;
                //RegisterStringChooser(personalityChooser);

                LoadExpressions(personalitiesMap[personalitiesMap.Keys.ToArray()[0]]);

                nextPlayTime = Time.fixedTime + 1.0f;

                headAudioSource = containingAtom.GetStorableByID("HeadAudioSource") as AudioSourceControl;

                jawAdjust = containingAtom.GetStorableByID("JawControl") as AdjustJoints;
                jawAdjust.driveXRotationFromAudioSourceMultiplier = 115.0f;
                jawAdjust.driveXRotationFromAudioSourceAdditionalAngle = 0.0f;
                jawAdjust.driveXRotationFromAudioSourceMaxAngle = -5.0f;

                eyelidControl = containingAtom.GetStorableByID("EyelidControl") as DAZMeshEyelidControl;
                eyelidControl.SetBoolParamValue("blinkEnabled", false);
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void OnDestroy()
        {
            CleanupExpressions();
            panting.Cleanup();
            breathingIdle.Cleanup();
            breathingActive.Cleanup();
        }

        private void LoadFacial()
        {
            string facialPath = Utils.AbsPath(facialFile);

            string jsonString = SuperController.singleton.ReadFileIntoString(facialPath);
            JSONNode loadJson = JSON.Parse(jsonString);

            JSONStorable geometry = containingAtom.GetStorableByID("geometry");

            foreach (KeyValuePair<string, JSONNode> kvp in loadJson.AsObject)
            {
                string key = kvp.Key;
                facial.Add(key, new Facial(geometry, loadJson[key]));
            }

            facialKeys = facial.Keys.ToList().ToArray();
        }

        private void CleanupExpressions()
        {
            if (this.expressions != null)
            {
                this.expressions.list.ToList().ForEach((expression) =>
                {
                    expression.Cleanup();
                });
            }

            if (this.climaxes != null)
            {
                this.climaxes.list.ToList().ForEach((expression) =>
                {
                    expression.Cleanup();
                });
            }
        }

        private void LoadExpressions(string absExpressionPath)
        {
            CleanupExpressions();

            string jsonString = SuperController.singleton.ReadFileIntoString(absExpressionPath);
            JSONNode loadJson = JSON.Parse(jsonString);

            List<Expression> loadedExpression = new List<Expression>();
            var expressions = loadJson["expressions"];
            for (int i = 0; i < expressions.Count; i++)
            {
                Expression exp = new Expression(expressions[i], facial);
                loadedExpression.Add(exp);
            }
            this.expressions = new Shuffler<Expression>(loadedExpression.ToArray());

            List<Expression> loadedClimaxes = new List<Expression>();
            var climaxes = loadJson["climaxes"];
            for (int i = 0; i < climaxes.Count; i++)
            {
                Expression exp = new Expression(climaxes[i], facial);
                loadedClimaxes.Add(exp);
            }
            this.climaxes = new Shuffler<Expression>(loadedClimaxes.ToArray());

            lastPlayedExpression = null;
        }

        private void LoadSharedExpressions()
        {
            string expressionPath = Utils.AbsPath(sharedExpressionsFile);
            string jsonString = SuperController.singleton.ReadFileIntoString(expressionPath);
            JSONNode sharedJson = JSON.Parse(jsonString);
            panting = new Expression(sharedJson["panting"], facial);
            breathingIdle = new Expression(sharedJson["breathing idle"], facial);
            breathingActive = new Expression(sharedJson["breathing active"], facial);
        }

        public bool CanPlay()
        {
            return Time.fixedTime >= nextPlayTime;
        }


        public void StartBreathing(Breathe breath, float duration)
        {
            if (panting == null)
            {
                Debug.Log("panting");
                return;
            }
            if (breathingIdle == null)
            {
                Debug.Log("idle");
                return;
            }
            if (breathingActive == null)
            {
                Debug.Log("active");
                return;
            }
            if(breath.breath > 0.6f)
            {
                panting.TriggerFacial(duration);
                headAudioSource.PlayNow(panting.audioClip);
            }
            else
            if(breath.breath > 0.3f)
            {
                breathingActive.TriggerFacial(duration);
                headAudioSource.PlayNow(breathingActive.audioClip);
            }
            else
            {
                breathingIdle.TriggerFacial(duration);
                headAudioSource.PlayNow(breathingIdle.audioClip);
            }

            foreach (string key in facialKeys)
            {
                Facial f = facial[key];
                f.shouldFade = true;
            }
        }


        public void StartPanting(float duration)
        {
            panting.TriggerFacial(duration);
            headAudioSource.PlayNow(panting.audioClip);

            foreach (string key in facialKeys)
            {
                Facial f = facial[key];
                f.shouldFade = true;
            }
        }


        public void SelectExpression(float intensity, float maxIntensity, float desiredIntensity)
        {
            if (expressions == null)
            {
                return;
            }

            Expression nextExpression = expressions.Next();
            if (nextExpression!=null && nextExpression.ShouldPlay(intensity, maxIntensity, desiredIntensity) && nextExpression != lastPlayedExpression)
            {

                foreach (string key in facialKeys)
                {
                    Facial f = facial[key];
                    f.shouldFade = false;
                }

                lastPlayedExpression = nextExpression;
                nextExpression.TriggerFacial(0);


                float nextDuration = nextExpression.audioClip.sourceClip.length;
                headAudioSource.PlayNow(nextExpression.audioClip);

                jawAdjust.driveXRotationFromAudioSource = !nextExpression.keepMouthClosed;

                nextPlayTime = Time.fixedTime + UnityEngine.Random.Range(nextDuration, nextDuration + 1.0f);
            }

            jawAdjust.driveXRotationFromAudioSourceMultiplier = 115.0f;
            jawAdjust.driveXRotationFromAudioSourceAdditionalAngle = 0.0f;
            jawAdjust.driveXRotationFromAudioSourceMaxAngle = -5.0f;
        }


        public void SelectClimax(float duration)
        {
            foreach (string key in facialKeys)
            {
                Facial f = facial[key];
                f.shouldFade = false;
            }

            Expression climax = climaxes.Next();
            climax.TriggerFacial(duration);
            headAudioSource.PlayNow(climax.audioClip);
            nextPlayTime = Time.fixedTime + Mathf.Max(duration, climax.audioClip.sourceClip.length) + UnityEngine.Random.Range(2.0f, 4.0f);
        }

        public void Update()
        {
            foreach (string key in facialKeys)
            {
                Facial f = facial[key];
                f.Update();
            }
        }

        public string FirstLetterToUpper(string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }
    }

}
