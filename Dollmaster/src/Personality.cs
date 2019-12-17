using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace DeluxePlugin.Dollmaster
{
    public class Personality
    {
        public string path;
        public string name;
        public JSONClass personaConfig;

        public Personality(string path, string name)
        {
            this.path = path;
            this.name = name;
        }

        List<NamedAudioClip> audioClips = new List<NamedAudioClip>();

        public void Load()
        {
            LoadAudio();
            personaConfig = LoadPersonaConfig();
        }

        JSONClass LoadPersonaConfig()
        {
            JSONClass json = null;
            try
            {
                json = JSON.Parse(SuperController.singleton.ReadFileIntoString(path + "/persona.json")).AsObject;
            }
            catch(Exception e)
            {
                Debug.LogWarning(e);
            }

            return json;
        }

        void LoadAudio()
        {
            GetAudioPaths().ForEach((string filePath) =>
            {
                audioClips.Add(DollmasterPlugin.LoadAudio(filePath));
            });
        }

        public void Unload()
        {
            UnloadAudio();
        }

        void UnloadAudio()
        {
            audioClips.ForEach((nac) =>
            {
                URLAudioClipManager.singleton.RemoveClip(nac);
            });
            audioClips.Clear();
        }

        List<string> GetAudioPaths()
        {
            List<string> acceptedExtensions = new List<string> { ".wav", ".mp3", ".ogg" };
            return SuperController.singleton.GetFilesAtPath(path)
            .ToList()
            .Where(s => acceptedExtensions.Contains(PathExt.GetExtension(s)))
            .ToList();
        }

        public NamedAudioClip GetTriggeredAudioClip(Arousal arousal)
        {
            if (audioClips.Count == 0)
            {
                return null;
            }

            if (personaConfig == null)
            {
                return GetRandomAudioClip();
            }
            else
            {
                float arousalNormalized = arousal.value / Arousal.SLIDER_MAX;
                JSONArray expressions = personaConfig["expressions"].AsArray;
                List<JSONClass> validExpressions = new List<JSONClass>();
                for(int i=0; i<expressions.Count; i++)
                {
                    JSONClass expression = expressions[i].AsObject;
                    float minArousal = expression["minIntensity"].AsFloat;
                    float maxArousal = expression["maxIntensity"].AsFloat;

                    if (arousalNormalized >= minArousal && arousalNormalized <= maxArousal)
                    {
                        //Debug.Log(arousalNormalized);
                        //Debug.Log(minArousal + " " + maxArousal);
                        validExpressions.Add(expression);
                    }
                }

                if (validExpressions.Count == 0)
                {
                    validExpressions.Add(expressions[0].AsObject);
                }

                int randomIndex = UnityEngine.Random.Range(0, validExpressions.Count);
                JSONClass picked = validExpressions[randomIndex].AsObject;
                string clipName = PathExt.GetFileName(picked["audio"].Value);
                return audioClips.Find((nac) =>
                {
                    return nac.displayName == clipName;
                });
            }
        }

        public NamedAudioClip GetRandomClimaxClip()
        {
            if (personaConfig == null)
            {
                return null;
            }
            JSONArray climaxes = personaConfig["climaxes"].AsArray;
            if (climaxes.Count == 0)
            {
                return GetRandomAudioClip();
            }
            int randomIndex = UnityEngine.Random.Range(0, climaxes.Count);
            JSONClass picked = climaxes[randomIndex].AsObject;
            string clipName = PathExt.GetFileName(picked["audio"].Value);
            return audioClips.Find((nac) =>
            {
                return nac.displayName == clipName;
            });
        }

        public NamedAudioClip GetRandomPantingClip()
        {
            return GetRandomClipFromCategory("panting");
        }

        public NamedAudioClip GetRandomBreatheClip()
        {
            return GetRandomClipFromCategory("breathe");
        }

        NamedAudioClip GetRandomClipFromCategory(string category)
        {
            if (personaConfig == null)
            {
                return null;
            }

            if (personaConfig[category] == null)
            {
                return null;
            }

            JSONArray clipGroup = personaConfig[category].AsArray;
            if (clipGroup == null)
            {
                return null;
            }

            if (clipGroup.Count == 0)
            {
                return null;
            }

            Debug.Log(clipGroup.Count);

            int randomIndex = UnityEngine.Random.Range(0, clipGroup.Count);
            string picked = clipGroup[randomIndex].Value;

            string clipName = PathExt.GetFileName(picked);
            Debug.Log(clipName);

            return audioClips.Find((nac) =>
            {
                return nac.displayName == clipName;
            });
        }

        NamedAudioClip GetRandomAudioClip()
        {
            if (audioClips.Count == 0)
            {
                return null;
            }

            int index = Mathf.Clamp(UnityEngine.Random.Range(0, audioClips.Count), 0, audioClips.Count - 1);
            return audioClips[index];
        }

        public void OnDestroy()
        {
            UnloadAudio();
        }
    }
}
