using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class ApplyObjectMapping
    {
        public void Apply(OptimizerSession session)
        {
            var mapping = session.MappingBuilder.BuildObjectMapping();

            // replace all objects
            foreach (var component in session.GetComponents<Component>())
            {
                var serialized = new SerializedObject(component);
                var p = serialized.GetIterator();
                AnimatorControllerMapper mapper = null;
                while (p.Next(true))
                {
                    if (p.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (mapping.InstanceIdToComponent.TryGetValue(p.objectReferenceInstanceIDValue,
                                out var mappedComponent))
                            p.objectReferenceValue = mappedComponent.Item3;

                        if (p.objectReferenceValue is AnimatorController controller)
                        {
                            if (mapper == null)
                                mapper = new AnimatorControllerMapper(mapping,
                                    session.RelativePath(component.transform), session);

                            var mapped = mapper.MapAnimatorController(controller);
                            if (mapped != null)
                                p.objectReferenceValue = mapped;
                        }
                    }
                }

                serialized.ApplyModifiedProperties();
            }
        }
    }

    internal class AnimatorControllerMapper
    {
        private readonly ObjectMapping _mapping;
        private readonly Dictionary<Object, Object> _cache = new Dictionary<Object, Object>();
        private readonly OptimizerSession _session;
        private readonly string _rootPath;
        private bool _mapped = false;

        public AnimatorControllerMapper(ObjectMapping mapping, string rootPath, OptimizerSession session)
        {
            _session = session;
            _mapping = mapping;
            _rootPath = rootPath;
        }

        public AnimatorController MapAnimatorController(AnimatorController controller)
        {
            if (_cache.TryGetValue(controller, out var cached)) return (AnimatorController)cached;
            _mapped = false;
            var newController = new AnimatorController
            {
                parameters = controller.parameters,
                layers = controller.layers.Select(MapAnimatorControllerLayer).ToArray()
            };
            if (!_mapped) newController = null;
            _cache[controller] = newController;
            return _session.AddToAsset(newController);
        }

        private AnimatorControllerLayer MapAnimatorControllerLayer(AnimatorControllerLayer layer) =>
            new AnimatorControllerLayer
            {
                name = layer.name,
                avatarMask = layer.avatarMask,
                blendingMode = layer.blendingMode,
                defaultWeight = layer.defaultWeight,
                syncedLayerIndex = layer.syncedLayerIndex,
                syncedLayerAffectsTiming = layer.syncedLayerAffectsTiming,
                iKPass = layer.iKPass,
                stateMachine = MapStateMachine(layer.stateMachine),
            };

        private AnimatorStateMachine MapStateMachine(AnimatorStateMachine stateMachine) =>
            DeepClone(stateMachine, CustomClone);

        // https://github.com/bdunderscore/modular-avatar/blob/db49e2e210bc070671af963ff89df853ae4514a5/Packages/nadena.dev.modular-avatar/Editor/AnimatorMerger.cs#L199-L241
        // Originally under MIT License
        // Copyright (c) 2022 bd_
        private Object CustomClone(Object o)
        {
            if (o is AnimationClip clip)
            {
                var newClip = _session.AddToAsset(new AnimationClip());
                newClip.name = "rebased " + clip.name;

                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    var newBinding = MapPath(binding);
                    if (newBinding.type == null) continue;
                    newClip.SetCurve(newBinding.path, newBinding.type, newBinding.propertyName,
                        AnimationUtility.GetEditorCurve(clip, binding));
                }

                foreach (var objBinding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    var newBinding = MapPath(objBinding);
                    if (newBinding.type == null) continue;
                    AnimationUtility.SetObjectReferenceCurve(newClip, newBinding,
                        AnimationUtility.GetObjectReferenceCurve(clip, objBinding));
                }

                newClip.wrapMode = clip.wrapMode;
                newClip.legacy = clip.legacy;
                newClip.frameRate = clip.frameRate;
                newClip.localBounds = clip.localBounds;
                AnimationUtility.SetAnimationClipSettings(newClip, AnimationUtility.GetAnimationClipSettings(clip));

                return newClip;
            }
            else
            {
                return null;
            }
        }

        private EditorCurveBinding MapPath(EditorCurveBinding binding)
        {
            string Join(string a, string b, char sep) => a == "" ? b : b == "" ? a : $"{a}{sep}{b}";
            string StripPrefixPath(string parent, string path, char sep)
            {
                if (path == null) return null;
                if (parent == path) return "";
                if (path.StartsWith($"{parent}{sep}", StringComparison.Ordinal))
                    return parent.Substring(parent.Length + 1);
                return null;
            }
            // Properties detailed first and nothing last
            IEnumerable<(string prop, string rest)> FindProps(string prop)
            {
                var rest = "";
                for (;;)
                {
                    yield return (prop, rest);

                    var index = prop.LastIndexOf('.');
                    if (index == -1) yield break;

                    rest = prop.Substring(index) + rest;
                    prop = prop.Substring(0, index);
                }
            }

            var oldPath = Join(_rootPath, binding.path, '/');

            // try as component first
            if (_mapping.ComponentMapping.TryGetValue((binding.type, oldPath), out var componentInfo))
            {
                var (newPath, propMapping) = componentInfo;
                newPath = StripPrefixPath(_rootPath, newPath, '/');
                if (newPath == null || propMapping == null)
                {
                    _mapped = true;
                    return default;
                }

                _mapped |= binding.path != newPath;
                binding.path = newPath;

                foreach (var (prop, rest) in FindProps(binding.propertyName))
                {
                    if (propMapping.TryGetValue(prop, out var newProp))
                    {
                        var newFullProp = newProp + rest;
                        _mapped |= binding.propertyName != newFullProp;
                        binding.propertyName = newFullProp;
                        break;
                    }
                }
                return binding;
            }

            // then, try as GameObject
            if (_mapping.GameObjectPathMapping.TryGetValue(oldPath, out var newGoPath))
            {
                newGoPath = StripPrefixPath(_rootPath, newGoPath, '/');
                if (newGoPath == null)
                {
                    _mapped = true;
                    return default;
                }
                _mapped |= binding.path != newGoPath;
                binding.path = newGoPath;
                return binding;
            }

            return binding;
        }

        // https://github.com/bdunderscore/modular-avatar/blob/db49e2e210bc070671af963ff89df853ae4514a5/Packages/nadena.dev.modular-avatar/Editor/AnimatorMerger.cs#LL242-L340C10
        // Originally under MIT License
        // Copyright (c) 2022 bd_
        private T DeepClone<T>(T original, Func<Object, Object> visitor) where T : Object
        {
            if (original == null) return null;

            // We want to avoid trying to copy assets not part of the animation system (eg - textures, meshes,
            // MonoScripts...), so check for the types we care about here
            switch (original)
            {
                // Any object referenced by an animator that we intend to mutate needs to be listed here.
                case Motion _:
                case AnimatorController _:
                case AnimatorState _:
                case AnimatorStateMachine _:
                case AnimatorTransitionBase _:
                case StateMachineBehaviour _:
                    break; // We want to clone these types

                // Leave textures, materials, and script definitions alone
                case Texture _:
                case MonoScript _:
                case Material _:
                    return original;

                // Also avoid copying unknown scriptable objects.
                // This ensures compatibility with e.g. avatar remote, which stores state information in a state
                // behaviour referencing a custom ScriptableObject
                case ScriptableObject _:
                    return original;

                default:
                    throw new Exception($"Unknown type referenced from animator: {original.GetType()}");
            }
            if (_cache.TryGetValue(original, out var cached)) return (T)cached;

            var obj = visitor(original);
            if (obj != null)
            {
                _cache[original] = obj;
                return (T)obj;
            }

            var ctor = original.GetType().GetConstructor(Type.EmptyTypes);
            if (ctor == null || original is ScriptableObject)
            {
                obj = Object.Instantiate(original);
            }
            else
            {
                obj = (T)ctor.Invoke(Array.Empty<object>());
                EditorUtility.CopySerialized(original, obj);
            }

            _cache[original] = _session.AddToAsset(obj);

            SerializedObject so = new SerializedObject(obj);
            SerializedProperty prop = so.GetIterator();

            bool enterChildren = true;
            while (prop.Next(enterChildren))
            {
                enterChildren = true;
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.ObjectReference:
                        prop.objectReferenceValue = DeepClone(prop.objectReferenceValue, visitor);
                        break;
                    // Iterating strings can get super slow...
                    case SerializedPropertyType.String:
                        enterChildren = false;
                        break;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return (T)obj;
        }
    }
}
