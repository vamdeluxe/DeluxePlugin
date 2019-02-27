using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;


namespace DeluxePlugin.Dollmaster
{
    public class ExpressionGroup
    {
        Dictionary<string, JSONClass> nameToJSON = new Dictionary<string, JSONClass>();
        DollmasterPlugin dm;

        Dictionary<string, NamedAudioClip> audioClips = new Dictionary<string, NamedAudioClip>();

        public ExpressionGroup(JSONClass groupJSON, DollmasterPlugin dm)
        {
            this.dm = dm;
            groupJSON.Keys.ToList().ForEach((string key) =>
             {
                 string name = PathExt.GetFileNameWithoutExtension(key);
                 JSONClass node = KeyToJSON(key, groupJSON[key]);
                 nameToJSON[name] = node;
             });
        }

        JSONClass KeyToJSON(string key, JSONNode expressionNode)
        {
            if (key.ToLower().Contains(".json"))
            {
                string expressionPath = Expressions.EXPRESSIONS_PATH + "/" + key;
                //Debug.Log(expressionPath);
                JSONClass expression = JSON.Parse(SuperController.singleton.ReadFileIntoString(expressionPath)).AsObject;
                if (expression["audio"] != null)
                {
                    NamedAudioClip clip = DollmasterPlugin.LoadAudio(DollmasterPlugin.PLUGIN_PATH +"/"+ expression["audio"]);
                    audioClips[expression["audio"]] = clip;
                }

                return expression;
            }
            else
            {
                JSONClass node = new JSONClass();
                node["morphs"] = new JSONArray();
                JSONClass morphNode = new JSONClass();
                morphNode["name"] = key;
                morphNode["value"] = expressionNode;
                node["morphs"].Add(morphNode);
                return node;
            }
        }

        public JSONNode SelectRandomExpression()
        {
            if (nameToJSON.Count == 0)
            {
                return null;
            }
            int index = Mathf.Clamp(UnityEngine.Random.Range(0, nameToJSON.Values.Count), 0, nameToJSON.Values.Count - 1);
            //Debug.Log("Selecting Expression: " + nameToJSON.Keys.ToArray()[index]);
            JSONClass expression = nameToJSON.Values.ToArray()[index];

            if (expression["audio"] != null)
            {
                dm.PlayAudio(audioClips[expression["audio"]]);
            }

            return expression;
        }
    }
}
