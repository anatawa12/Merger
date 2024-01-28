using System;
using CustomLocalization4EditorExtension;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;

namespace Anatawa12.AvatarOptimizer
{
    // previously known as Automatic Configuration
    [AddComponentMenu("Avatar Optimizer/AAO Trace And Optimize")]
    [DisallowMultipleComponent]
    [HelpURL("https://vpm.anatawa12.com/avatar-optimizer/ja/docs/reference/trace-and-optimize/")]
    internal class TraceAndOptimize : AvatarGlobalComponent
    {
        [NotKeyable]
        [CL4EELocalized("TraceAndOptimize:prop:freezeBlendShape")]
        [ToggleLeft]
        public bool freezeBlendShape = true;
        [NotKeyable]
        [CL4EELocalized("TraceAndOptimize:prop:removeUnusedObjects")]
        [ToggleLeft]
        public bool removeUnusedObjects = true;

        // Remove Unused Objects Options
        [NotKeyable]
        [CL4EELocalized("TraceAndOptimize:prop:preserveEndBone",
            "TraceAndOptimize:tooltip:preserveEndBone")]
        [ToggleLeft]
        public bool preserveEndBone;

        [NotKeyable]
        [CL4EELocalized("TraceAndOptimize:prop:removeZeroSizedPolygons")]
        [ToggleLeft]
        public bool removeZeroSizedPolygons = false;

        [NotKeyable]
        [CL4EELocalized("TraceAndOptimize:prop:optimizePhysBone")]
        [ToggleLeft]
#if !AAO_VRCSDK3_AVATARS
        // no meaning without VRCSDK
        [HideInInspector]
#endif
        public bool optimizePhysBone = true;

        [NotKeyable]
        public AnimatorOptimizer animatorOptimizer = new AnimatorOptimizer();

        // common parsing configuration
        [NotKeyable]
        [CL4EELocalized("TraceAndOptimize:prop:mmdWorldCompatibility",
            "TraceAndOptimize:tooltip:mmdWorldCompatibility")]
        [ToggleLeft]
        public bool mmdWorldCompatibility = true;

        [NotKeyable]
        public AdvancedSettings advancedSettings;
        
        [Serializable]
        public struct AdvancedSettings
        {
            [Tooltip("Exclude some GameObjects from Trace and Optimize")]
            public GameObject[] exclusions;
            [Tooltip("Add GC Debug Components instead of setting GC components")]
            [ToggleLeft]
            public bool gcDebug;
            [Tooltip("Do Not Configure MergeBone in New GC algorithm")]
            [ToggleLeft]
            public bool noConfigureMergeBone;
            [ToggleLeft]
            public bool noActivenessAnimation;
            [ToggleLeft]
            public bool skipFreezingNonAnimatedBlendShape;
            [ToggleLeft]
            public bool skipFreezingMeaninglessBlendShape;
            [ToggleLeft]
            public bool skipIsAnimatedOptimization;
            [ToggleLeft]
            public bool skipMergePhysBoneCollider;
        }

        [Serializable]
        internal class AnimatorOptimizer
        {
            [CL4EELocalized("TraceAndOptimize:prop:animatorOptimizer")]
            [ToggleLeft]
            public bool enabled = true;

            [CL4EELocalized("TraceAndOptimize:prop:removeUnusedAnimatingProperties")]
            [ToggleLeft]
            public bool removeUnusedAnimatingProperties = true;

            [CL4EELocalized("TraceAndOptimize:prop:entryExitToBlendTree")]
            [ToggleLeft]
#if !AAO_VRCSDK3_AVATARS
            // EntryExit to BlendTree optimization heavily depends on VRChat's behavior
            [HideInInspector]
#endif
            public bool entryExitToBlendTree = true;
        }
    }
}