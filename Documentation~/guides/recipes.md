# Recipes

Everything you can do from the Selection window can also be done from your own editor scripts. This page shows how to get started and covers the most common use cases. For the complete list of members, check the API reference.

## Before you start

- This package is **editor-only**: its classes can only be used from editor code (scripts in an `Editor/` folder, an editor assembly, or code wrapped in `#if UNITY_EDITOR`).
- Its assembly, `SideXP.Unity.Selection`, is referenced automatically by the default editor assembly. If your code lives in your own assembly definition, add `SideXP.Unity.Selection` to its references.
- All the types are declared in the `SideXP.Selection` namespace.

The two entry points are:

- [`SelectionHistory`](https://side-xp.github.io/unity-selection/api/SideXP.Selection/SelectionHistory): navigates through the recorded selections
- [`SelectionSlots`](https://side-xp.github.io/unity-selection/api/SideXP.Selection/SelectionSlots): saves and restores selections in numbered slots

Always go through these two classes to change the history or the slots: they notify the Selection window, save the slots on disk, and log a warning when some objects can't be selected.

> Note: if your own code is declared in a namespace that starts with `SideXP`, the name `Selection` refers to the `SideXP.Selection` namespace instead of Unity's `Selection` class. In that case, use an alias to reach Unity's API: `using UnitySelection = UnityEditor.Selection;`

## Navigate through the history

```csharp
using SideXP.Selection;

// Go back to the previous selection, if there's one
if (SelectionHistory.CanGoBack)
    SelectionHistory.Back();

// Go back by 3 entries (or to the oldest entry if there are fewer)
SelectionHistory.Back(3);

// Go forward again
SelectionHistory.Forward();

// Select the oldest entry of the history
SelectionHistory.GoTo(0);
```

Entries whose objects can't be selected (for example because their scene is not loaded) are skipped by `Back()` and `Forward()`.

## Read the history

[`SelectionHistory.Entries`](https://side-xp.github.io/unity-selection/api/SideXP.Selection/SelectionHistory) lists the recorded selections from the oldest to the most recent, and `CurrentIndex` is the index of the current one. Each entry is a [`SelectionSnapshot`](https://side-xp.github.io/unity-selection/api/SideXP.Selection/SelectionSnapshot).

```csharp
using UnityEngine;
using SideXP.Selection;

// Log the history from the most recent entry to the oldest one, as displayed in the Selection window
for (int i = SelectionHistory.Entries.Count - 1; i >= 0; i--)
{
    SelectionSnapshot entry = SelectionHistory.Entries[i];
    string marker = i == SelectionHistory.CurrentIndex ? "> " : "  ";
    Debug.Log($"{marker}{SelectionDisplayUtility.GetName(entry)} ({entry.Count} objects)");
}
```

[`SelectionDisplayUtility`](https://side-xp.github.io/unity-selection/api/SideXP.Selection/SelectionDisplayUtility) gives you the same names and messages as the ones displayed by the Selection window.

## Save objects in a slot

Slot indices start at 0, while the Selection window labels them after the number keys of the keyboard: the slot at index `0` is labelled *1*, and the slot at index `9` is labelled *0*. Using an index out of range throws an `ArgumentOutOfRangeException`.

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SideXP.Selection;

public static class MySelectionTools
{
    // Saves the children of the selected GameObject in the first slot
    [MenuItem("Tools/My Project/Save Children In Slot 1")]
    private static void SaveChildrenInSlot()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null)
            return;

        List<Object> children = new List<Object>();
        foreach (Transform child in root.transform)
            children.Add(child.gameObject);

        // Returns false if there's nothing to save
        if (!SelectionSlots.Assign(0, children))
            Debug.LogWarning($"{root.name} has no children to save.");
    }
}
```

`SelectionSlots` also provides:

- `Assign(index)` to save the current selection,
- `Assign(index, snapshot)` to save a copy of an existing selection (for example, `SelectionHistory.Current`), including its objects that can't be selected right now,
- `AssignOrRestore(index)`, which behaves like clicking a slot button in the Selection window.

## Restore a slot

```csharp
using UnityEngine;
using SideXP.Selection;

SelectionSnapshot slot = SelectionSlots.Get(0);
if (slot == null)
{
    Debug.Log("The first slot is empty.");
}
else if (!slot.CanResolve())
{
    // None of the saved objects can be selected right now
    Debug.Log(SelectionDisplayUtility.GetUnavailableObjectsMessage(slot));
}
else
{
    SelectionSlots.Restore(0);
}
```

> Note: `SelectionSlots.Restore()` already logs a warning listing the objects that can't be selected, so you only need to check them yourself if you want to handle that case differently.

## Inspect the objects of a selection

A [`SelectionSnapshot`](https://side-xp.github.io/unity-selection/api/SideXP.Selection/SelectionSnapshot) doesn't store direct references to the selected objects, but [`ObjectRef`](https://side-xp.github.io/unity-selection/api/SideXP.Selection/ObjectRef) instances, which can find the objects again after they've been reloaded.

```csharp
using UnityEngine;
using SideXP.Selection;

SelectionSnapshot current = SelectionHistory.Current;
if (current != null)
{
    foreach (ObjectRef objRef in current.Objects)
    {
        // Resolved, Unloaded (its scene is not loaded) or Missing (it doesn't exist anymore)
        EObjectRefState state = objRef.GetState(out Object obj);
        string location = objRef.IsSceneObject ? $"{objRef.SceneName}: {objRef.HierarchyPath}" : "asset";
        Debug.Log($"{objRef.Name} ({location}) is {state}", obj);
    }

    // Or get all the objects that can be selected right now
    Object[] objects = current.Resolve(out Object activeObject);
}
```

## React to changes

Both `SelectionHistory` and `SelectionSlots` raise an `OnChanged` event. For example, to keep a custom editor window up to date:

```csharp
using UnityEditor;
using SideXP.Selection;

public class MySelectionWindow : EditorWindow
{
    private void OnEnable()
    {
        SelectionHistory.OnChanged += Repaint;
        SelectionSlots.OnChanged += Repaint;
    }

    private void OnDisable()
    {
        SelectionHistory.OnChanged -= Repaint;
        SelectionSlots.OnChanged -= Repaint;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("History entries", SelectionHistory.Entries.Count.ToString());
    }
}
```

> Note: a new selection is recorded in the history only once it has remained unchanged for the *Commit Delay* defined in the [Preferences](./manual.md#preferences). `SelectionHistory.OnChanged` is raised at that moment, not as soon as the selection changes.
