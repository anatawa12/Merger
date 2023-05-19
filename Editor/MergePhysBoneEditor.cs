using System;
using System.Collections.Generic;
using System.Linq;
using Anatawa12.AvatarOptimizer.ErrorReporting;
using CustomLocalization4EditorExtension;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(MergePhysBone))]
    internal class MergePhysBoneEditor : AvatarTagComponentEditorBase
    {
        private MergePhysBoneEditorRenderer _renderer;
        private SerializedProperty _makeParent;
        private SerializedProperty _componentsSetProp;

        private void OnEnable()
        {
            _renderer = new MergePhysBoneEditorRenderer(serializedObject);
            _makeParent = serializedObject.FindProperty("makeParent");
            _componentsSetProp = serializedObject.FindProperty(nameof(MergePhysBone.componentsSet));
        }

        protected override void OnInspectorGUIInner()
        {
            EditorGUILayout.PropertyField(_makeParent);
            if (_makeParent.boolValue && ((Component)target).transform.childCount != 0)
                EditorGUILayout.HelpBox(CL4EE.Tr("MergePhysBone:error:makeParentWithChildren"), MessageType.Error);

            EditorGUILayout.PropertyField(_componentsSetProp);

            // draw custom editor
            _renderer.DoProcess();

            serializedObject.ApplyModifiedProperties();
        }
    }

    sealed class MergePhysBoneEditorRenderer : MergePhysBoneEditorModificationUtils
    {
        public MergePhysBoneEditorRenderer(SerializedObject serializedObject) : base(serializedObject)
        {
        }

        private readonly Dictionary<string, bool> _sectionFolds = new Dictionary<string, bool>();

        protected override void BeginPbConfig()
        {
            Utils.HorizontalLine();
        }

        protected override bool BeginSection(string name, string docTag) {
            if (!_sectionFolds.TryGetValue(name, out var open)) open = true;
            var rect = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, EditorGUIUtility.fieldWidth, 18f, 18f, EditorStyles.foldoutHeader);
            var (foldout, button) = SplitRect(rect, OverrideWidth);
            open = EditorGUI.Foldout(foldout, open, name, EditorStyles.foldoutHeader);
            _sectionFolds[name] = open;
            if (GUI.Button(button, "?"))
                Application.OpenURL("https://docs.vrchat.com/docs/physbones#" + docTag);
            EditorGUI.indentLevel++;
            return open;
        }

        protected override void EndSection() {
            EditorGUI.indentLevel--;
        }

        protected override void EndPbConfig()
        {
        }

        protected override void UnsupportedPbVersion()
        {
            EditorGUILayout.HelpBox(CL4EE.Tr("MergePhysBone:error:unsupportedPbVersion"), MessageType.Error);
        }

        protected override void NoSource() {
            EditorGUILayout.HelpBox(CL4EE.Tr("MergePhysBone:error:noSources"), MessageType.Error);
        }

        protected override void TransformSection() {
            EditorGUILayout.LabelField("Root Transform", "Auto Generated");
            if (!MakeParent.boolValue)
            {
                var differ = SourcePhysBones.Cast<Component>()
                    .Select(x => x.transform.parent)
                    .ZipWithNext()
                    .Any(x => x.Item1 != x.Item2);
                if (differ)
                    EditorGUILayout.HelpBox(CL4EE.Tr("MergePhysBone:error:parentDiffer"), MessageType.Error);
            }
            EditorGUILayout.LabelField("Ignore Transforms", "Automatically Merged");
            EditorGUILayout.LabelField("Endpoint Position", "Cleared to zero");
            EditorGUILayout.LabelField("Multi Child Type", "Must be Ignore");
            var multiChildType = GetSourceProperty("multiChildType");
            if (multiChildType.enumValueIndex != 0 || multiChildType.hasMultipleDifferentValues)
                EditorGUILayout.HelpBox(CL4EE.Tr("MergePhysBone:error:multiChildType"), MessageType.Error);
        }
        protected override void OptionParameter() {
            EditorGUILayout.PropertyField(Parameter.OverrideValue);
            EditorGUILayout.HelpBox("See VRCPhysBone editor's text OR docs for more info about Parameter.",
                MessageType.Info);
        }
        protected override void OptionIsAnimated() {
            EditorGUILayout.PropertyField(IsAnimated.OverrideValue);
        }

        const float OverrideWidth = 30f;
        const float CurveButtonWidth = 20f;

        private (Rect restRect, Rect fixedRect) SplitRect(Rect propRect, float width)
        {
            var restRect = propRect;
            restRect.width -= EditorGUIUtility.standardVerticalSpacing + width;
            var fixedRect = propRect;
            fixedRect.x = restRect.xMax + EditorGUIUtility.standardVerticalSpacing;
            fixedRect.width = width;
            return (restRect, fixedRect);
        }

        private (Rect restRect, Rect fixedRect0, Rect fixedRect1) SplitRect(Rect propRect, float width0, float width1)
        {
            var (tmp, fixedRect1) = SplitRect(propRect, width1);
            var (restRect, fixedRect0) = SplitRect(tmp, width0);
            return (restRect, fixedRect0, fixedRect1);
        }

        
        bool IsCurveWithValue(SerializedProperty prop) =>
            prop.animationCurveValue != null && prop.animationCurveValue.length > 0;

        protected override void PbVersionProp(string label,
            ValueConfigProp prop, bool forceOverride = false)
        {
            var labelContent = new GUIContent(label);

            var (valueRect, buttonRect, overrideRect) =
                SplitRect(EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight), CurveButtonWidth,
                    OverrideWidth);

            if (forceOverride || prop.IsOverride)
            {
                // Override mode

                renderer(prop.OverrideValue);

                if (forceOverride)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    PopupNoIndent(overrideRect, 1, CopyOverride);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUI.BeginProperty(overrideRect, null, prop.IsOverrideProperty);
                    var selected = PopupNoIndent(overrideRect, 1, CopyOverride);
                    if (selected != 1)
                        prop.IsOverrideProperty.boolValue = false;
                    EditorGUI.EndProperty();
                }
            }
            else
            {
                // Copy mode
                EditorGUI.BeginDisabledGroup(true);
                var differ = renderer(prop.SourceValue);
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginProperty(overrideRect, null, prop.IsOverrideProperty);
                var selected = PopupNoIndent(overrideRect, 0, CopyOverride);
                if (selected != 0)
                    prop.IsOverrideProperty.boolValue = true;
                EditorGUI.EndProperty();

                if (differ)
                {
                    EditorGUILayout.HelpBox(
                        "The value is differ between two or more sources. " +
                        "You have to set same value OR override this property", 
                        MessageType.Error);
                }
            }
            
            const string docURL = "https://docs.vrchat.com/docs/physbones#versions";

            if (GUI.Button(buttonRect, "?"))
            {
                Application.OpenURL(docURL);
            }

            bool renderer(SerializedProperty property)
            {
                var prevValue = property.enumValueIndex;
                EditorGUI.PropertyField(valueRect, property, labelContent);
                var newValue = property.enumValueIndex;
                if (prevValue != newValue)
                {
                    switch (EditorUtility.DisplayDialogComplex(
                                CL4EE.Tr("MergePhysBone:dialog:versionInfo:title"), 
                                CL4EE.Tr("MergePhysBone:dialog:versionInfo:message"),
                                CL4EE.Tr("MergePhysBone:dialog:versionInfo:openDoc"), 
                                CL4EE.Tr("MergePhysBone:dialog:versionInfo:revert"),
                                CL4EE.Tr("MergePhysBone:dialog:versionInfo:continue")))
                    {
                        case 0:
                            Application.OpenURL(docURL);
                            break;
                        case 1:
                            property.enumValueIndex = prevValue;
                            break;
                        case 2:
                            property.enumValueIndex = newValue;
                            break;
                    }
                }
                return property.hasMultipleDifferentValues;
            }
        }


        protected override void PbProp(string label, ValueConfigProp prop, bool forceOverride = false)
        {
            PbPropImpl(label, prop, forceOverride, (valueRect, merged, labelContent) =>
            {
                var property = prop.GetValueProperty(merged);
                EditorGUI.PropertyField(valueRect, property, labelContent);
                return property.hasMultipleDifferentValues;
            });
        }

        private void DrawCurveFieldWithButton(SerializedProperty curveProp, Rect buttonRect, Func<Rect> curveRect)
        {
            if (IsCurveWithValue(curveProp))
            {
                if (GUI.Button(buttonRect, "X"))
                {
                    curveProp.animationCurveValue = new AnimationCurve();
                }

                var rect = curveRect();
                EditorGUI.BeginProperty(rect, null, curveProp);
                EditorGUI.BeginChangeCheck();
                var cur = EditorGUI.CurveField(rect, " ", curveProp.animationCurveValue, Color.cyan,
                    new Rect(0.0f, 0.0f, 1f, 1f));
                if (EditorGUI.EndChangeCheck())
                    curveProp.animationCurveValue = cur;
                EditorGUI.EndProperty();
            }
            else
            {
                if (GUI.Button(buttonRect, "C"))
                {
                    var curve = new AnimationCurve();
                    curve.AddKey(new Keyframe(0.0f, 1f));
                    curve.AddKey(new Keyframe(1f, 1f));
                    curveProp.animationCurveValue = curve;
                }
            }
        }

        protected override void PbCurveProp(string label, CurveConfigProp prop, bool forceOverride = false)
        {
            PbPropImpl(label, prop, forceOverride, (rect, merged, labelContent) =>
            {
                var (valueRect, buttonRect) = SplitRect(rect, CurveButtonWidth);

                var valueProp = prop.GetValueProperty(merged);
                var curveProp = prop.GetCurveProperty(merged);

                EditorGUI.PropertyField(valueRect, valueProp, labelContent);
                DrawCurveFieldWithButton(curveProp, buttonRect, 
                    () => SplitRect(EditorGUILayout.GetControlRect(), OverrideWidth).restRect);

                return valueProp.hasMultipleDifferentValues || curveProp.hasMultipleDifferentValues;
            });
        }

        protected override void PbPermissionProp(string label, PermissionConfigProp prop, bool forceOverride = false)
        {
            PbPropImpl(label, prop, forceOverride, (rect, merged, labelContent) =>
            {
                var valueProp = prop.GetValueProperty(merged);

                EditorGUI.PropertyField(rect, valueProp, labelContent);
                if (valueProp.enumValueIndex == 2)
                {
                    var filterProp = prop.GetFilterProperty(merged);
                    var allowSelf = filterProp.FindPropertyRelative("allowSelf");
                    var allowOthers = filterProp.FindPropertyRelative("allowOthers");
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(allowSelf);
                    EditorGUILayout.PropertyField(allowOthers);
                    EditorGUI.indentLevel--;
                    return valueProp.hasMultipleDifferentValues || filterProp.hasMultipleDifferentValues;
                }
                else
                {
                    return valueProp.hasMultipleDifferentValues;
                }
            });
        }

        protected override void Pb3DCurveProp(string label,
            string pbXCurveLabel, string pbYCurveLabel, string pbZCurveLabel, 
            CurveVector3ConfigProp prop, bool forceOverride = false)
        {
            PbPropImpl(label, prop, forceOverride, (rect, merged, labelContent) =>
            {
                var (valueRect, buttonRect) = SplitRect(rect, CurveButtonWidth);

                var valueProp = prop.GetValueProperty(merged);
                var xCurveProp = prop.GetCurveXProperty(merged);
                var yCurveProp = prop.GetCurveYProperty(merged);
                var zCurveProp = prop.GetCurveZProperty(merged);

                void DrawCurve(string curveLabel, SerializedProperty curveProp)
                {
                    var (curveRect, curveButtonRect, _) = SplitRect(EditorGUILayout.GetControlRect(true),
                        CurveButtonWidth, OverrideWidth);

                    EditorGUI.LabelField(curveRect, curveLabel, " ");
                    DrawCurveFieldWithButton(curveProp, curveButtonRect, () => curveRect);
                }

                if (IsCurveWithValue(xCurveProp) || IsCurveWithValue(yCurveProp) || IsCurveWithValue(zCurveProp))
                {
                    // with curve
                    EditorGUI.PropertyField(valueRect, valueProp, labelContent);
                    DrawCurve(pbXCurveLabel, xCurveProp);
                    DrawCurve(pbYCurveLabel, yCurveProp);
                    DrawCurve(pbZCurveLabel, zCurveProp);
                }
                else
                {
                    // without curve: constant
                    EditorGUI.PropertyField(valueRect, valueProp, labelContent);
                    
                    if (GUI.Button(buttonRect, "C"))
                    {
                        var curve = new AnimationCurve();
                        curve.AddKey(new Keyframe(0.0f, 1f));
                        curve.AddKey(new Keyframe(1f, 1f));
                        xCurveProp.animationCurveValue = curve;
                        yCurveProp.animationCurveValue = curve;
                        zCurveProp.animationCurveValue = curve;
                    }
                }

                return valueProp.hasMultipleDifferentValues
                       || xCurveProp.hasMultipleDifferentValues
                       || yCurveProp.hasMultipleDifferentValues
                       || zCurveProp.hasMultipleDifferentValues;
            });
        }

        private static readonly string[] CopyOverride = { "C:Copy", "O:Override" };

        private void PbPropImpl([NotNull] string label, 
            [NotNull] OverridePropBase prop, 
            bool forceOverride, 
            [NotNull] Func<Rect, bool, GUIContent, bool> renderer)
        {
            var labelContent = new GUIContent(label);

            var (valueRect, overrideRect) =
                SplitRect(EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight), OverrideWidth);

            if (forceOverride || prop.IsOverride)
            {
                // Override mode

                renderer(valueRect, true, labelContent);

                if (forceOverride)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    PopupNoIndent(overrideRect, 1, CopyOverride);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUI.BeginProperty(overrideRect, null, prop.IsOverrideProperty);
                    var selected = PopupNoIndent(overrideRect, 1, CopyOverride);
                    if (selected != 1)
                        prop.IsOverrideProperty.boolValue = false;
                    EditorGUI.EndProperty();
                }
            }
            else
            {
                // Copy mode
                EditorGUI.BeginDisabledGroup(true);
                var differ = renderer(valueRect, false, labelContent);
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginProperty(overrideRect, null, prop.IsOverrideProperty);
                var selected = PopupNoIndent(overrideRect, 0, CopyOverride);
                if (selected != 0)
                    prop.IsOverrideProperty.boolValue = true;
                EditorGUI.EndProperty();

                if (differ)
                {
                    EditorGUILayout.HelpBox(
                        "The value is differ between two or more sources. " +
                        "You have to set same value OR override this property", 
                        MessageType.Error);
                }
            }
        }

        protected override void CollidersProp(string label, CollidersConfigProp prop)
        {
            var labelContent = new GUIContent(label);

            Rect valueRect, overrideRect;

            switch ((MergePhysBone.CollidersConfig.CollidersOverride)prop.OverrideProperty.enumValueIndex)
            {
                case MergePhysBone.CollidersConfig.CollidersOverride.Copy:
                {
                    var colliders = prop.PhysBoneValue;

                    var height = EditorGUI.GetPropertyHeight(colliders, null, true);

                    (valueRect, overrideRect) = SplitRect(EditorGUILayout.GetControlRect(true, height), OverrideWidth);

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.PropertyField(valueRect, colliders, labelContent, true);
                    EditorGUI.EndDisabledGroup();

                    if (colliders.hasMultipleDifferentValues)
                    {
                        EditorGUILayout.HelpBox(
                            "The value is differ between two or more sources. " +
                            "You have to set same value OR override this property",
                            MessageType.Error);
                    }
                }
                    break;
                case MergePhysBone.CollidersConfig.CollidersOverride.Merge:
                {
                    (valueRect, overrideRect) =
                        SplitRect(EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight), OverrideWidth);

                    var colliders = ComponentsSetEditorUtil.Values.SelectMany(x => x.colliders).Distinct().ToList();
                    var mergedProp = prop.ValueProperty;
                    EditorGUI.BeginDisabledGroup(true);
                    mergedProp.isExpanded = EditorGUI.Foldout(valueRect, mergedProp.isExpanded, labelContent);
                    if (mergedProp.isExpanded)
                    {
                        EditorGUILayout.IntField("Size", colliders.Count);
                        for (var i = 0; i < colliders.Count; i++)
                            EditorGUILayout.ObjectField($"Element {i}", colliders[i], typeof(VRCPhysBoneColliderBase),
                                true);
                    }

                    EditorGUI.EndDisabledGroup();
                }
                    break;
                case MergePhysBone.CollidersConfig.CollidersOverride.Override:
                {
                    var colliders = prop.ValueProperty;

                    var height = EditorGUI.GetPropertyHeight(colliders, null, true);

                    (valueRect, overrideRect) = SplitRect(EditorGUILayout.GetControlRect(true, height), OverrideWidth);

                    EditorGUI.PropertyField(valueRect, colliders, labelContent, true);
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            EditorGUI.BeginProperty(overrideRect, null, prop.OverrideProperty);
            var selected = PopupNoIndent(overrideRect, prop.OverrideProperty.enumValueIndex, prop.OverrideProperty.enumDisplayNames);
            if (selected != 0) prop.OverrideProperty.enumValueIndex = selected;
            EditorGUI.EndProperty();
        }

        private static int PopupNoIndent(Rect position, int selectedIndex, string[] displayedOptions)
        {
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            var result = EditorGUI.Popup(position, selectedIndex, displayedOptions);
            EditorGUI.indentLevel = indent;
            return result;
        }
    }

    [InitializeOnLoad]
    sealed class MergePhysBoneValidator : MergePhysBoneEditorModificationUtils
    {
        private readonly List<ErrorLog> _errorLogs;
        private readonly List<string> _differProps = new List<string>();
        private readonly MergePhysBone _mergePhysBone;

        static MergePhysBoneValidator()
        {
            ComponentValidation.RegisterValidator<MergePhysBone>(Validate);
        }

        private static List<ErrorLog> Validate(MergePhysBone mergePhysBone)
        {
            var list = new List<ErrorLog>();
            if (mergePhysBone.makeParent && mergePhysBone.transform.childCount != 0)
                list.Add(ErrorLog.Validation("MergePhysBone:error:makeParentWithChildren", mergePhysBone));

            new MergePhysBoneValidator(list, mergePhysBone).DoProcess();

            return list;
        }

        public MergePhysBoneValidator(List<ErrorLog> errorLogs, MergePhysBone mergePhysBone)
            : base(new SerializedObject(mergePhysBone))
        {
            _errorLogs = errorLogs;
            _mergePhysBone = mergePhysBone;
        }

        private static void Void()
        {
        }

        protected override void BeginPbConfig() => Void();
        protected override bool BeginSection(string name, string docTag) => true;
        protected override void EndSection() => Void();
        protected override void EndPbConfig() {
            if (_differProps.Count != 0)
            {
                _errorLogs.Add(ErrorLog.Validation("MergePhysBone:error:differValues",
                    new[] { string.Join(", ", _differProps) }));
            }
        }

        protected override void NoSource() =>
            _errorLogs.Add(ErrorLog.Validation("MergePhysBone:error:noSources"));

        protected override void TransformSection()
        {
            if (!_mergePhysBone.makeParent)
            {
                var differ = SourcePhysBones
                    .Select(x => x.transform.parent)
                    .ZipWithNext()
                    .Any(x => x.Item1 != x.Item2);
                if (differ)
                    _errorLogs.Add(ErrorLog.Validation("MergePhysBone:error:parentDiffer"));
            }
            var multiChildType = GetSourceProperty(nameof(VRCPhysBoneBase.multiChildType));
            if (multiChildType.enumValueIndex != 0 || multiChildType.hasMultipleDifferentValues)
                _errorLogs.Add(ErrorLog.Validation("MergePhysBone:error:multiChildType"));
        }

        protected override void OptionParameter() => Void();
        protected override void OptionIsAnimated() => Void();

        protected override void UnsupportedPbVersion() =>
            _errorLogs.Add(ErrorLog.Validation("MergePhysBone:error:unsupportedPbVersion"));

        protected override void PbVersionProp(string label, ValueConfigProp prop, bool forceOverride = false)
            => PbProp(label, prop, forceOverride);

        protected override void PbProp(string label, ValueConfigProp prop, bool forceOverride = false)
            => PbPropImpl(label, prop, forceOverride);

        protected override void PbCurveProp(string label, CurveConfigProp prop, bool forceOverride = false)
            => PbPropImpl(label, prop, forceOverride);

        protected override void Pb3DCurveProp(string label, string pbXCurveLabel, string pbYCurveLabel, string pbZCurveLabel,
            CurveVector3ConfigProp prop, bool forceOverride = false)
            => PbPropImpl(label, prop, forceOverride);

        protected override void PbPermissionProp(string label, PermissionConfigProp prop, bool forceOverride = false)
            => PbPropImpl(label, prop, forceOverride);

        private void PbPropImpl(string label, OverridePropBase prop, bool forceOverride)
        {
            if (forceOverride || prop.IsOverride) return;
            
            if (prop.GetActiveProps(false).Any(x => x.Item2.hasMultipleDifferentValues))
                _differProps.Add(label);
        }

        protected override void CollidersProp(string label, CollidersConfigProp prop)
        {
            // 0: copy
            if (prop.OverrideProperty.enumValueIndex == 0)
            {
                if (prop.PhysBoneValue.hasMultipleDifferentValues)
                    _differProps.Add(label);
            }
        }
    }
}
