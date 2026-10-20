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

## Known limits

- Below 140 px width the inline controls are hidden; the grid itself does not reflow.
- Entries beyond a category's slot count are not rendered in the grid. They stay in the
  model and remain visible in the editor — grow the grid to see them.
- Dropping from Explorer depends on the platform drag source being active in the host. If
  it does not work in your build, use the Add path in the editor.
- English only.

---

## How this was made

This widget was vibe-coded with a local AI model, in conversation with the author over the course
of the build: a layout reference as a Figma file was given to the model, and each round of
feedback (spacing, hover, icon size, the settings panel, the drag-and-drop bugs) turned into the
change described above. The design notes in this README are the record of that process — they
document why each decision was made and which mistakes were found, including the ones the model
made and had to fix.
