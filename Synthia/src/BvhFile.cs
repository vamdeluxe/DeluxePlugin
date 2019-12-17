using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using SimpleJSON;

// Original script by ElkVR
// Adapted in Synthia by VAMDeluxe
namespace DeluxePlugin.Synthia
{
    public class BvhTransform
    {
        public BvhBone bone;
        public Vector3 position;
        public Quaternion rotation;
    }

    // enums are not allowed in scripts (they crash VaM)
    public class RotationOrder
    {
        public const int XYZ = 0, XZY = 1;
        public const int YXZ = 2, YZX = 3;
        public const int ZXY = 4, ZYX = 5;
    }

    public class BvhBone
    {
        public string name;
        public BvhBone parent;
        public bool hasPosition, hasRotation;
        public int frameOffset;
        public Vector3 offset, posZero = Vector3.zero;
        public bool isHipBone = false;
        public int rotationOrder = RotationOrder.ZXY;

        public string ToDebugString()
        {
            return string.Format("{0} {1} {2} fo:{3} par:{4}", name, hasPosition ? "position" : "", hasRotation ? "rotation" : "", frameOffset, parent != null ? parent.name : "(null)");
        }
    }

    public class BvhFile
    {
        string[] raw;
        int posMotion;
        public BvhBone[] bones;
        float[][] frames;
        public int nFrames;
        public float frameTime;
        public string path;
        public bool isTranslationLocal;

        public BvhFile(string _path)
        {
            path = _path;
            Load(path);
        }

        public void Load(string path)
        {
            char[] delims = { '\r', '\n' };
            var raw = SuperController.singleton.ReadFileIntoString(path).Split(delims, System.StringSplitOptions.RemoveEmptyEntries);
            bones = ReadHierarchy(raw);
            frames = ReadMotion(raw);
            frameTime = ReadFrameTime(raw);
            nFrames = frames.Length;
            isTranslationLocal = IsEstimatedLocalTranslation();
            ReadZeroPos();
        }

        void ReadZeroPos()
        {
            if (nFrames > 0)
            {
                foreach (var tf in ReadFrame(0))
                {
                    if (tf.bone.hasPosition)
                        tf.bone.posZero = tf.position;
                }
            }
        }

        bool IsEstimatedLocalTranslation()
        {
            BvhBone hip = null;
            foreach (var bone in bones)
                if (bone.isHipBone)
                    hip = bone;
            if (hip == null)
                return true;    // best estimate without a hip bone
            var index = hip.frameOffset + 1;
            // Use hip 'y' to estimate the translation mode (local or "absolute")
            float sum = 0;
            for (var i = 0; i < nFrames; i++)
            {
                var data = frames[i];
                sum += data[index];
            }
            float average = sum / nFrames;
            float absScore = Mathf.Abs(hip.offset.y - average);    // absolute will have average close to offset
            float locScore = Mathf.Abs(average);    // lowest score wins
            return locScore < absScore;
        }

        public void LogHierarchy()
        {
            foreach (var bone in bones)
            {
                Debug.Log(bone.ToDebugString());
            }
        }

        float ReadFrameTime(string[] lines)
        {
            foreach (var line in lines)
            {
                if (line.StartsWith("Frame Time:"))
                {
                    var parts = line.Split(':');
                    return float.Parse(parts[1]);
                }
            }
            return (1f / 30);   // default to 30 FPS
        }

        int GetRotationOrder(string c1, string c2, string c3)
        {
            c1 = c1.ToLower().Substring(0, 1); c2 = c2.ToLower().Substring(0, 1); c3 = c3.ToLower().Substring(0, 1);
            if (c1 == "x" && c2 == "y" && c3 == "z") return RotationOrder.XYZ;
            if (c1 == "x" && c2 == "z" && c3 == "y") return RotationOrder.XZY;
            if (c1 == "y" && c2 == "x" && c3 == "z") return RotationOrder.YXZ;
            if (c1 == "y" && c2 == "z" && c3 == "x") return RotationOrder.YZX;
            if (c1 == "z" && c2 == "x" && c3 == "y") return RotationOrder.ZXY;
            if (c1 == "z" && c2 == "y" && c3 == "x") return RotationOrder.ZYX;
            return RotationOrder.ZXY;
        }

        BvhBone[] ReadHierarchy(string[] lines)
        {
            char[] delims = { ' ', '\t' };
            var boneList = new List<BvhBone>();
            BvhBone current = null;
            int frameOffset = 0;
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "MOTION")
                    break;
                var parts = lines[i].Split(delims, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && (parts[0] == "JOINT" || parts[0] == "ROOT"))
                {
                    current = new BvhBone();
                    current.name = parts[1];
                    current.offset = Vector3.zero;
                    current.frameOffset = frameOffset;
                    if (current.name == "hip")
                        current.isHipBone = true;
                    boneList.Add(current);
                }
                if (parts.Length >= 4 && parts[0] == "OFFSET" && current != null)
                    current.offset = new Vector3(-float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3])) * 0.01f;
                if (parts.Length >= 2 && parts[0] == "CHANNELS" && current != null)
                {
                    var nChannels = int.Parse(parts[1]);
                    frameOffset += nChannels;
                    // XXX: examples may exist that are not covered here (but I think they're rare) -- Found some!
                    // We now support 6 channels with X,Y,Zpos in first 3 and any rotation order
                    // Or 3 channels with any rotation order
                    if (nChannels == 3)
                    {
                        current.hasPosition = false;
                        current.hasRotation = true;
                        current.rotationOrder = GetRotationOrder(parts[2], parts[3], parts[4]);
                    }
                    else if (nChannels == 6)
                    {
                        current.hasPosition = true;
                        current.hasRotation = true;
                        current.rotationOrder = GetRotationOrder(parts[5], parts[6], parts[7]);
                    }
                    else
                        SuperController.LogError(string.Format("Unexpect number of channels in BVH Hierarchy {1} {0}", nChannels, current.name));
                }
                if (parts.Length >= 2 && parts[0] == "End" && parts[1] == "Site")
                    current = null;
            }
            return boneList.ToArray();
        }

        float[][] ReadMotion(string[] lines)
        {
            char[] delims = { ' ', '\t' };
            var output = new List<float[]>();
            var i = 0;
            for (; i < lines.Length; i++)
            {
                if (lines[i] == "MOTION")
                    break;
            }
            i++;
            for (; i < lines.Length; i++)
            {
                var raw = lines[i].Split(delims, System.StringSplitOptions.RemoveEmptyEntries);
                if (raw[0].StartsWith("F")) // Frame Time: and Frames:
                    continue;
                var frame = new float[raw.Length];
                for (var j = 0; j < raw.Length; j++)
                    frame[j] = float.Parse(raw[j]);
                output.Add(frame);
            }
            return output.ToArray();
        }

        public BvhTransform[] ReadFrame(int frame)
        {
            var data = frames[frame];
            var ret = new BvhTransform[bones.Length];
            for (var i = 0; i < bones.Length; i++)
            {
                var tf = new BvhTransform();
                var bone = bones[i];
                tf.bone = bone;
                var offset = bone.frameOffset;
                if (bone.hasPosition)
                {
                    // Use -'ve X to convert RH->LH
                    tf.position = new Vector3(-data[offset], data[offset + 1], data[offset + 2]) * 0.01f;
                    offset += 3;
                }
                float v1 = data[offset], v2 = data[offset + 1], v3 = data[offset + 2];

                Quaternion qx, qy, qz;
                switch (bone.rotationOrder)
                {
                    case RotationOrder.XYZ:
                        qx = Quaternion.AngleAxis(-v1, Vector3.left);
                        qy = Quaternion.AngleAxis(-v2, Vector3.up);
                        qz = Quaternion.AngleAxis(-v3, Vector3.forward);
                        tf.rotation = qx * qy * qz;
                        break;
                    case RotationOrder.XZY:
                        qx = Quaternion.AngleAxis(-v1, Vector3.left);
                        qy = Quaternion.AngleAxis(-v3, Vector3.up);
                        qz = Quaternion.AngleAxis(-v2, Vector3.forward);
                        tf.rotation = qx * qz * qy;
                        break;
                    case RotationOrder.YXZ:
                        qx = Quaternion.AngleAxis(-v2, Vector3.left);
                        qy = Quaternion.AngleAxis(-v1, Vector3.up);
                        qz = Quaternion.AngleAxis(-v3, Vector3.forward);
                        tf.rotation = qy * qx * qz;
                        break;
                    case RotationOrder.YZX:
                        qx = Quaternion.AngleAxis(-v3, Vector3.left);
                        qy = Quaternion.AngleAxis(-v1, Vector3.up);
                        qz = Quaternion.AngleAxis(-v2, Vector3.forward);
                        tf.rotation = qy * qz * qx;
                        break;
                    case RotationOrder.ZXY:
                        qx = Quaternion.AngleAxis(-v2, Vector3.left);
                        qy = Quaternion.AngleAxis(-v3, Vector3.up);
                        qz = Quaternion.AngleAxis(-v1, Vector3.forward);
                        tf.rotation = qz * qx * qy;
                        break;
                    case RotationOrder.ZYX:
                        qx = Quaternion.AngleAxis(-v3, Vector3.left);
                        qy = Quaternion.AngleAxis(-v2, Vector3.up);
                        qz = Quaternion.AngleAxis(-v1, Vector3.forward);
                        tf.rotation = qz * qy * qx;
                        break;
                }

                ret[i] = tf;
            }
            return ret;
        }
    }
}
