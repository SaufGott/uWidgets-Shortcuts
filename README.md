# Shortcuts — a uWidgets widget

Runs on [uWidgets](https://github.com/creewick/uWidgets) — a standalone .NET app with
customizable widgets. This is a third-party widget plugin for it, built against the
[widget API](https://github.com/creewick/uWidgets/wiki/API).

A container for desktop shortcuts, grouped into categories. Each category is a modular
slot grid you size with `-` / `+`, entries can be dragged between slots and categories,
files can be dropped in from Explorer, and a lock freezes the layout you built.

It is built in the uWidgets house style: transparent background, theme accent colour,
dashed separators, and — optionally — monochrome icons that follow the global uWidgets
monochrome theme, with a contrast slider per shortcut.

---

## Build

Requires the .NET 8 SDK.

```powershell
dotnet build -c Release
```

The output is `dist\Widgets\Shortcuts.dll` — that single file is the whole plugin.
`uWidgets.Core` is referenced with `Private=false` / `ExcludeAssets=runtime`, so no extra
DLLs are copied: the host already provides that assembly, which keeps type identity intact.

The build reports `AVLN3001 … no public constructor was found` for the two views. That is
expected: the host activates them through DI, so they take constructor arguments and are
never loaded through the XAML runtime loader.

## Install

1. Copy `dist\Widgets\Shortcuts.dll` into uWidgets' `Widgets` folder (the folder next to
   `uWidgets.exe`; open it from the tray menu if you are unsure).
2. Restart uWidgets, add the widget from the widget list.

---

## Getting started

1. **Open the editor.** Right-click the widget and choose Edit "Shortcuts".
2. **Create a category.** Type a name in the *New category* field and press `+`. The category
   appears in the list and as a box on the widget.
3. **Size it.** Use the `-` / `+` next to *Rows* and *Columns* — in the editor or on the widget
   itself while it is unlocked. Each category is its own grid, so they can be different sizes.
4. **Add a shortcut.** Either type a path or a name in the editor and press `+`, or drag a
   `.lnk` / file / folder from Explorer onto an empty slot. A dropped shortcut is copied into
   `Widgets\shortcuts`, so deleting the original on your desktop does not break the entry.
5. **Arrange it.** Drag an entry to another slot (two entries swap if the target is occupied),
   or drag a whole category box onto another to reorder them.
6. **Use it.** A single click on a slot launches the target. Right-click gives Open / Open
   containing folder / Remove. The tooltip shows the shortcut name.
7. **Finish.** Tick *Lock layout* when the arrangement is right: resizing, dragging, adding and
   removing stop, clicking still works. The settings (icon size, padding, gap, edge margin, title
   font size, alignment) are in the same window and apply immediately.

Everything is stored in uWidgets' `layout.json`, so the layout is restored after a restart.

---

## Using it

**Categories.** Each category renders as a column: a header pill with its name, then its own
grid of icon boxes. `Columns` is the number of slots per row, `Rows` is the number of rows.
`-` / `+` change them live. Categories flow left to right and wrap when they run out of
width. While unlocked, grab a category box anywhere on its background and drop it on another
to reorder them.

The layout panel is `Views/Controls/CategoryPanel.cs` rather than `WrapPanel`, for two
reasons: `WrapPanel` stretches every item in a row to the height of the tallest one, so a
category that uses two rows looks like it has four; and the panel centres the block, which is
what makes the spacing equal on all four sides. Note that the widget window itself is sized by
uWidgets — the panel can only distribute the space the window gives it.

**Slots.** An empty slot is a template box with a plus in it — that is the drop target and
the preview of the grid the user built. The plus belongs to edit mode only: once the widget
is locked the boxes stay as the layout preview, the plus disappears. A filled slot shows the
icon centred inside with the configured inset on a 20 % white backdrop, which fades to opaque
white on hover. The image is not hit test visible so that the tile itself is the pointer target
— otherwise hover and click land on the image instead of the box.

**Entries.** Add a shortcut by typing a path or name in the editor, or by dropping a
`.lnk` / file / folder from Explorer onto an empty slot. A dropped `.lnk` is copied into
`Widgets\shortcuts` and the entry points at the copy, so removing the desktop shortcut does not
break it. A single click on a slot launches the target; right-click gives Open / Open containing
folder / Remove. The tooltip shows the shortcut name, not the target path. The tile marks the
left press as handled, because the host's `Widget` window starts a move-drag on any left press —
without that, a click on a tile would move the widget instead of launching the shortcut.

**Drag and drop.** With the widget unlocked, drag a slot onto another slot to move the
entry — if the target slot is occupied, the two entries swap. Drag a category box (anywhere on
its background, or its header pill) onto another category to reorder them. This works in the
widget itself, not only in the Edit window.

**Stored shortcuts.** A dropped `.lnk` is copied into `Widgets\shortcuts` next to the plugin,
and the entry points at that copy. Deleting the original shortcut from the desktop then does not
break the widget. The copy is reused if the same shortcut is dropped again, and a path that
already points at the program itself is not copied. Entries that existed before this change keep
their original paths.

**Lock.** Locking freezes the layout structure: no resizing, no dragging, no adding or
removing. Launching still works. This is the "the layout is finished" state. There is no lock
icon on the widget; the toggle is the checkbox in the Edit window. The `-` / `+` row only appears
while the widget is unlocked, so locking does change the height of a category.

**Margins.** The widget sets no margin of its own. The host applies `Margin="12"` to every
widget `UserControl`, so the inset is identical to every other uWidget. Inside that, the edge
margin and the gap are the values from the Edit window.

**Icons.** They are requested at exactly the size they are drawn into, so the renderer does not
have to resample them — that was the cause of the pixelated look. `PrivateExtractIconsW` is
asked for the configured icon size, and the `Image` uses
`RenderOptions.BitmapInterpolationMode="HighQuality"` for the cases where scaling still happens
(display scaling, or a file whose icon group has no entry near that size). Verified against the
current entries: every `.exe` target returns the requested size.

A shortcut whose target is a shell folder rather than a file has no icon resource to read, so
those fall back to the shell's 32 px system icon and are upscaled — that is the one case that
still looks soft, and getting 48 for it would need COM (`SHGetImageList`), which the plugin
deliberately avoids.

**Layout settings.** The Edit window exposes icon size, the inset inside the icon box, the gap
between boxes, the margin to the widget edge, the title font size, and a 3×3 alignment picker
for where the category block sits in the content area. Values are stored in the model and
survive a restart; an older `layout.json` without them keeps the defaults (40 / 10 / 20 / 0 /
14, centred).

The mapping is: the icon box is `icon size + 2 × icon padding`, and the gap is split as half on
each side of every box, so the visible distance between two boxes is exactly the gap value. The
edge margin is the padding of the content area, so it is not added to the gap.

**Why a press has to be handled.** The host's `Widget` window starts a move-drag on any left
press that is not handled, and saves the position on release — and `OnWidgetLayoutUpdated`
re-creates the whole user control whenever the model changes. So grabbing a tile, a slot or a
category box in edit mode would move the widget, save, rebuild the control and cancel the drag
that was in progress. Every control that owns a drag marks its press as handled while the widget
is unlocked; when it is locked the press passes through to the host as normal.

The same thing applies to `DragOver`: it is a routed event, so a drop over a slot bubbles to the
category around it. Without a `e.Source == this` guard the category overrode the slot's
`DragEffects` to `None`, which is what made Explorer drops stop working.

**Monochrome.** The widget follows the global uWidgets monochrome setting by default. The
editor can override it per widget (Inherit / Force On / Force Off). When monochrome is
active, each icon is converted to its own luminance — grey, not the accent colour — scaled by
that shortcut's contrast slider (0.2 – 2.0).

---

## How it is put together

| Part | Role |
| --- | --- |
| `Models/` | Plain records. The whole widget state is one `ShortcutsModel`, serialised into `layout.json` through `IWidgetLayoutProvider`. |
| `Services/LnkResolver.cs` | Reads the Shell Link header with P/Invoke only (no `System.Drawing`, no COM). Canonical LinkInfo first, then a defensive scan of the data section. Best-effort: launching never depends on it. |
| `Services/ShortcutStore.cs` | Copies a dropped `.lnk` into `Widgets\shortcuts` and returns that path, so an entry does not depend on the original file. Reuses an identical copy, leaves non-shortcut paths alone, falls back to the original path if the copy fails. |
| `Services/NativeIcon.cs` | Asks the file's own icon resource for the size we want (`PrivateExtractIconsW`), falls back to `SHGetFileInfo` which is capped at 32 px. Then `GetIconInfo` → `GetDIBits` → BGRA top-down pixels. Sizes come from `GetObjectW` on the bitmap, because `GetIconInfo` reports the hotspot — half the icon for an icon created at a custom size. Monochrome icons use the mask bitmap as the image. |
| `Services/IconService.cs` | Tints pixels to grey scaled by contrast. Cache keyed `path \| size \| monochrome \| contrast`, where size is the configured icon size. Static, because the host re-creates the control on every save. |
| `Services/Diagnostic.cs` | uWidgets does not catch exceptions from a widget constructor, so one failure takes down the whole application. Per-entry work is guarded and written to `Shortcuts_error.txt` next to the plugin instead. |
| `Views/Controls/` | `Border` subclasses that handle drag-over / drop, plus the right-click `MenuFlyout` on a tile. `CategoryPanel` is a custom `Panel`: items keep their own height and the block is placed by the alignment setting. |

The management UI is the standard "Edit Widget" window; the widget itself only shows the
minimal inline controls (`-` / `+` and the lock) while it is unlocked.

---

## Design notes

The layout follows `design-reference/design-reference-example.png` and the Figma file
(`Uwidgets_Shortcut`, node `1:8`): 10 px gaps and 10 px edges, rounded category containers,
header pill with the category name, plus placeholders as drop targets, 20 % white overlay
fills. Slot size is 50 px rather than the 36 px in the mockup, at the user's request; the
spacing is unchanged, so a slot is 50 px with the icon centred inside at 40 px.

The defaults are icon 40 px with a 10 px inset, 20 px between boxes, no edge margin, title font
14 px, block centred. The gap is applied as half on each side of a box, so two adjacent boxes
end up exactly `gap` apart, and the edge margin is the content area's own padding. All of it is
settable in the Edit window.

Inside a category every distance is the same value, the gap: the container padding plus each
slot's own half-margin add up to `gap` from the box edge to the first icon, and the same between
two icons. The header pill carries that inset on the top and the sides but not the bottom — the
stack spacing already provides the gap below it, and a bottom margin would count it twice, which
is what made the space under the title look too large. The pill therefore ends up exactly as wide
as the columns.

The widget no longer draws a title row or the dotted line under it — the block starts at the
content edge. The `Title` field is still in the model and in the editor, but nothing renders it
anymore.

The one thing kept that the mockup does not show is the row / column steppers: they are the
`-` / `+` controls the layout needs, and they only appear while the widget is unlocked.

Category containers, the header pill and an empty slot use the fixed `rgba(255,255,255,0.2)`
overlay from the mockup, which is the uWidgets look. The plus and the fallback glyph use the
theme tokens (`SystemControlForegroundBaseHighBrush`) so they still read correctly in the light
theme. The hover change on an icon box is a `BrushTransition` (150 ms with a 50 ms delay), so
it fades instead of snapping.

A type selector matches the exact type only — `Border.cell` does not match a control derived
from `Border`. The slot, tile and header are custom `Border` subclasses, so their styles name
the actual type (`controls|SlotBorder`, `controls|TileBorder`, `controls|CategoryHeaderBorder`).
With `Border.cell` the cells silently collapsed to the size of their content: no margin, no
padding, no hover.

---

## Known limits

- Below 140 px width the inline controls are hidden; the grid itself does not reflow.
- Entries beyond a category's slot count are not rendered in the grid. They stay in the
  model and remain visible in the editor — grow the grid to see them.
- Dropping from Explorer depends on the platform drag source being active in the host. If
  it does not work in your build, use the Add path in the editor.
- English only.

## Acceptance checklist

No test harness exists in uWidgets, so this is the manual pass:

1. Add a widget, create two categories, resize one to 3×2 with `-` / `+`.
2. Add an entry by path; the tile shows the real icon and launches on click.
3. Drop a `.lnk` from Explorer into an empty slot — it is added.
4. Drag a tile into an occupied slot — the two entries swap.
5. Drag a category header onto another — the order changes and survives a restart.
6. Lock from the checkbox in the Edit window: resizing, dragging, adding and removing are
   blocked; clicking still launches. Unlock there and the controls come back.
7. Set monochrome to Force On: icons become greyscale; the per-entry contrast slider
   changes them live after the slider settles.
8. Set monochrome to Inherit: the widget follows the global setting.
9. Set the icon size, gap, edge margin, padding, font size and alignment in the Edit window:
   the widget updates immediately and the values are restored after a restart.
10. Restart uWidgets — categories, entries, grid sizes, contrast values and lock state are
   restored from `layout.json`.

---

## How this was made

This widget was vibe-coded with a local AI model, in conversation with the author over the course
of the build: the design reference and the Figma file were given to the model, and each round of
feedback (spacing, hover, icon size, the settings panel, the drag-and-drop bugs) turned into the
change described above. The design notes in this README are the record of that process — they
document why each decision was made and which mistakes were found, including the ones the model
made and had to fix.

The model ran locally, so no data left the machine.
