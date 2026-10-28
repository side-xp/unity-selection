using System;
using System.Text;

using UnityEngine;
using UnityEditor;

using SideXP.Core;
using SideXP.Core.EditorOnly;

using Object = UnityEngine.Object;
using UnitySelection = UnityEditor.Selection;

namespace SideXP.Selection
{

    /// <summary>
    /// Window for navigating through the selection history and using the selection slots.
    /// </summary>
    /// <remarks>
    /// This window doesn't hold any data: it only draws the state of <see cref="SelectionHistory"/> and <see cref="SelectionSlots"/>,
    /// and forwards user actions to them.
    /// </remarks>
    public class SelectionWindow : EditorWindow
    {

        #region Fields

        private const string WindowTitle = "Selection";
        private const string MenuItem = EditorConstants.EditorWindowMenu + "/Selection/Selection Window";

        /// <summary>
        /// The priority of the menu item, lower than the other menus of the same submenu (with a gap of more than 10, so Unity draws a
        /// separator), so it's displayed first.
        /// </summary>
        private const int MenuPriority = -100;

        private const int RightMouseButton = 1;

        /// <summary>
        /// The number of keys in the number row of a keyboard, used to label the 10th slot "0" so it matches its default shortcut key.
        /// </summary>
        private const int NumberKeysCount = 10;

        /// <summary>
        /// The maximum number of object names listed in the tooltip of a slot.
        /// </summary>
        private const int MaxTooltipNamesCount = 5;

        private const float SlotButtonMinWidth = 24f;

        /// <summary>
        /// The width of the button on each history entry that opens the menu for saving that entry in a slot.
        /// </summary>
        private const float SaveToSlotButtonWidth = 48f;

        /// <summary>
        /// The size of the object icons displayed in the history, so large icons don't stretch the rows.
        /// </summary>
        private const float HistoryIconSize = 16f;

        /// <summary>
        /// The color of the slot buttons that are not assigned, and of the slots or history entries whose objects are not available.
        /// </summary>
        private static readonly Color UnavailableColor = new Color(1f, 1f, 1f, .5f);

        /// <summary>
        /// The background color of the current entry of the history. Translucent, so it works with both light and dark editor themes.
        /// </summary>
        private static readonly Color CurrentEntryColor = new Color(.25f, .5f, 1f, .3f);

        /// <summary>
        /// The style of the slot buttons that are assigned.
        /// </summary>
        private static GUIStyle s_assignedSlotStyle = null;

        /// <summary>
        /// The scroll position of the history list.
        /// </summary>
        [SerializeField]
        private Vector2 _historyScrollPosition = Vector2.zero;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Opens the selection window.
        /// </summary>
        /// <returns>Returns the opened window.</returns>
        [MenuItem(MenuItem, false, MenuPriority)]
        public static SelectionWindow Open()
        {
            SelectionWindow window = GetWindow<SelectionWindow>(false, WindowTitle, true);
            window.Show();
            return window;
        }

        /// <summary>
        /// Called when this window is loaded.
        /// </summary>
        private void OnEnable()
        {
            SelectionHistory.OnChanged += Repaint;
            SelectionSlots.OnChanged += Repaint;
        }

        /// <summary>
        /// Called when this window is closed or unloaded.
        /// </summary>
        private void OnDisable()
        {
            SelectionHistory.OnChanged -= Repaint;
            SelectionSlots.OnChanged -= Repaint;
        }

        #endregion


        #region UI

        /// <summary>
        /// Draws this window's content on GUI.
        /// </summary>
        private void OnGUI()
        {
            DrawNavigation();
            EditorGUILayout.Space();
            DrawSlots();
            EditorGUILayout.Space();
            DrawHistory();
        }

        /// <summary>
        /// Draws the history navigation buttons, side by side and sharing the full width of the window.
        /// </summary>
        private void DrawNavigation()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EnabledScope(SelectionHistory.CanGoBack))
                {
                    GUIContent content = MakeContent("← Back", "Select the previous selection.", SelectionShortcuts.BackId);
                    if (GUILayout.Button(content, MoreGUI.HeightMOpt, GUILayout.ExpandWidth(true)))
                        SelectionHistory.Back();
                }

                using (new EnabledScope(SelectionHistory.CanGoForward))
                {
                    GUIContent content = MakeContent("Forward →", "Select the next selection.", SelectionShortcuts.ForwardId);
                    if (GUILayout.Button(content, MoreGUI.HeightMOpt, GUILayout.ExpandWidth(true)))
                        SelectionHistory.Forward();
                }
            }
        }

        /// <summary>
        /// Draws the numbered slot buttons.
        /// </summary>
        private void DrawSlots()
        {
            EditorGUILayout.LabelField("Slots", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < SelectionSlots.Count; i++)
                    DrawSlotButton(i);
            }
        }

        /// <summary>
        /// Draws the button of a given slot. Clicking it restores the slot (or assigns it if it's not assigned), and right-clicking it
        /// opens a context menu with more options.
        /// </summary>
        /// <param name="index">The index of the slot.</param>
        private void DrawSlotButton(int index)
        {
            SelectionSnapshot slot = SelectionSlots.Get(index);
            bool isAssigned = slot != null;
            bool isAvailable = isAssigned && slot.CanResolve();

            GUIStyle style = isAssigned ? AssignedSlotStyle : GUI.skin.button;
            GUIContent content = new GUIContent(GetSlotLabel(index), GetSlotTooltip(index, slot, isAvailable));
            Rect rect = GUILayoutUtility.GetRect(
                content,
                style,
                MoreGUI.HeightMOpt,
                GUILayout.MinWidth(SlotButtonMinWidth),
                GUILayout.ExpandWidth(true));

            Event currentEvent = Event.current;
            if (rect.Contains(currentEvent.mousePosition))
            {
                // Buttons also react to right clicks, which are reserved for opening the context menu
                if (currentEvent.type == EventType.MouseDown && currentEvent.button == RightMouseButton)
                {
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.ContextClick)
                {
                    ShowSlotContextMenu(index);
                    currentEvent.Use();
                }
            }

            using (new ColorScope(isAvailable ? GUI.color : UnavailableColor))
            {
                // Restoring a slot already logs a warning if its objects are not available
                if (GUI.Button(rect, content, style) && !SelectionSlots.AssignOrRestore(index) && !isAssigned)
                    Debug.LogWarning("Select objects to save them in this slot");
            }
        }

        /// <summary>
        /// Opens the context menu of a given slot.
        /// </summary>
        /// <param name="index">The index of the slot.</param>
        private void ShowSlotContextMenu(int index)
        {
            bool isAssigned = SelectionSlots.IsAssigned(index);
            bool hasSelection = UnitySelection.objects.Length > 0;
            string assignLabel = isAssigned ? "Replace With Current Selection" : "Assign Current Selection";

            GenericMenu menu = new GenericMenu();
            AddMenuItem(menu, "Restore", isAssigned, () => SelectionSlots.Restore(index));
            AddMenuItem(menu, assignLabel, hasSelection, () => SelectionSlots.Assign(index));
            menu.AddSeparator(string.Empty);
            AddMenuItem(menu, "Clear", isAssigned, () => SelectionSlots.Clear(index));
            menu.ShowAsContext();
        }

        /// <summary>
        /// Draws the list of the selection history entries, from the most recent to the oldest.
        /// </summary>
        private void DrawHistory()
        {
            int count = SelectionHistory.Entries.Count;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("History", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                using (new EnabledScope(count > 0))
                {
                    GUIContent content = new GUIContent("Clear", "Remove all the entries of the history, except the current selection.");
                    if (GUILayout.Button(content, EditorStyles.miniButton, MoreGUI.WidthSOpt))
                    {
                        SelectionHistory.Clear();
                        // The entries count has changed, so the rest of this GUI pass would draw entries that don't exist anymore
                        GUIUtility.ExitGUI();
                    }
                }
            }

            if (count <= 0)
            {
                EditorGUILayout.HelpBox("No selection recorded yet.", MessageType.Info);
                return;
            }

            using (var scope = new EditorGUILayout.ScrollViewScope(_historyScrollPosition))
            {
                EditorGUIUtility.SetIconSize(new Vector2(HistoryIconSize, HistoryIconSize));
                for (int i = count - 1; i >= 0; i--)
                    DrawHistoryEntry(i);
                EditorGUIUtility.SetIconSize(Vector2.zero);

                _historyScrollPosition = scope.scrollPosition;
            }
        }

        /// <summary>
        /// Draws a given entry of the history. Clicking it selects that entry, and its button opens a menu for saving it in a slot.
        /// </summary>
        /// <param name="index">The index of the entry in the history.</param>
        private void DrawHistoryEntry(int index)
        {
            SelectionSnapshot entry = SelectionHistory.Entries[index];
            bool isCurrent = index == SelectionHistory.CurrentIndex;

            Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            if (isCurrent)
                EditorGUI.DrawRect(rowRect, CurrentEntryColor);

            Rect labelRect = new Rect(rowRect);
            labelRect.width -= SaveToSlotButtonWidth + MoreGUI.HMargin;

            using (new ColorScope(entry.CanResolve() ? GUI.color : UnavailableColor))
            {
                GUIContent content = new GUIContent(
                    SelectionDisplayUtility.GetName(entry),
                    GetEntryIcon(entry),
                    GetSnapshotDescription(entry));
                // Selecting an entry already logs a warning if its objects are not available
                if (GUI.Button(labelRect, content, isCurrent ? EditorStyles.boldLabel : EditorStyles.label))
                    SelectionHistory.GoTo(index);
            }

            Rect buttonRect = new Rect(rowRect);
            buttonRect.xMin = buttonRect.xMax - SaveToSlotButtonWidth;

            GUIContent buttonContent = new GUIContent("Slot", "Save this selection in a slot.");
            if (EditorGUI.DropdownButton(buttonRect, buttonContent, FocusType.Passive, EditorStyles.miniPullDown))
                ShowSaveToSlotMenu(buttonRect, entry);
        }

        /// <summary>
        /// Opens the menu for saving a given history entry in a slot.
        /// </summary>
        /// <param name="buttonRect">The position of the button that opens the menu.</param>
        /// <param name="entry">The history entry to save.</param>
        private void ShowSaveToSlotMenu(Rect buttonRect, SelectionSnapshot entry)
        {
            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < SelectionSlots.Count; i++)
            {
                int slotIndex = i;
                SelectionSnapshot slot = SelectionSlots.Get(slotIndex);
                // Checked if this entry is already saved in that slot
                bool isSaved = slot != null && slot.HasSameObjects(entry);

                string label = slot != null && !isSaved
                    ? $"Slot {GetSlotLabel(slotIndex)} (replace)"
                    : $"Slot {GetSlotLabel(slotIndex)}";
                menu.AddItem(new GUIContent(label), isSaved, () => SelectionSlots.Assign(slotIndex, entry));
            }
            menu.DropDown(buttonRect);
        }

        #endregion


        #region Private API

        /// <inheritdoc cref="s_assignedSlotStyle"/>
        private static GUIStyle AssignedSlotStyle
        {
            get
            {
                if (s_assignedSlotStyle == null)
                    s_assignedSlotStyle = GUI.skin.button.Bold();
                return s_assignedSlotStyle;
            }
        }

        /// <summary>
        /// Creates the content of a control, with the keys bound to the given shortcut appended to its tooltip.
        /// </summary>
        /// <param name="text">The text of the control.</param>
        /// <param name="tooltip">The tooltip of the control.</param>
        /// <param name="shortcutId">The id of the shortcut that performs the same action as the control.</param>
        /// <returns>Returns the created content.</returns>
        private static GUIContent MakeContent(string text, string tooltip, string shortcutId)
        {
            string binding = SelectionShortcuts.GetBindingLabel(shortcutId);
            return new GUIContent(text, string.IsNullOrEmpty(binding) ? tooltip : $"{tooltip} ({binding})");
        }

        /// <summary>
        /// Adds an item to the given menu, enabled or not.
        /// </summary>
        /// <param name="menu">The menu to which the item is added.</param>
        /// <param name="label">The label of the item.</param>
        /// <param name="isEnabled">If disabled, the item is displayed but can't be selected.</param>
        /// <param name="action">The function to call when the item is selected.</param>
        private static void AddMenuItem(GenericMenu menu, string label, bool isEnabled, GenericMenu.MenuFunction action)
        {
            GUIContent content = new GUIContent(label);
            if (isEnabled)
                menu.AddItem(content, false, action);
            else
                menu.AddDisabledItem(content);
        }

        /// <summary>
        /// Gets the label of a given slot button, matching the number key of its default shortcut.
        /// </summary>
        /// <param name="index">The index of the slot.</param>
        /// <returns>Returns the label of the slot.</returns>
        private static string GetSlotLabel(int index)
        {
            return ((index + 1) % NumberKeysCount).ToString();
        }

        /// <summary>
        /// Gets the tooltip of a given slot button, describing its content and how to use it.
        /// </summary>
        /// <param name="index">The index of the slot.</param>
        /// <param name="slot">The selection saved in the slot, or null if the slot is not assigned.</param>
        /// <param name="isAvailable">Indicates if at least one of the objects saved in the slot can be resolved.</param>
        /// <returns>Returns the tooltip of the slot.</returns>
        private static string GetSlotTooltip(int index, SelectionSnapshot slot, bool isAvailable)
        {
            StringBuilder builder = new StringBuilder();
            if (slot == null)
            {
                builder.Append("Empty slot. Click to save the current selection in it.");
            }
            else
            {
                builder.Append(GetSnapshotDescription(slot));
                if (!isAvailable)
                    builder.Append("\nThese objects are not available (their scene may not be loaded).");
                builder.Append("\nClick to restore this selection.");
            }
            builder.Append("\nRight-click for more options.");

            string restoreBinding = SelectionShortcuts.GetBindingLabel(SelectionShortcuts.GetRestoreSlotId(index));
            if (!string.IsNullOrEmpty(restoreBinding))
                builder.Append($"\nRestore or assign: {restoreBinding}");

            string assignBinding = SelectionShortcuts.GetBindingLabel(SelectionShortcuts.GetAssignSlotId(index));
            if (!string.IsNullOrEmpty(assignBinding))
                builder.Append($"\nAssign or replace: {assignBinding}");

            return builder.ToString();
        }

        /// <summary>
        /// Gets the icon of a given history entry: the icon of its main object (see
        /// <see cref="SelectionDisplayUtility.GetMainObject(SelectionSnapshot)"/>), or the icon of its type if it can't be resolved.
        /// </summary>
        /// <param name="entry">The history entry.</param>
        /// <returns>Returns the icon of the entry, or null if there's none.</returns>
        private static Texture GetEntryIcon(SelectionSnapshot entry)
        {
            ObjectRef mainObject = SelectionDisplayUtility.GetMainObject(entry);
            if (mainObject == null)
                return null;

            Object obj = mainObject.Resolve();
            if (obj != null)
                return EditorGUIUtility.ObjectContent(obj, obj.GetType()).image;

            Type type = mainObject.ObjectType;
            return type != null ? EditorGUIUtility.ObjectContent(null, type).image : null;
        }

        /// <summary>
        /// Lists the names of the objects of a given snapshot, as they were when the snapshot was captured.
        /// </summary>
        /// <param name="snapshot">The snapshot to describe.</param>
        /// <returns>Returns the description of the snapshot, with one object per line.</returns>
        private static string GetSnapshotDescription(SelectionSnapshot snapshot)
        {
            StringBuilder builder = new StringBuilder();
            int listedCount = Mathf.Min(snapshot.Count, MaxTooltipNamesCount);
            for (int i = 0; i < listedCount; i++)
            {
                if (i > 0)
                    builder.Append('\n');
                builder.Append($"- {snapshot.Objects[i].Name}");
            }

            if (snapshot.Count > listedCount)
                builder.Append($"\n(+{snapshot.Count - listedCount} more)");

            return builder.ToString();
        }

        #endregion

    }

}
