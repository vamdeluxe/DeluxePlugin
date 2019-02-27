using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;


namespace DeluxePlugin.Dollmaster
{
    public class Personas : BaseModule
    {
        public static string PERSONA_PATH;

        Dictionary<string, Personality> nameToPersonality = new Dictionary<string, Personality>();

        public Personas(DollmasterPlugin dm) : base(dm)
        {
            PERSONA_PATH = DollmasterPlugin.ASSETS_PATH + "/Personas";
            SuperController.singleton.GetDirectoriesAtPath(PERSONA_PATH).ToList().ForEach((string path)=>
            {
                path = SuperController.singleton.NormalizePath(path);
                string name = PathExt.GetFileName(path);
                nameToPersonality[name] = new Personality(path, name);
            });
        }

        public Personality GetPersonality(string name)
        {
            return nameToPersonality[name];
        }

        public Personality GetRandomPersonality()
        {
            int randomIndex = Mathf.Clamp(UnityEngine.Random.Range(0, personalities.Count), 0, personalities.Count-1);
            return personalities[randomIndex];
        }

        public List<string> personalityNames
        {
            get
            {
                return nameToPersonality.Keys.ToList();
            }
        }

        public List<Personality> personalities
        {
            get
            {
                return nameToPersonality.Values.ToList();
            }
        }
    }
}
