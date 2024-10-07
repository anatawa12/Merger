using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    public class TraceAndOptimizeState
    {
        public bool Enabled;
        public bool FreezeBlendShape;
        public bool RemoveUnusedObjects;
        public bool RemoveZeroSizedPolygon;
        public bool OptimizePhysBone;
        public bool OptimizeAnimator;
        public bool MergeSkinnedMesh;
        public bool AllowShuffleMaterialSlots;
        public bool OptimizeTexture;
        public bool MmdWorldCompatibility = true;

        public bool PreserveEndBone;
        public HashSet<GameObject> Exclusions = new HashSet<GameObject>();
        public bool GCDebug;
        public bool NoConfigureMergeBone;
        public bool NoActivenessAnimation;
        public bool SkipFreezingNonAnimatedBlendShape;
        public bool SkipFreezingMeaninglessBlendShape;
        public bool SkipIsAnimatedOptimization;
        public bool SkipMergePhysBoneCollider;
        public bool SkipEntryExitToBlendTree;
        public bool SkipRemoveUnusedAnimatingProperties;
        public bool SkipMergeBlendTreeLayer;
        public bool SkipRemoveMeaninglessAnimatorLayer;
        public bool SkipMergeStaticSkinnedMesh;
        public bool SkipMergeAnimatingSkinnedMesh;
        public bool SkipMergeMaterialAnimatingSkinnedMesh;
        public bool SkipMergeMaterials;
        public bool SkipRemoveEmptySubMesh;
        public bool SkipAnyStateToEntryExit;

        public Dictionary<SkinnedMeshRenderer, HashSet<string>> PreserveBlendShapes =
            new Dictionary<SkinnedMeshRenderer, HashSet<string>>();

        internal void Initialize(TraceAndOptimize config)
        {
            FreezeBlendShape = config.freezeBlendShape;
            RemoveUnusedObjects = config.removeUnusedObjects;
            RemoveZeroSizedPolygon = config.removeZeroSizedPolygons;
            OptimizePhysBone = config.optimizePhysBone;
            OptimizeAnimator = config.optimizeAnimator;
            MergeSkinnedMesh = config.mergeSkinnedMesh;
            AllowShuffleMaterialSlots = config.allowShuffleMaterialSlots;
            OptimizeTexture = config.optimizeTexture;
            MmdWorldCompatibility = config.mmdWorldCompatibility;

            PreserveEndBone = config.preserveEndBone;

            Exclusions = new HashSet<GameObject>(config.debugOptions.exclusions);
            GCDebug = config.debugOptions.gcDebug;
            NoConfigureMergeBone = config.debugOptions.noConfigureMergeBone;
            NoActivenessAnimation = config.debugOptions.noActivenessAnimation;
            SkipFreezingNonAnimatedBlendShape = config.debugOptions.skipFreezingNonAnimatedBlendShape;
            SkipFreezingMeaninglessBlendShape = config.debugOptions.skipFreezingMeaninglessBlendShape;
            SkipIsAnimatedOptimization = config.debugOptions.skipIsAnimatedOptimization;
            SkipMergePhysBoneCollider = config.debugOptions.skipMergePhysBoneCollider;
            SkipEntryExitToBlendTree = config.debugOptions.skipEntryExitToBlendTree;
            SkipRemoveUnusedAnimatingProperties = config.debugOptions.skipRemoveUnusedAnimatingProperties;
            SkipMergeBlendTreeLayer = config.debugOptions.skipMergeBlendTreeLayer;
            SkipRemoveMeaninglessAnimatorLayer = config.debugOptions.skipRemoveMeaninglessAnimatorLayer;
            SkipMergeStaticSkinnedMesh = config.debugOptions.skipMergeStaticSkinnedMesh;
            SkipMergeAnimatingSkinnedMesh = config.debugOptions.skipMergeAnimatingSkinnedMesh;
            SkipMergeMaterialAnimatingSkinnedMesh = config.debugOptions.skipMergeMaterialAnimatingSkinnedMesh;
            SkipMergeMaterials = config.debugOptions.skipMergeMaterials;
            SkipRemoveEmptySubMesh = config.debugOptions.skipRemoveEmptySubMesh;
            SkipAnyStateToEntryExit = config.debugOptions.skipAnyStateToEntryExit;

            Enabled = true;
        }
    }

    public class LoadTraceAndOptimizeConfiguration : Pass<LoadTraceAndOptimizeConfiguration>
    {
        public override string DisplayName => "T&O: Load Configuration";

        protected override void Execute(BuildContext context)
        {
            var config = context.AvatarRootObject.GetComponent<TraceAndOptimize>();
            if (config)
                context.GetState<TraceAndOptimizeState>().Initialize(config);
            DestroyTracker.DestroyImmediate(config);
        }
    }

    public abstract class TraceAndOptimizePass<T> : Pass<T> where T : TraceAndOptimizePass<T>, new()
    {
        protected sealed override void Execute(BuildContext context)
        {
            var state = context.GetState<TraceAndOptimizeState>();
            if (!state.Enabled) return;
            Execute(context, state);
        }

        protected abstract void Execute(BuildContext context, TraceAndOptimizeState state);
    }
}
