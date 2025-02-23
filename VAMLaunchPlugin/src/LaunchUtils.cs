using System;
using UnityEngine;

namespace VaMLaunchPlugin
{
    public static class LaunchUtils
    {
        public const float LAUNCH_MAX_VAL = 99.0f;
        private const float LaunchMinSpeed = 10.0f;
        private const float LaunchMaxSpeed = 90.0f;
        
        // https://github.com/funjack/launchcontrol/blob/master/protocol/funscript/functions.go#L10
        public static float PredictMoveSpeed(float prevPos, float currPos, float durationSecs)
        {
            var durationNanoSecs = durationSecs * 1e9;
            
            var delta = currPos - prevPos;
            var dist = Math.Abs(delta);

            var mil = (durationNanoSecs / 1e6) * 90 / dist;
            var speed = 25000.0 * Math.Pow(mil, -1.05);

            return Mathf.Clamp((float)speed, LaunchMinSpeed, LaunchMaxSpeed);
        }

        // https://github.com/funjack/launchcontrol/blob/master/protocol/funscript/functions.go#L23
        public static float PredictMoveDuration(float dist, float speed)
        {
            if (dist <= 0.0f)
            {
                return 0.0f;
            }

            var mil = Math.Pow(speed / 25000, -0.95);
            var dur = (mil / (90 / dist)) / 1000;
            return (float) dur;
        }

        // https://github.com/funjack/launchcontrol/blob/master/protocol/funscript/functions.go#L34
        public static float PredictDistanceTraveled(float speed, float durationSecs)
        {
            if (speed <= 0.0f)
            {
                return 0.0f;
            }

            var durationNanoSecs = durationSecs * 1e9;
            
            var mil = Math.Pow((double)speed / 25000, -0.95);
            var diff = mil - durationNanoSecs / 1e6;
            var dist = 90 - (diff / mil * 90);

            return (float) dist;
        }
    }
}