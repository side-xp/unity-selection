using System.Text;

using UnityEngine;
using UnityEditor;

using SideXP.Core;
using SideXP.Core.EditorOnly;

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
        /// The color of the slot buttons that are not assigned, or whose objects are not available.
        /// </summary>
        private static readonly Color UnavailableSlotColor = new Color(1f, 1f, 1f, .5f);

        /// <summary>
        /// The style of the slot buttons that are assigned.
        /// </summary>
        private static GUIStyle s_assignedSlotStyle = null;

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
            DrawToolbar();
            EditorGUILayout.Space();
            DrawSlots();
        }

        /// <summary>
        /// Draws the toolbar, with the history navigation buttons.
        /// </summary>
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new EnabledScope(SelectionHistory.CanGoBack))
                {
                    GUIContent content = MakeContent("← Back", "Select the previous selection.", SelectionShortcuts.BackId);
                    if (GUILayout.Button(content, EditorStyles.toolbarButton))
                        SelectionHistory.Back();
                }

                using (new EnabledScope(SelectionHistory.CanGoForward))
                {
                    GUIContent content = MakeContent("Forward →", "Select the next selection.", SelectionShortcuts.ForwardId);
                    if (GUILayout.Button(content, EditorStyles.toolbarButton))
                        SelectionHistory.Forward();
                }

                GUILayout.FlexibleSpace();
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

            using (new ColorScope(isAvailable ? GUI.color : UnavailableSlotColor))
            {
                if (GUI.Button(rect, content, style) && !SelectionSlots.AssignOrRestore(index))
                {
                    string message = isAssigned
                        ? "The objects of this slot are not available"
                        : "Select objects to save them in this slot";
                    Debug.LogWarning(message);
                }
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
