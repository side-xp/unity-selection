using UnityEditor;

using SideXP.Core.EditorOnly;

namespace SideXP.Selection
{

    /// <summary>
    /// Main toolbar menus for navigating through the selection history and managing the selection slots.
    /// </summary>
    internal static class SelectionMenus
    {

        #region Fields

        private const string MenuPrefix = EditorConstants.ToolbarMenu + "/Selection/";

        private const string BackMenu = MenuPrefix + "Back";
        private const string ForwardMenu = MenuPrefix + "Forward";
        private const string ClearHistoryMenu = MenuPrefix + "Clear History";
        private const string ClearSlotsMenu = MenuPrefix + "Clear All Slots";

        // Priorities are spaced by more than 10 between groups, so Unity draws a separator between them
        private const int NavigationMenuPriority = 0;
        private const int ClearMenuPriority = NavigationMenuPriority + 20;

        #endregion


        #region Navigation

        /// <inheritdoc cref="SelectionHistory.Back(int)"/>
        [MenuItem(BackMenu, false, NavigationMenuPriority)]
        private static void Back()
        {
            SelectionHistory.Back();
        }

        /// <summary>
        /// Checks if the <see cref="BackMenu"/> menu is enabled.
        /// </summary>
        /// <returns>Returns true if there's a previous entry that can be selected.</returns>
        [MenuItem(BackMenu, true)]
        private static bool CanGoBack()
        {
            return SelectionHistory.CanGoBack;
        }

        /// <inheritdoc cref="SelectionHistory.Forward(int)"/>
        [MenuItem(ForwardMenu, false, NavigationMenuPriority + 1)]
        private static void Forward()
        {
            SelectionHistory.Forward();
        }

        /// <summary>
        /// Checks if the <see cref="ForwardMenu"/> menu is enabled.
        /// </summary>
        /// <returns>Returns true if there's a next entry that can be selected.</returns>
        [MenuItem(ForwardMenu, true)]
        private static bool CanGoForward()
        {
            return SelectionHistory.CanGoForward;
        }

        #endregion


        #region Clear

        /// <inheritdoc cref="SelectionHistory.Clear"/>
        [MenuItem(ClearHistoryMenu, false, ClearMenuPriority)]
        private static void ClearHistory()
        {
            SelectionHistory.Clear();
        }

        /// <summary>
        /// Unassigns all the selection slots, after the user confirmed.
        /// </summary>
        [MenuItem(ClearSlotsMenu, false, ClearMenuPriority + 1)]
        private static void ClearSlots()
        {
            bool isConfirmed = EditorUtility.DisplayDialog(
                "Clear All Slots",
                "All the selection slots will be unassigned. This can't be undone.\nProceed?",
                "Clear All Slots",
                "Cancel");

            if (isConfirmed)
                SelectionSlots.ClearAll();
        }

        #endregion

    }

}
