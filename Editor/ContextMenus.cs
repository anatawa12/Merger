using System;
using System.Linq;
using nadena.dev.ndmf.runtime;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal static class ContextMenus
    {
        private const string BASE_PATH = "GameObject/Avatar Optimizer/";
        private const int PRIORITY = 20;

        private const string MERGE_SKINNED_MESH = "Merge Skinned Mesh";
        private const string MERGE_PHYSBONE = "Merge PhysBone";

        private static bool CreateOrCreateAndConfigureWithMultipleValidate(Func<GameObject, bool> filter)
        {
            var objects = Selection.objects;
            if (objects.Length == 0) return false; // no selection
            var gameObjects = objects.OfType<GameObject>().ToArray();
            if (gameObjects.Length != objects.Length) return false; // some selections are not GameObject
            if (gameObjects.Length == 1) return true; // only one selection: create empty object
            // otherwise, we create configuration object and configure it
            return gameObjects.Any(filter);
        }

        private static void CreateAddComponentAndConfigure<T>(string componentName, Action<T, GameObject[]> configure) where T : MonoBehaviour
        {
            var gameObjects = Selection.gameObjects;
            if (gameObjects.Length == 0) return;
            var parentGameObject = FindCommonRoot(gameObjects);

            var newGameObject = new GameObject(componentName);
            var transform = newGameObject.transform;
            transform.SetParent(parentGameObject?.transform, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            
            var addedComponent = newGameObject.AddComponent<T>();

            if (gameObjects.Length > 1)
                configure(addedComponent, gameObjects);

            Undo.RegisterCreatedObjectUndo(newGameObject, $"Create {componentName}");
            Selection.activeGameObject = newGameObject;
            EditorGUIUtility.PingObject(newGameObject);
        }

        private static GameObject? FindCommonRoot(GameObject[] gameObjects)
        {
            if (gameObjects.Length == 0) return null;
            if (gameObjects.Length == 1) return gameObjects[0];

            var commonParents = gameObjects[0].transform.ParentEnumerable(includeMe: false).ToList();
            if (commonParents.Count == 0) return null;
            commonParents.Reverse();

            foreach (var gameObject in gameObjects.Skip(1))
            {
                var otherParents = gameObject.transform.ParentEnumerable(includeMe: false).ToList();
                otherParents.Reverse();

                var commonParentCount = commonParents.Zip(otherParents, (a, b) => a == b).TakeWhile(x => x).Count();
                commonParents.RemoveRange(commonParentCount, commonParents.Count - commonParentCount);

                if (commonParents.Count == 0) return null;
            }

            return commonParents.Last().gameObject;
        }

        // for GameObject/ menu, the handler will be called multiple times (selection count times)
        //   so we need to filter it
        // https://issuetracker.unity3d.com/issues/menuitem-is-executed-more-than-once-when-multiple-objects-are-selected
        private static bool _called;
        private static void OnceFilter(Action action)
        {
            if (_called) return;
            action();
            _called = true;
            EditorApplication.delayCall += () => _called = false;
        }

        [MenuItem(BASE_PATH + MERGE_SKINNED_MESH, true, PRIORITY)]
        private static bool ValidateCreateMergeSkinnedMesh() =>
            CreateOrCreateAndConfigureWithMultipleValidate(go => 
                go.TryGetComponent<SkinnedMeshRenderer>(out _)
                || go.TryGetComponent<MeshRenderer>(out _));

        [MenuItem(BASE_PATH + MERGE_SKINNED_MESH, false, PRIORITY)]
        private static void CreateMergeSkinnedMesh() => OnceFilter(() =>
            CreateAddComponentAndConfigure<MergeSkinnedMesh>(MERGE_SKINNED_MESH,
                (mergeSkinnedMesh, objects) =>
                {
                    mergeSkinnedMesh.renderersSet.AddRange(
                        objects.Select(x => x.GetComponent<SkinnedMeshRenderer>()));
                    mergeSkinnedMesh.staticRenderersSet.AddRange(
                        objects.Select(x => x.GetComponent<MeshRenderer>()));
                }));


#if AAO_VRCSDK3_AVATARS
        [MenuItem(BASE_PATH + MERGE_PHYSBONE, true, PRIORITY)]
        private static bool ValidateCreateMergePhysBone() => 
            CreateOrCreateAndConfigureWithMultipleValidate(go => 
                go.TryGetComponent<VRC.Dynamics.VRCPhysBoneBase>(out _));

        [MenuItem(BASE_PATH + MERGE_PHYSBONE, false, PRIORITY)]
        private static void CreateMergePhysBone() => OnceFilter(() =>
            CreateAddComponentAndConfigure<MergePhysBone>(MERGE_PHYSBONE,
                (mergePhysBone, objects) =>
                {
                    mergePhysBone.componentsSet.AddRange(
                        objects.Select(x => x.GetComponent<VRC.Dynamics.VRCPhysBoneBase>()));
                }));
#endif

        [MenuItem(BASE_PATH + "Add Trace and Optimize", true, PRIORITY)]
        private static bool ValidateAddTraceAndOptimize() => Selection.activeGameObject != null;

        [MenuItem(BASE_PATH + "Add Trace and Optimize", false, PRIORITY)]
        private static void AddTraceAndOptimize()
        {
            var gameObject = Selection.activeGameObject;
            if (gameObject == null) return;
            if (RuntimeUtil.IsAvatarRoot(gameObject.transform))
            {
                var traceAndOptimize = Undo.AddComponent<TraceAndOptimize>(gameObject);
                EditorGUIUtility.PingObject(traceAndOptimize);
            }
            else
            {
                EditorUtility.DisplayDialog(AAOL10N.Tr("ContextMenus:AddTraceAndOptimize:FailedToAddTraceAndOptimize:Title"),
                    AAOL10N.Tr("ContextMenus:AddTraceAndOptimize:FailedToAddTraceAndOptimize:Message"), 
                    "OK");
            }
        }
    }
}
