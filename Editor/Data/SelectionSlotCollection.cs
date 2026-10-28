using System;
using System.Collections.Generic;

using UnityEngine;

namespace SideXP.Selection
{

    /// <summary>
    /// Fixed-size list of numbered slots, each one containing a saved selection or being unassigned.
    /// </summary>
    [Serializable]
    public class SelectionSlotCollection
    {

        #region Fields

        /// <summary>
        /// The default number of slots, matching the number keys of a keyboard.
        /// </summary>
        public const int DefaultCount = 10;

        /// <summary>
        /// The saved selection of each slot. An unassigned slot contains an empty snapshot.
        /// </summary>
        [SerializeField]
        private List<SelectionSnapshot> _slots = new List<SelectionSnapshot>();

        #endregion


        #region Lifecycle

        /// <summary>
        /// Creates a collection with <see cref="DefaultCount"/> unassigned slots.
        /// </summary>
        public SelectionSlotCollection()
            : this(DefaultCount) { }

        /// <summary>
        /// Creates a collection with the given number of unassigned slots.
        /// </summary>
        /// <param name="count">The number of slots.</param>
        public SelectionSlotCollection(int count)
        {
            Resize(count);
        }

        #endregion


        #region Public API

        /// <summary>
        /// The number of slots.
        /// </summary>
        public int Count => _slots.Count;

        /// <summary>
        /// Checks if a selection is saved in the given slot.
        /// </summary>
        /// <param name="index">The index of the slot.</param>
        /// <returns>Returns true if the slot is assigned.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
        public bool IsAssigned(int index)
        {
            ValidateIndex(index);
            return !_slots[index].IsEmpty;
        }

        /// <summary>
        /// Gets the selection saved in the given slot.
        /// </summary>
        /// <param name="index">The index of the slot.</param>
        /// <returns>Returns the saved selection, or null if the slot is not assigned.</returns>
        /// <inheritdoc cref="IsAssigned(int)" path="/exception"/>
        public SelectionSnapshot Get(int index)
        {
            return IsAssigned(index) ? _slots[index] : null;
        }

        /// <summary>
        /// Saves a selection in the given slot, replacing the previous one if any.
        /// </summary>
        /// <param name="index">The index of the slot.</param>
        /// <param name="snapshot">The selection to save. If null or empty, the slot is unassigned.</param>
        /// <inheritdoc cref="IsAssigned(int)" path="/exception"/>
        public void Set(int index, SelectionSnapshot snapshot)
        {
            ValidateIndex(index);
            _slots[index] = snapshot ?? CreateEmptySlot();
        }

        /// <summary>
        /// Unassigns the given slot.
        /// </summary>
        /// <param name="index">The index of the slot.</param>
        /// <returns>Returns true if the slot was assigned.</returns>
        /// <inheritdoc cref="IsAssigned(int)" path="/exception"/>
        public bool Clear(int index)
        {
            if (!IsAssigned(index))
                return false;

            _slots[index] = CreateEmptySlot();
            return true;
        }

        /// <summary>
        /// Unassigns all the slots.
        /// </summary>
        public void ClearAll()
        {
            for (int i = 0; i < _slots.Count; i++)
                _slots[i] = CreateEmptySlot();
        }

        /// <summary>
        /// Changes the number of slots. New slots are unassigned, and the last slots are discarded if the collection is shrunk.
        /// </summary>
        /// <param name="count">The new number of slots. Negative values are considered as 0.</param>
        public void Resize(int count)
        {
            count = Mathf.Max(0, count);

            if (_slots.Count > count)
                _slots.RemoveRange(count, _slots.Count - count);

            while (_slots.Count < count)
                _slots.Add(CreateEmptySlot());
        }

        /// <summary>
        /// Removes the objects that don't exist anymore from the saved selections. A slot that becomes empty is unassigned.
        /// </summary>
        /// <inheritdoc cref="SelectionSnapshot.Clean" path="/remarks"/>
        /// <returns>Returns true if at least one object has been removed.</returns>
        public bool Clean()
        {
            bool hasChanged = false;
            foreach (SelectionSnapshot slot in _slots)
            {
                if (slot.Clean())
                    hasChanged = true;
            }
            return hasChanged;
        }

        /// <summary>
        /// Captures again the identifiers of the objects that are still alive in all the slots.
        /// </summary>
        /// <inheritdoc cref="ObjectRef.Refresh" path="/remarks"/>
        public void Refresh()
        {
            foreach (SelectionSnapshot slot in _slots)
                slot.Refresh();
        }

        #endregion


        #region Internal API

        /// <summary>
        /// Throws an exception if the given slot index is out of range.
        /// </summary>
        /// <param name="index">The index to check.</param>
        /// <inheritdoc cref="IsAssigned(int)" path="/exception"/>
        internal void ValidateIndex(int index)
        {
            if (index < 0 || index >= _slots.Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"The slot index must be between 0 and {_slots.Count - 1}.");
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates the value of an unassigned slot.
        /// </summary>
        /// <returns>Returns an empty snapshot.</returns>
        private static SelectionSnapshot CreateEmptySlot()
        {
            return new SelectionSnapshot(null);
        }

        #endregion

    }

}
