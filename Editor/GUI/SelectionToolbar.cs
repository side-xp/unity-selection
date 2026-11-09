#if UNITY_6000_3_OR_NEWER
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.Toolbars;

using SideXP.Core;

using UnitySelection = UnityEditor.Selection;

namespace SideXP.Selection
{

    /// <summary>
    /// Elements added to the left side of the main toolbar of the editor for navigating through the selection history and
    /// using the selection slots.
    /// </summary>
    /// <remarks>
    /// Relies on the Main Toolbar API, which is only available from Unity 6.3. Users can move or hide these elements by right-clicking
    /// the toolbar.
    /// </remarks>
    [InitializeOnLoad]
    internal static class SelectionToolbar
    {

        #region Fields

        /// <summary>
        /// The path of the toolbar group. The elements are created as a single group, so users show or hide them all at once.
        /// </summary>
        private const string GroupPath = Constants.CompanyName + "/Selection";

        #endregion


        #region Lifecycle

        /// <summary>
        /// Static constructor, called when the editor is loaded.
        /// </summary>
        static SelectionToolbar()
        {
            SelectionHistory.OnChanged += RefreshNavigation;
        }

        #endregion


        #region Elements

        /// <summary>
        /// Creates the elements of the toolbar group, in the order they're displayed.
        /// </summary>
        /// <remarks>
        /// Docked on the left side of the toolbar rather than in the middle, since the middle dock is centered as a whole, so adding
        /// elements to it would move the Play controls away from the center.
        /// </remarks>
        /// <returns>Returns the created elements.</returns>
        [MainToolbarElement(GroupPath, defaultDockPosition = MainToolbarDockPosition.Left)]
        private static IEnumerable<MainToolbarElement> CreateElements()
        {
            return new List<MainToolbarElement>
            {
                CreatePreviousButton(),
                CreateSlotsDropdown(),
                CreateNextButton()
            };
        }

        /// <summary>
        /// Creates the button that selects the previous entry of the selection history.
        /// </summary>
        /// <returns>Returns the created element.</returns>
        private static MainToolbarElement CreatePreviousButton()
        {
            MainToolbarContent content = new MainToolbarContent(
                "←",
                MakeTooltip("Select the previous selection.", SelectionShortcuts.BackId));

            return new MainToolbarButton(content, () => SelectionHistory.Back())
            {
                enabled = SelectionHistory.CanGoBack
            };
        }

        /// <summary>
        /// Creates the dropdown that lists the selection slots.
        /// </summary>
        /// <returns>Returns the created element.</returns>
        private static MainToolbarElement CreateSlotsDropdown()
        {
            MainToolbarContent content = new MainToolbarContent("Selection", "Restore or assign a selection slot.");
            return new MainToolbarDropdown(content, ShowSlotsMenu);
        }

        /// <summary>
        /// Creates the button that selects the next entry of the selection history.
        /// </summary>
        /// <returns>Returns the created element.</returns>
        private static MainToolbarElement CreateNextButton()
        {
            MainToolbarContent content = new MainToolbarContent(
                "→",
                MakeTooltip("Select the next selection.", SelectionShortcuts.ForwardId));

            return new MainToolbarButton(content, () => SelectionHistory.Forward())
            {
                enabled = SelectionHistory.CanGoForward
            };
        }

        #endregion


        #region Private API

        /// <summary>
        /// Rebuilds the toolbar group, so the enabled state of the navigation buttons matches the current state of the history.
        /// </summary>
        private static void RefreshNavigation()
        {
            MainToolbar.Refresh(GroupPath);
        }

        /// <summary>
        /// Opens the menu that lists the selection slots, below the slots dropdown.
        /// </summary>
        /// <remarks>
        /// Clicking an assigned slot restores it, and clicking an empty slot saves the current selection in it (same behavior as the slot
        /// buttons of the <see cref="SelectionWindow"/>). Assigned slots can also be replaced from a submenu.
        /// </remarks>
        /// <param name="dropdownRect">The position of the dropdown that opens the menu.</param>
        private static void ShowSlotsMenu(Rect dropdownRect)
        {
            bool hasSelection = UnitySelection.objects.Length > 0;
            GenericMenu menu = new GenericMenu();

            for (int i = 0; i < SelectionSlots.Count; i++)
            {
                int index = i;
                SelectionSnapshot slot = SelectionSlots.Get(index);
                string slotLabel = SelectionWindow.GetSlotLabel(index);

                if (slot != null)
                {
                    string name = SelectionDisplayUtility.GetName(slot);
                    string label = slot.CanResolve() ? $"{slotLabel}: {name}" : $"{slotLabel}: {name} (unavailable)";
                    // Restoring a slot already logs a warning if its objects are not available
                    menu.AddItem(new GUIContent(label), false, () => SelectionSlots.Restore(index));
                }
                else
                {
                    AddMenuItem(menu, $"{slotLabel}: (Empty)", hasSelection, () => SelectionSlots.Assign(index));
                }
            }

            menu.AddSeparator(string.Empty);
            for (int i = 0; i < SelectionSlots.Count; i++)
            {
                int index = i;
                string label = SelectionSlots.IsAssigned(index)
                    ? $"Save Current Selection/Slot {SelectionWindow.GetSlotLabel(index)} (replace)"
                    : $"Save Current Selection/Slot {SelectionWindow.GetSlotLabel(index)}";
                AddMenuItem(menu, label, hasSelection, () => SelectionSlots.Assign(index));
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Open Selection Window"), false, () => SelectionWindow.Open());
            menu.DropDown(dropdownRect);
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
        /// Appends the keys bound to the given shortcut to a tooltip.
        /// </summary>
        /// <param name="tooltip">The tooltip of the element.</param>
        /// <param name="shortcutId">The id of the shortcut that performs the same action as the element.</param>
        /// <returns>Returns the complete tooltip.</returns>
        private static string MakeTooltip(string tooltip, string shortcutId)
        {
            string binding = SelectionShortcuts.GetBindingLabel(shortcutId);
            return string.IsNullOrEmpty(binding) ? tooltip : $"{tooltip} ({binding})";
        }

        #endregion

    }

}
#endif
