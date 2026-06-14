using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Html5Build.Editor
{
    // ─────────────────────────────────────────────────────────────────────────
    // Unity RectTransform → CSS calc() conversion
    //
    // CSS calc() pattern:  calc(anchor% + offset_px)
    //
    // Left:   calc( anchorMin.x*100 %  +  (anchoredPos.x - sizeDelta.x * pivot.x) px )
    // Top:    calc( (1-anchorMax.y)*100 %  +  (-anchoredPos.y - sizeDelta.y*(1-pivot.y)) px )
    //         ↑ Y flipped (Unity Y-up → CSS Y-down)
    // Width:  calc( (anchorMax.x-anchorMin.x)*100 %  +  sizeDelta.x px )
    // Height: calc( (anchorMax.y-anchorMin.y)*100 %  +  sizeDelta.y px )
    //
    // Rules:
    //  • Full stretch (anchorMin==0,anchorMax==1, sizeDelta==0) → left:0;top:0;width:100%;height:100%
    //  • Fixed size (anchorMin==anchorMax, sizeDelta==explicit size) → left:Npx; top:Npx; width:Wpx; height:Hpx
    //  • Mixed stretch → calc() with both % and px parts
    //
    // Scale & Rotation:
    //  • transform: scale(sx,sy) rotate(Zdeg)
    //  • CSS pixels are in LOCAL (unscaled) space; CSS transform applies visually on top
    //  • pivot → transform-origin
    //  • Rotation negated (Unity CCW = CSS CW, so −Unity = CSS)
    // ─────────────────────────────────────────────────────────────────────────
    public static class SceneReader
    {
        private static int _refW, _refH;
        private static readonly Dictionary<GameObject, string> _elementIds =
            new Dictionary<GameObject, string>();

        public static CanvasModel ReadActiveScene()
        {
            var roots = FindRootCanvases();
            if (roots.Count == 0)
                throw new Exception("No root Canvas found in the active scene.");

            foreach (var root in roots)
                LayoutRebuilder.ForceRebuildLayoutImmediate(root.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();

            BuildElementIds(roots);

            // Reference resolution from the first canvas that has a CanvasScaler
            _refW = 1080; _refH = 1920;
            foreach (var root in roots)
            {
                var scaler = root.GetComponent<CanvasScaler>();
                if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    _refW = Mathf.RoundToInt(scaler.referenceResolution.x);
                    _refH = Mathf.RoundToInt(scaler.referenceResolution.y);
                    break;
                }
            }

            // Background color = first Image child found across all canvases
            Color bg = new Color(0.08f, 0.08f, 0.08f);
            bool  bgFound = false;
            foreach (var root in roots)
            {
                if (bgFound) break;
                foreach (Transform child in root.transform)
                {
                    var img = child.GetComponent<Image>();
                    if (img != null) { bg = img.color; bgFound = true; break; }
                }
            }

            var model = new CanvasModel
            {
                GameName        = Application.productName,
                ReferenceWidth  = _refW,
                ReferenceHeight = _refH,
                BackgroundColor = bg,
            };

            // Read children from ALL root canvases.
            // Canvases beyond the first get a prefix (c1_, c2_, …) so their HTML IDs are unique.
            for (int ci = 0; ci < roots.Count; ci++)
            {
                string prefix = ci == 0 ? "" : $"c{ci}_";
                foreach (Transform child in roots[ci].transform)
                    ReadElement(child, model.Children, prefix);
            }

            return model;
        }

        // ─────────────────────────────────────────────────────────────────────
        private static void ReadElement(Transform t, List<UiElement> list, string prefix = "")
        {
            var rt = t.GetComponent<RectTransform>();
            if (rt == null) return;

            var el = new UiElement
            {
                Id               = _elementIds[t.gameObject],
                Name             = prefix + t.gameObject.name,
                Active           = t.gameObject.activeSelf,
                AnchorMin        = rt.anchorMin,
                AnchorMax        = rt.anchorMax,
                AnchoredPosition = rt.anchoredPosition,
                SizeDelta        = rt.sizeDelta,
                Pivot            = rt.pivot,
                PivotForOrigin   = rt.pivot,
            };

            // ── CSS position/size via anchor calc() formulas ──────────────────
            // These formulas map Unity's RectTransform exactly to CSS,
            // and are RESPONSIVE because anchor % references the parent's size.
            el.CssLeft   = CalcLeft(rt);
            el.CssTop    = CalcTop(rt);
            el.CssWidth  = CalcWidth(rt);
            el.CssHeight = CalcHeight(rt);

            // ── Scale & Rotation ──────────────────────────────────────────────
            el.ScaleX = rt.localScale.x;
            el.ScaleY = rt.localScale.y;

            float rotZ = rt.localEulerAngles.z;
            if (rotZ > 180f) rotZ -= 360f;   // normalize to [-180, 180]
            el.Rotation = rotZ;               // stored as Unity value; CSS will negate

            // ── Component type ────────────────────────────────────────────────
            var imgComp    = t.GetComponent<Image>();
            var rawImgComp = t.GetComponent<RawImage>();
            var btnComp    = t.GetComponent<Button>();
            var sliderComp = t.GetComponent<Slider>();
            var toggleComp = t.GetComponent<Toggle>();
            var dropdownComp = t.GetComponent<Dropdown>();
            var tmpDropdown = t.GetComponent<TMP_Dropdown>();
            var inputComp = t.GetComponent<InputField>();
            var tmpInput = t.GetComponent<TMP_InputField>();
            var scrollComp = t.GetComponent<ScrollRect>();
            var tmpText    = t.GetComponent<TMP_Text>();
            var legacyText = t.GetComponent<Text>();

            if (tmpInput != null || inputComp != null) el.Type = "input";
            else if (sliderComp != null) el.Type = "slider";
            else if (toggleComp != null) el.Type = "toggle";
            else if (tmpDropdown != null || dropdownComp != null) el.Type = "dropdown";
            else if (scrollComp != null) el.Type = "scroll";
            else if (btnComp    != null) el.Type = "button";
            else if (tmpText    != null || legacyText != null) el.Type = "text";
            else if (imgComp != null || rawImgComp != null) el.Type = "image";
            else                       el.Type = "container";

            el.IsRectMask = t.GetComponent<RectMask2D>() != null;
            el.IsMask = t.GetComponent<Mask>() != null;

            var canvasGroup = t.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                el.CanvasGroupAlpha = canvasGroup.alpha;
                el.CanvasGroupInteractable = canvasGroup.interactable;
                el.CanvasGroupBlocksRaycasts = canvasGroup.blocksRaycasts;
            }

            // ── Visuals ───────────────────────────────────────────────────────
            if (imgComp != null)
            {
                el.HasImage   = true;
                el.Raycast    = imgComp.raycastTarget;
                el.Color      = imgComp.color;
                el.ImageType  = imgComp.type;
                el.FillMethod = imgComp.fillMethod;
                el.FillAmount = imgComp.fillAmount;
                el.FillOrigin = imgComp.fillOrigin;
                el.Clockwise  = imgComp.fillClockwise;

                if (imgComp.sprite != null)
                {
                    string path = AssetDatabase.GetAssetPath(imgComp.sprite);
                    if (!string.IsNullOrEmpty(path))
                    {
                        el.SpritePath   = path;
                        el.SpriteBorder  = imgComp.sprite.border; // x=left, y=bottom, z=right, w=top
                        el.SpriteWidth   = imgComp.sprite.rect.width;
                        el.SpriteHeight  = imgComp.sprite.rect.height;
                    }
                }
            }
            else if (rawImgComp != null)
            {
                el.HasImage = true;
                el.Raycast = rawImgComp.raycastTarget;
                el.Color = rawImgComp.color;
                el.ImageType = Image.Type.Simple;

                if (rawImgComp.texture != null)
                    el.SpritePath = AssetDatabase.GetAssetPath(rawImgComp.texture);
            }

            // ── Input fields ────────────────────────────────────────────────
            if (tmpInput != null)
            {
                el.InputText = tmpInput.text;
                el.InputPlaceholder = (tmpInput.placeholder as TMP_Text)?.text ?? "";
                el.InputCharacterLimit = tmpInput.characterLimit;
                el.InputReadOnly = tmpInput.readOnly;
                el.InputMultiline = tmpInput.lineType != TMP_InputField.LineType.SingleLine;
                el.InputPassword = tmpInput.contentType == TMP_InputField.ContentType.Password
                    || tmpInput.contentType == TMP_InputField.ContentType.Pin;
                el.InputContentType = InputContentType(tmpInput.contentType);
                el.Raycast = tmpInput.interactable;

                if (tmpInput.textComponent != null)
                {
                    el.FontSize = tmpInput.textComponent.fontSize;
                    el.TextColor = tmpInput.textComponent.color;
                    ReadTmpAlign(tmpInput.textComponent.alignment, out el.TextAlignH, out el.TextAlignV);
                }

                ReadPersistentCalls(tmpInput, "m_OnValueChanged", el.OnValueChangedCalls, prefix);
                ReadPersistentCalls(tmpInput, "m_OnEndEdit", el.OnEndEditCalls, prefix);
            }
            else if (inputComp != null)
            {
                el.InputText = inputComp.text;
                el.InputPlaceholder = inputComp.placeholder is Text p ? p.text : "";
                el.InputCharacterLimit = inputComp.characterLimit;
                el.InputReadOnly = inputComp.readOnly;
                el.InputMultiline = inputComp.lineType != InputField.LineType.SingleLine;
                el.InputPassword = inputComp.contentType == InputField.ContentType.Password
                    || inputComp.contentType == InputField.ContentType.Pin;
                el.InputContentType = InputContentType(inputComp.contentType);
                el.Raycast = inputComp.interactable;

                if (inputComp.textComponent != null)
                {
                    el.FontSize = inputComp.textComponent.fontSize;
                    el.TextColor = inputComp.textComponent.color;
                    el.TextAlignH = LegacyAlignH(inputComp.textComponent.alignment);
                    el.TextAlignV = LegacyAlignV(inputComp.textComponent.alignment);
                }

                ReadPersistentCalls(inputComp, "m_OnValueChanged", el.OnValueChangedCalls, prefix);
                ReadPersistentCalls(inputComp, "m_OnEndEdit", el.OnEndEditCalls, prefix);
            }

            // ── Slider / Toggle / Dropdown / ScrollRect ─────────────────────
            if (sliderComp != null)
            {
                el.SliderMin = sliderComp.minValue;
                el.SliderMax = sliderComp.maxValue;
                el.SliderValue = sliderComp.value;
                el.SliderWholeNumbers = sliderComp.wholeNumbers;
                el.SliderDirection = (int)sliderComp.direction;
                el.Raycast = sliderComp.interactable;

                var fillImage = sliderComp.fillRect != null
                    ? sliderComp.fillRect.GetComponent<Image>()
                    : null;
                if (fillImage != null) el.SliderFillColor = fillImage.color;
                ReadPersistentCalls(sliderComp, "m_OnValueChanged", el.OnValueChangedCalls, prefix);
            }

            if (toggleComp != null)
            {
                el.ToggleIsOn = toggleComp.isOn;
                el.ToggleInteractable = toggleComp.interactable;
                el.Raycast = toggleComp.interactable;
                ReadPersistentCalls(toggleComp, "onValueChanged", el.OnValueChangedCalls, prefix);
            }

            if (tmpDropdown != null)
            {
                el.DropdownValue = tmpDropdown.value;
                el.DropdownInteractable = tmpDropdown.interactable;
                foreach (var option in tmpDropdown.options)
                    el.DropdownOptions.Add(option.text);
                el.Raycast = tmpDropdown.interactable;
                ReadPersistentCalls(tmpDropdown, "m_OnValueChanged", el.OnValueChangedCalls, prefix);
            }
            else if (dropdownComp != null)
            {
                el.DropdownValue = dropdownComp.value;
                el.DropdownInteractable = dropdownComp.interactable;
                foreach (var option in dropdownComp.options)
                    el.DropdownOptions.Add(option.text);
                el.Raycast = dropdownComp.interactable;
                ReadPersistentCalls(dropdownComp, "m_OnValueChanged", el.OnValueChangedCalls, prefix);
            }

            if (scrollComp != null)
            {
                el.ScrollHorizontal = scrollComp.horizontal;
                el.ScrollVertical = scrollComp.vertical;
                el.ScrollSensitivity = scrollComp.scrollSensitivity;
                el.Raycast = true;
            }

            // ── HtmlAction ────────────────────────────────────────────────────
            // Read via SerializedObject — avoids a hard compile-time reference to
            // the game assembly (HtmlAction lives in Assembly-CSharp, not here).
            foreach (var comp in t.GetComponents<UnityEngine.Component>())
            {
                if (comp == null || comp.GetType().Name != "HtmlAction") continue;
                var compSo = new UnityEditor.SerializedObject(comp);
                var prop   = compSo.FindProperty("action");
                if (prop != null && prop.propertyType == UnityEditor.SerializedPropertyType.String
                    && !string.IsNullOrEmpty(prop.stringValue))
                    el.HtmlAction = prop.stringValue.Trim();
                break;
            }

            // ── Tweening ──────────────────────────────────────────────────────
            foreach (var comp in t.GetComponents<UnityEngine.Component>())
            {
                if (comp == null || comp.GetType().Name != "Tweening") continue;
                el.TweenConfig = ReadTweenConfig(comp);
                break;
            }

            // ── Button state colors + OnClick wiring ─────────────────────────
            if (btnComp != null)
            {
                var cols = btnComp.colors;
                el.BtnNormalColor      = cols.normalColor;
                el.BtnHighlightedColor = cols.highlightedColor;
                el.BtnPressedColor     = cols.pressedColor;
                el.BtnDisabledColor    = cols.disabledColor;
                el.BtnColorMultiplier  = cols.colorMultiplier;
                el.BtnFadeDuration     = cols.fadeDuration;
                el.BtnInteractable     = btnComp.interactable;

                // Read persistent OnClick calls via SerializedObject so we get
                // method name, target object name, and all argument values.
                var so    = new UnityEditor.SerializedObject(btnComp);
                var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                if (calls != null)
                {
                    for (int i = 0; i < calls.arraySize; i++)
                    {
                        var c     = calls.GetArrayElementAtIndex(i);
                        int state = c.FindPropertyRelative("m_CallState").intValue;
                        if (state == 1) continue; // Off — skip disabled listeners

                        var call = new OnClickCall
                        {
                            MethodName = c.FindPropertyRelative("m_MethodName").stringValue,
                            Mode       = c.FindPropertyRelative("m_Mode").intValue,
                            StringArg  = c.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue,
                            BoolArg    = c.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue,
                            IntArg     = c.FindPropertyRelative("m_Arguments.m_IntArgument").intValue,
                            FloatArg   = c.FindPropertyRelative("m_Arguments.m_FloatArgument").floatValue,
                        };

                        var targetObj = c.FindPropertyRelative("m_Target").objectReferenceValue;
                        if (targetObj != null)
                        {
                            var targetGo = targetObj is GameObject go
                                ? go
                                : ((Component)targetObj).gameObject;
                            string rawTarget = targetGo.name;
                            // Prefix SetActive targets so they resolve to the correct canvas's element
                            call.TargetName = call.MethodName == "SetActive"
                                && _elementIds.TryGetValue(targetGo, out string targetId)
                                    ? targetId
                                    : (!string.IsNullOrEmpty(prefix) ? prefix + rawTarget : rawTarget);

                            // For methods with no arg (e.g. LinkButton.Open), read
                            // the component's serialized 'url' field as the string arg.
                            if (string.IsNullOrEmpty(call.StringArg) && targetObj is Component comp)
                            {
                                var compSo  = new UnityEditor.SerializedObject(comp);
                                var urlProp = compSo.FindProperty("url");
                                if (urlProp != null && urlProp.propertyType == UnityEditor.SerializedPropertyType.String)
                                    call.StringArg = urlProp.stringValue;
                            }
                        }

                        if (!string.IsNullOrEmpty(call.MethodName))
                            el.OnClickCalls.Add(call);
                    }
                }
            }

            // ── Text ──────────────────────────────────────────────────────────
            // fontSize is in TMP local units. CSS transform:scale() of parent
            // handles visual scaling automatically — no manual scale needed here.
            if (tmpText != null)
            {
                el.TextContent = tmpText.text;
                el.TextColor   = tmpText.color;
                el.FontSize    = tmpText.fontSize;
                el.Raycast     = tmpText.raycastTarget;
                ReadTmpAlign(tmpText.alignment, out el.TextAlignH, out el.TextAlignV);
            }
            else if (legacyText != null)
            {
                el.TextContent = legacyText.text;
                el.TextColor   = legacyText.color;
                el.FontSize    = legacyText.fontSize;
                el.Raycast     = legacyText.raycastTarget;
                el.TextAlignH  = LegacyAlignH(legacyText.alignment);
                el.TextAlignV  = LegacyAlignV(legacyText.alignment);
            }

            // Button overrides raycast — Button component itself is always interactive
            if (btnComp != null) el.Raycast = btnComp.interactable;

            list.Add(el);

            // Recurse — pass prefix down so all descendants stay namespaced
            foreach (Transform child in t)
                ReadElement(child, el.Children, prefix);
        }

        // ─────────────────────────────────────────────────────────────────────
        // calc() formula builders
        // ─────────────────────────────────────────────────────────────────────

        // left = anchorMin.x * parentW  +  (anchoredPos.x - sizeDelta.x * pivot.x)
        private static string CalcLeft(RectTransform rt) =>
            Calc(rt.anchorMin.x * 100f,
                 rt.anchoredPosition.x - rt.sizeDelta.x * rt.pivot.x);

        // top (CSS Y-down) = (1 - anchorMax.y) * parentH  +  (-anchoredPos.y - sizeDelta.y*(1-pivot.y))
        // The (1-anchorMax.y) flips Unity Y-up → CSS Y-down
        private static string CalcTop(RectTransform rt) =>
            Calc((1f - rt.anchorMax.y) * 100f,
                 -rt.anchoredPosition.y - rt.sizeDelta.y * (1f - rt.pivot.y));

        // width = (anchorMax.x - anchorMin.x) * parentW  +  sizeDelta.x
        private static string CalcWidth(RectTransform rt) =>
            Calc((rt.anchorMax.x - rt.anchorMin.x) * 100f, rt.sizeDelta.x);

        // height = (anchorMax.y - anchorMin.y) * parentH  +  sizeDelta.y
        private static string CalcHeight(RectTransform rt) =>
            Calc((rt.anchorMax.y - rt.anchorMin.y) * 100f, rt.sizeDelta.y);

        // Produce a responsive CSS value.
        // % part = anchor fraction (auto-responsive via parent size)
        // px part = offset in reference-resolution pixels, scaled by --rs so it
        //           shrinks/grows with the viewport alongside the % anchor.
        private static string Calc(float pct, float px)
        {
            bool hasPct = Mathf.Abs(pct) > 0.01f;
            bool hasPx  = Mathf.Abs(px)  > 0.01f;

            if (!hasPct && !hasPx) return "0px";
            // Pure percentage — fully responsive, no --rs needed
            if (!hasPx)  return $"{pct:F1}%";
            // Pure pixel — scale with --rs
            if (!hasPct) return $"calc({px:F1}px * var(--rs))";
            // Mixed: anchor% + scaled offset
            string sign = px >= 0f ? "+" : "-";
            return $"calc({pct:F1}% {sign} {Mathf.Abs(px):F1}px * var(--rs))";
        }

        // ─────────────────────────────────────────────────────────────────────
        // TMP / Legacy Text alignment helpers
        // ─────────────────────────────────────────────────────────────────────
        private static void ReadTmpAlign(TextAlignmentOptions a, out string h, out string v)
        {
            int f = (int)a;
            h = (f & 0xFF) switch { 1 => "flex-start", 2 => "center", 4 => "flex-end", _ => "center" };
            v = ((f >> 8) & 0xFF) switch { 1 => "flex-start", 2 => "center", 4 => "flex-end", _ => "center" };
        }

        private static string LegacyAlignH(TextAnchor a) =>
            (a == TextAnchor.UpperLeft || a == TextAnchor.MiddleLeft || a == TextAnchor.LowerLeft) ? "flex-start" :
            (a == TextAnchor.UpperRight || a == TextAnchor.MiddleRight || a == TextAnchor.LowerRight) ? "flex-end" : "center";

        private static string LegacyAlignV(TextAnchor a) =>
            (a == TextAnchor.UpperLeft || a == TextAnchor.UpperCenter || a == TextAnchor.UpperRight) ? "flex-start" :
            (a == TextAnchor.LowerLeft || a == TextAnchor.LowerCenter || a == TextAnchor.LowerRight) ? "flex-end" : "center";

        private static string InputContentType(TMP_InputField.ContentType type)
        {
            switch (type)
            {
                case TMP_InputField.ContentType.IntegerNumber:
                case TMP_InputField.ContentType.DecimalNumber:
                    return "number";
                case TMP_InputField.ContentType.EmailAddress:
                    return "email";
                default:
                    return "text";
            }
        }

        private static string InputContentType(InputField.ContentType type)
        {
            switch (type)
            {
                case InputField.ContentType.IntegerNumber:
                case InputField.ContentType.DecimalNumber:
                    return "number";
                case InputField.ContentType.EmailAddress:
                    return "email";
                default:
                    return "text";
            }
        }

        private static void ReadPersistentCalls(
            Component component,
            string eventProperty,
            List<OnClickCall> output,
            string prefix)
        {
            var so = new SerializedObject(component);
            var calls = so.FindProperty(eventProperty + ".m_PersistentCalls.m_Calls");
            if (calls == null) return;

            for (int i = 0; i < calls.arraySize; i++)
            {
                var c = calls.GetArrayElementAtIndex(i);
                int state = c.FindPropertyRelative("m_CallState").intValue;
                if (state == 1) continue;

                var call = new OnClickCall
                {
                    MethodName = c.FindPropertyRelative("m_MethodName").stringValue,
                    Mode = c.FindPropertyRelative("m_Mode").intValue,
                    StringArg = c.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue,
                    BoolArg = c.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue,
                    IntArg = c.FindPropertyRelative("m_Arguments.m_IntArgument").intValue,
                    FloatArg = c.FindPropertyRelative("m_Arguments.m_FloatArgument").floatValue,
                };

                var targetObj = c.FindPropertyRelative("m_Target").objectReferenceValue;
                if (targetObj != null)
                {
                    var targetGo = targetObj is GameObject go
                        ? go
                        : ((Component)targetObj).gameObject;
                    call.TargetName = call.MethodName == "SetActive"
                        && _elementIds.TryGetValue(targetGo, out string targetId)
                            ? targetId
                            : (!string.IsNullOrEmpty(prefix) ? prefix + targetGo.name : targetGo.name);
                }

                if (!string.IsNullOrEmpty(call.MethodName))
                    output.Add(call);
            }
        }

        private static List<Canvas> FindRootCanvases()
        {
            var result = new List<Canvas>();
            foreach (var c in UnityEngine.Object.FindObjectsOfType<Canvas>())
                if (c.isRootCanvas) result.Add(c);
            // Sort by sibling index so the order matches the Unity hierarchy
            result.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            return result;
        }

        private static void BuildElementIds(List<Canvas> roots)
        {
            _elementIds.Clear();
            var nameCounts = new Dictionary<string, int>();

            for (int ci = 0; ci < roots.Count; ci++)
            {
                string prefix = ci == 0 ? "" : $"c{ci}_";
                foreach (var rt in roots[ci].GetComponentsInChildren<RectTransform>(true))
                {
                    if (rt == roots[ci].transform) continue;
                    string key = prefix + rt.gameObject.name;
                    nameCounts.TryGetValue(key, out int count);
                    nameCounts[key] = count + 1;
                }
            }

            for (int ci = 0; ci < roots.Count; ci++)
            {
                string prefix = ci == 0 ? "" : $"c{ci}_";
                foreach (var rt in roots[ci].GetComponentsInChildren<RectTransform>(true))
                {
                    if (rt == roots[ci].transform) continue;
                    string key = prefix + rt.gameObject.name;
                    string id = nameCounts[key] == 1
                        ? SafeId(key)
                        : SafeId(prefix + HierarchyPath(rt, roots[ci].transform));
                    _elementIds[rt.gameObject] = id;
                }
            }
        }

        private static string HierarchyPath(Transform t, Transform root)
        {
            var parts = new List<string>();
            while (t != null && t != root)
            {
                parts.Add($"{t.gameObject.name}_{t.GetSiblingIndex()}");
                t = t.parent;
            }
            parts.Reverse();
            return string.Join("__", parts);
        }

        private static string SafeId(string value) =>
            string.IsNullOrEmpty(value)
                ? "el"
                : System.Text.RegularExpressions.Regex.Replace(value, @"[^a-zA-Z0-9_\-]", "_");

        // ── Tweening component reader ─────────────────────────────────────────
        private static TweenConfig ReadTweenConfig(Component comp)
        {
            var so  = new SerializedObject(comp);
            var cfg = new TweenConfig
            {
                AutoPlay        = so.FindProperty("autoPlay")?.boolValue        ?? false,
                NumberAnimArray = so.FindProperty("numberAnimArray")?.intValue  ?? -1,
                Loop            = so.FindProperty("loop")?.boolValue            ?? false,
                Yoyo            = so.FindProperty("yoyo")?.boolValue            ?? false,
                Repeat          = so.FindProperty("repeat")?.intValue           ?? 0,
            };

            var tweenDataProp = so.FindProperty("tweenData");
            if (tweenDataProp == null) return cfg;

            for (int i = 0; i < tweenDataProp.arraySize; i++)
            {
                var dataEl = tweenDataProp.GetArrayElementAtIndex(i);
                var item   = new TweenDataItem
                {
                    Type       = ComponentTypeToStr(dataEl.FindPropertyRelative("type")?.intValue ?? 0),
                    StartPoint = dataEl.FindPropertyRelative("startPoint")?.vector3Value ?? Vector3.zero,
                    Loop       = dataEl.FindPropertyRelative("loop")?.boolValue          ?? false,
                    Yoyo       = dataEl.FindPropertyRelative("yoyo")?.boolValue          ?? false,
                };

                var seqProp = dataEl.FindPropertyRelative("sequence");
                if (seqProp != null)
                {
                    for (int j = 0; j < seqProp.arraySize; j++)
                    {
                        var p = seqProp.GetArrayElementAtIndex(j);
                        item.Sequence.Add(new TweenPathItem
                        {
                            Duration = p.FindPropertyRelative("duration")?.floatValue   ?? 1f,
                            Delay    = p.FindPropertyRelative("delay")?.floatValue      ?? 0f,
                            Ease     = EaseIntToStr(p.FindPropertyRelative("ease")?.intValue ?? 0),
                            WayPoint = p.FindPropertyRelative("wayPoint")?.vector3Value ?? Vector3.zero,
                        });
                    }
                }

                cfg.TweenData.Add(item);
            }

            return cfg;
        }

        // ComponentType enum order: Position=0, Rotation=1, Scale=2, Opacity=3, CamShake=4, PositionUI=5, TextFade=6
        private static readonly string[] _compTypes =
            { "Position", "Rotation", "Scale", "Opacity", "CamShake", "PositionUI", "TextFade" };
        private static string ComponentTypeToStr(int i) =>
            (i >= 0 && i < _compTypes.Length) ? _compTypes[i] : "Position";

        // DOTween Ease underlying int values (Linear=0, InSine=1, OutSine=2 … OutBounce=29 …)
        private static readonly string[] _easeNames =
        {
            "Linear","InSine","OutSine","InOutSine","InQuad","OutQuad","InOutQuad",
            "InCubic","OutCubic","InOutCubic","InQuart","OutQuart","InOutQuart",
            "InQuint","OutQuint","InOutQuint","InExpo","OutExpo","InOutExpo",
            "InCirc","OutCirc","InOutCirc","InElastic","OutElastic","InOutElastic",
            "InBack","OutBack","InOutBack","InBounce","OutBounce","InOutBounce",
            "Flash","Flash","Flash","Linear","Linear","Flash"
        };
        private static string EaseIntToStr(int i) =>
            (i >= 0 && i < _easeNames.Length) ? _easeNames[i] : "Linear";
    }
}
