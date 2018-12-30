using UnityEngine;
using SimpleJSON;

namespace DeluxePlugin.PoseToPose
{
    public class Keyframe
    {
        public AnimationStep step;
        public JSONClass pose;

        public Keyframe(JSONClass pose, AnimationStep step)
        {
            this.pose = pose;
            this.step = step;
        }
    }
}
