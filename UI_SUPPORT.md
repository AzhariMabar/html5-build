# Unity UI Support

## Supported

| Unity component | HTML output | Notes |
| --- | --- | --- |
| `RectTransform` | positioned element | Anchors, pivot, size, scale, rotation |
| `Image` | `div` / background / `img` | Simple, Sliced, Tiled, Filled |
| `RawImage` | `img` background | Texture, color, raycast |
| `TMP_Text` / `Text` | text `div` | Content, color, size, alignment |
| `Button` | `button` | Interactable, color states, `OnClick` |
| `TMP_InputField` / `InputField` | `input` / `textarea` | Value, placeholder, content type, character limit, read-only |
| `Slider` | `input[type=range]` | Min, max, value, whole numbers, direction |
| `Toggle` | checkbox | Value, interactable |
| `TMP_Dropdown` / `Dropdown` | `select` | Options, selected value, interactable |
| `ScrollRect` | scroll container | Horizontal/vertical scrolling |
| `RectMask2D` / `Mask` | CSS clipping | Rectangular clipping |
| `CanvasGroup` | CSS opacity/input state | Alpha, interactable, blocks raycasts |
| `LayoutGroup` | baked RectTransforms | Unity rebuilds layout before export |
| `ContentSizeFitter` | baked RectTransform | Unity rebuilds layout before export |
| `LayoutElement` | baked RectTransform | Applied by Unity layout calculation |
| `HtmlAction` | JavaScript action | Panel, URL, or raw JS escape hatch |
| `Tweening` | generated tween | Existing custom component |

`OnValueChanged` is exported for input fields, sliders, toggles, and dropdowns.
`OnEndEdit` is exported for input fields.

## Partial

- TMP rich-text tags are exported as plain text.
- Custom fonts are not copied yet; output uses Arial.
- Slider, toggle, and dropdown use native browser controls, so their appearance
  is functional but not pixel-identical to custom Unity sprite states.
- `Mask` is implemented as rectangular CSS clipping, not sprite-alpha masking.
- Layout components are baked at the reference resolution. Runtime CSS does not
  recalculate Unity's layout algorithm.

## Not Yet Supported

- `Scrollbar` as a standalone control;
- `Selectable` sprite-swap and animation transitions;
- TMP fallback fonts, material presets, outline, glow, and underlay;
- non-rectangular alpha masks;
- `AspectRatioFitter` runtime recalculation;
- `Canvas` world-space and screen-space camera modes;
- navigation between controls using Unity's `Navigation` graph.
