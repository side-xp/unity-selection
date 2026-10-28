using System;
using System.Collections.Generic;

using Object = UnityEngine.Object;

namespace SideXP.Selection
{

    /// <summary>
    /// Numbered slots where selections can be saved and restored later. The slots are personal, and saved on disk after each change.
    /// </summary>
    public static class SelectionSlots
    {

        #region Fields

        /// <summary>
        /// Called when a slot has been assigned, replaced, cleared or cleaned.
        /// </summary>
        public static event Action OnChanged;

        #endregion


        #region Public API

        /// <inheritdoc cref="SelectionSlotCollection.Count"/>
        public static int Count => Slots.Count;

        /// <inheritdoc cref="SelectionSlotCollection.IsAssigned(int)"/>
        public static bool IsAssigned(int index)
        {
            return Slots.IsAssigned(index);
        }

        /// <inheritdoc cref="SelectionSlotCollection.Get(int)"/>
        public static SelectionSnapshot Get(int index)
        {
            return Slots.Get(index);
        }

        /// <summary>
        /// Saves the current selection in the given slot, replacing the previous one if any.
        /// </summary>
        /// <param name="index">The index of the slot.</param>
        /// <returns>Returns true if the selection has been saved, or false if the current selection is empty.</returns>
        /// <inheritdoc cref="SelectionSlotCollection.IsAssigned(int)" path="/exception"/>
        public static bool Assign(int index)
        {
            return AssignSnapshot(index, SelectionSnapshot.FromCurrentSelection());
        }

        /// <summary>
        /// Saves the given selection in the given slot, replacing the previous one if any.
        /// </summary>
        /// <param name="index">The index of the slot.</param>
        /// <param name="objects">The objects to save. Null entries and duplicates are ignored.</param>
        /// <param name="activeObject">The active object of the selection. Ignored if it's not one of the given objects.</param>
        /// <returns>Returns true if the selection has been saved, or false if there's no object to save.</returns>
        /// <inheritdoc cref="SelectionSlotCollection.IsAssigned(int)" path="/exception"/>
        public static bool Assign(int index, IEnumerable<Object> objects, Object activeObject = null)
        {
            return AssignSnapshot(index, new SelectionSnapshot(objects, activeObject));
        }

        /// <summary>
        /// Saves a copy of the given selection in the given slot, replacing the previous one if any.
        /// </summary>
        /// <remarks>
        /// Objects that can't be resolved (for example, objects in a scene that is not loaded) are saved as well, which is not the case
        /// when saving resolved objects with <see cref="Assign(int, IEnumerable{Object}, Object)"/>.
        /// </remarks>
        /// <param name="index">The index of the slot.</param>
        /// <param name="snapshot">The selection to save (for example, an entry of <see cref="SelectionHistory.Entries"/>).</param>
        /// <returns>Returns true if the selection has been saved, or false if it's empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the given snapshot is null.</exception>
        /// <inheritdoc cref="SelectionSlotCollection.IsAssigned(int)" path="/exception"/>
        public static bool Assign(int index, SelectionSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            return AssignSnapshot(index, snapshot.Clone());
        }

        /// <summary>
        /// Selects the objects saved in the given slot. Objects that can't be resolved are skipped, and a warning is logged to list them.
        /// </summary>
        /// <param name="index">The index of the slot.</param>
        /// <returns>
        /// Returns true if the selection has been restored, or false if the slot is not assigned or none of its objects can be resolved.
        /// </returns>
        /// <inheritdoc cref="SelectionSlotCollection.IsAssigned(int)" path="/exception"/>
        public static bool Restore(int index)
        {
            SelectionSnapshot snapshot = Slots.Get(index);
            if (snapshot == null)
                return false;

            bool isApplied = snapshot.Apply();
            SelectionDisplayUtility.LogUnavailableObjects(snapshot);
            return isApplied;
        }

        /// <summary>
        /// Restores the selection saved in the given slot if it's assigned, or saves the current selection in it otherwise.
        /// </summary>
        /// <param name="index">The index of the slot.</param>
        /// <returns>Returns true if the selection has been restored or saved.</returns>
        /// <inheritdoc cref="SelectionSlotCollection.IsAssigned(int)" path="/exception"/>
        public static bool AssignOrRestore(int index)
        {
            return Slots.IsAssigned(index)
                ? Restore(index)
                : Assign(index);
        }

        /// <inheritdoc cref="SelectionSlotCollection.Clear(int)"/>
        public static bool Clear(int index)
        {
            if (!Slots.Clear(index))
                return false;

            NotifyChange();
            return true;
        }

        /// <inheritdoc cref="SelectionSlotCollection.ClearAll"/>
        public static void ClearAll()
        {
            Slots.ClearAll();
            NotifyChange();
        }

        /// <inheritdoc cref="SelectionSlotCollection.Clean"/>
        public static bool Clean()
        {
            if (!Slots.Clean())
                return false;

            NotifyChange();
            return true;
        }

        /// <inheritdoc cref="SelectionSlotCollection.Refresh"/>
        public static void RefreshReferences()
        {
            Slots.Refresh();
            SelectionSlotsState.instance.SaveToDisk();
        }

        #endregion


        #region Private API

        /// <inheritdoc cref="SelectionSlotsState.Slots"/>
        private static SelectionSlotCollection Slots => SelectionSlotsState.instance.Slots;

        /// <summary>
        /// Saves the given selection in the given slot, replacing the previous one if any.
        /// </summary>
        /// <param name="index">The index of the slot.</param>
        /// <param name="snapshot">The selection to save. This instance is stored as is, so it must not be shared.</param>
        /// <returns>Returns true if the selection has been saved, or false if it's empty.</returns>
        /// <inheritdoc cref="SelectionSlotCollection.IsAssigned(int)" path="/exception"/>
        private static bool AssignSnapshot(int index, SelectionSnapshot snapshot)
        {
            Slots.ValidateIndex(index);
            if (snapshot.IsEmpty)
                return false;

            Slots.Set(index, snapshot);
            NotifyChange();
            return true;
        }

        /// <summary>
        /// Saves the slots on disk, and notifies listeners that they have changed.
        /// </summary>
        private static void NotifyChange()
        {
            SelectionSlotsState.instance.SaveToDisk();
            OnChanged?.Invoke();
        }

        #endregion

    }

}
