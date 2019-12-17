using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace DeluxePlugin.Dollmaster
{
    public class Montage
    {
        Atom atom;
        public string name;
        public JSONClass montageJSON;
        public List<Pose> poses = new List<Pose>();
        int currentPoseIndex = 0;

        public Montage(Atom atom, string filePath)
        {
            this.atom = atom;
            montageJSON = JSON.Parse(SuperController.singleton.ReadFileIntoString(filePath)) as JSONClass;
            poses.Add(ExtractPersonPose(montageJSON));

            string fileName = PathExt.GetFileName(filePath);
            string folder = filePath.Replace(fileName, "");
            string montageName = PathExt.GetFileNameWithoutExtension(filePath);

            name = montageName;

            List<string> poseFiles = SuperController.singleton.GetFilesAtPath(folder, "*.json").ToList().Where(path =>
            {
                if (path.Contains(montageName) == false)
                {
                    return false;
                }
                int periodCount = path.Count(f => f == '.');
                if (periodCount <= 1)
                {
                    return false;
                }
                return true;
            }).ToList();

            poseFiles.ForEach(poseFilePath =>
            {
                //Debug.Log("pose file found" + poseFilePath);
                var poseMontageJSON = JSON.Parse(SuperController.singleton.ReadFileIntoString(poseFilePath)) as JSONClass;

                poses.Add(ExtractPersonPose(poseMontageJSON));
            });

            //Debug.Log("montage " + filePath + " has " + poses.Count() + " poses");
        }

        public Pose ExtractPersonPose(JSONClass json)
        {
            JSONArray atoms = json["atoms"].AsArray;

            JSONClass person1JSON = null;
            JSONClass person2JSON = null;
            for (int i = 0; i < atoms.Count; i++)
            {
                JSONClass atomObj = atoms[i].AsObject;
                string id = atomObj["id"].Value;
                if (id == "Person")
                {
                    person1JSON = atomObj;
                }
                if (id == "Person#2")
                {
                    person2JSON = atomObj;
                }
            }
            if (person1JSON == null)
            {
                SuperController.LogError("Expected at least one person named Person in montage");
                return null;
            }

            return new Pose(atom, person1JSON);
        }

        public void Activate(DollmasterPlugin dm)
        {
            MontageController.BeginMontage(dm, montageJSON);
        }

        public Pose SelectRandomPose()
        {
            if (poses.Count <= 0)
            {
                return null;
            }
            if (poses.Count == 1)
            {
                return poses[0];
            }

            int index = UnityEngine.Random.Range(0, poses.Count);
            while (currentPoseIndex == index)
            {
                index = UnityEngine.Random.Range(0, poses.Count);
            }

            currentPoseIndex = index;
            return poses[index];
        }
    }
}
