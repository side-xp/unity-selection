using UnityEngine;
using UnityEditor.ShortcutManagement;

using SideXP.Core;

namespace SideXP.Selection
{

    /// <summary>
    /// Keyboard shortcuts for navigating through the selection history and using the selection slots. They can be rebound from the
    /// Shortcuts window (Edit > Shortcuts).
    /// </summary>
    /// <remarks>
    /// Slot numbers start at 1 and match the number keys of the keyboard, the 10th slot being bound to the 0 key.
    /// </remarks>
    internal static class SelectionShortcuts
    {

        #region Fields

        private const string ShortcutPrefix = Constants.CompanyName + "/Selection/";

        private const string BackId = ShortcutPrefix + "Back";
        private const string ForwardId = ShortcutPrefix + "Forward";

        /// <summary>
        /// Prefix of the shortcuts that restore a slot, or assign it if it's not assigned yet (same behavior as clicking a slot button).
        /// </summary>
        private const string RestoreSlotIdPrefix = ShortcutPrefix + "Restore Or Assign Slot ";

        /// <summary>
        /// Prefix of the shortcuts that save the current selection in a slot, replacing the previous one if any.
        /// </summary>
        private const string AssignSlotIdPrefix = ShortcutPrefix + "Assign Slot ";

        /// <summary>
        /// The default modifiers of the shortcuts that restore or assign a slot. Different from the ones used by Unity's own selection
        /// slots (Edit > Selection > Load Selection), which use Ctrl+Shift.
        /// </summary>
        private const ShortcutModifiers RestoreSlotModifiers = ShortcutModifiers.Alt;

        /// <summary>
        /// The default modifiers of the shortcuts that assign or replace a slot. Different from the ones used by Unity's own selection
        /// slots (Edit > Selection > Save Selection), which use Ctrl+Alt.
        /// </summary>
        private const ShortcutModifiers AssignSlotModifiers = ShortcutModifiers.Alt | ShortcutModifiers.Shift;

        #endregion


        #region History

        /// <summary>
        /// Selects the previous entry of the selection history.
        /// </summary>
        [Shortcut(BackId, KeyCode.LeftArrow, ShortcutModifiers.Action)]
        private static void Back()
        {
            SelectionHistory.Back();
        }

        /// <summary>
        /// Selects the next entry of the selection history.
        /// </summary>
        [Shortcut(ForwardId, KeyCode.RightArrow, ShortcutModifiers.Action)]
        private static void Forward()
        {
            SelectionHistory.Forward();
        }

        #endregion


        #region Restore Slots

        /// <summary>
        /// Restores the selection saved in the slot 1, or assigns it if it's not assigned.
        /// </summary>
        [Shortcut(RestoreSlotIdPrefix + "1", KeyCode.Alpha1, RestoreSlotModifiers)]
        private static void RestoreSlot1()
        {
            SelectionSlots.AssignOrRestore(0);
        }

        /// <summary>
        /// Restores the selection saved in the slot 2, or assigns it if it's not assigned.
        /// </summary>
        [Shortcut(RestoreSlotIdPrefix + "2", KeyCode.Alpha2, RestoreSlotModifiers)]
        private static void RestoreSlot2()
        {
            SelectionSlots.AssignOrRestore(1);
        }

        /// <summary>
        /// Restores the selection saved in the slot 3, or assigns it if it's not assigned.
        /// </summary>
        [Shortcut(RestoreSlotIdPrefix + "3", KeyCode.Alpha3, RestoreSlotModifiers)]
        private static void RestoreSlot3()
        {
            SelectionSlots.AssignOrRestore(2);
        }

        /// <summary>
        /// Restores the selection saved in the slot 4, or assigns it if it's not assigned.
        /// </summary>
        [Shortcut(RestoreSlotIdPrefix + "4", KeyCode.Alpha4, RestoreSlotModifiers)]
        private static void RestoreSlot4()
        {
            SelectionSlots.AssignOrRestore(3);
        }

        /// <summary>
        /// Restores the selection saved in the slot 5, or assigns it if it's not assigned.
        /// </summary>
        [Shortcut(RestoreSlotIdPrefix + "5", KeyCode.Alpha5, RestoreSlotModifiers)]
        private static void RestoreSlot5()
        {
            SelectionSlots.AssignOrRestore(4);
        }

        /// <summary>
        /// Restores the selection saved in the slot 6, or assigns it if it's not assigned.
        /// </summary>
        [Shortcut(RestoreSlotIdPrefix + "6", KeyCode.Alpha6, RestoreSlotModifiers)]
        private static void RestoreSlot6()
        {
            SelectionSlots.AssignOrRestore(5);
        }

        /// <summary>
        /// Restores the selection saved in the slot 7, or assigns it if it's not assigned.
        /// </summary>
        [Shortcut(RestoreSlotIdPrefix + "7", KeyCode.Alpha7, RestoreSlotModifiers)]
        private static void RestoreSlot7()
        {
            SelectionSlots.AssignOrRestore(6);
        }

        /// <summary>
        /// Restores the selection saved in the slot 8, or assigns it if it's not assigned.
        /// </summary>
        [Shortcut(RestoreSlotIdPrefix + "8", KeyCode.Alpha8, RestoreSlotModifiers)]
        private static void RestoreSlot8()
        {
            SelectionSlots.AssignOrRestore(7);
        }

        /// <summary>
        /// Restores the selection saved in the slot 9, or assigns it if it's not assigned.
        /// </summary>
        [Shortcut(RestoreSlotIdPrefix + "9", KeyCode.Alpha9, RestoreSlotModifiers)]
        private static void RestoreSlot9()
        {
            SelectionSlots.AssignOrRestore(8);
        }

        /// <summary>
        /// Restores the selection saved in the slot 10, or assigns it if it's not assigned.
        /// </summary>
        [Shortcut(RestoreSlotIdPrefix + "10", KeyCode.Alpha0, RestoreSlotModifiers)]
        private static void RestoreSlot10()
        {
            SelectionSlots.AssignOrRestore(9);
        }

        #endregion


        #region Assign Slots

        /// <summary>
        /// Saves the current selection in the slot 1, replacing the previous one if any.
        /// </summary>
        [Shortcut(AssignSlotIdPrefix + "1", KeyCode.Alpha1, AssignSlotModifiers)]
        private static void AssignSlot1()
        {
            SelectionSlots.Assign(0);
        }

        /// <summary>
        /// Saves the current selection in the slot 2, replacing the previous one if any.
        /// </summary>
        [Shortcut(AssignSlotIdPrefix + "2", KeyCode.Alpha2, AssignSlotModifiers)]
        private static void AssignSlot2()
        {
            SelectionSlots.Assign(1);
        }

        /// <summary>
        /// Saves the current selection in the slot 3, replacing the previous one if any.
        /// </summary>
        [Shortcut(AssignSlotIdPrefix + "3", KeyCode.Alpha3, AssignSlotModifiers)]
        private static void AssignSlot3()
        {
            SelectionSlots.Assign(2);
        }

        /// <summary>
        /// Saves the current selection in the slot 4, replacing the previous one if any.
        /// </summary>
        [Shortcut(AssignSlotIdPrefix + "4", KeyCode.Alpha4, AssignSlotModifiers)]
        private static void AssignSlot4()
        {
            SelectionSlots.Assign(3);
        }

        /// <summary>
        /// Saves the current selection in the slot 5, replacing the previous one if any.
        /// </summary>
        [Shortcut(AssignSlotIdPrefix + "5", KeyCode.Alpha5, AssignSlotModifiers)]
        private static void AssignSlot5()
        {
            SelectionSlots.Assign(4);
        }

        /// <summary>
        /// Saves the current selection in the slot 6, replacing the previous one if any.
        /// </summary>
        [Shortcut(AssignSlotIdPrefix + "6", KeyCode.Alpha6, AssignSlotModifiers)]
        private static void AssignSlot6()
        {
            SelectionSlots.Assign(5);
        }

        /// <summary>
        /// Saves the current selection in the slot 7, replacing the previous one if any.
        /// </summary>
        [Shortcut(AssignSlotIdPrefix + "7", KeyCode.Alpha7, AssignSlotModifiers)]
        private static void AssignSlot7()
        {
            SelectionSlots.Assign(6);
        }

        /// <summary>
        /// Saves the current selection in the slot 8, replacing the previous one if any.
        /// </summary>
        [Shortcut(AssignSlotIdPrefix + "8", KeyCode.Alpha8, AssignSlotModifiers)]
        private static void AssignSlot8()
        {
            SelectionSlots.Assign(7);
        }

        /// <summary>
        /// Saves the current selection in the slot 9, replacing the previous one if any.
        /// </summary>
        [Shortcut(AssignSlotIdPrefix + "9", KeyCode.Alpha9, AssignSlotModifiers)]
        private static void AssignSlot9()
        {
            SelectionSlots.Assign(8);
        }

        /// <summary>
        /// Saves the current selection in the slot 10, replacing the previous one if any.
        /// </summary>
        [Shortcut(AssignSlotIdPrefix + "10", KeyCode.Alpha0, AssignSlotModifiers)]
        private static void AssignSlot10()
        {
            SelectionSlots.Assign(9);
        }

        #endregion

    }

}
