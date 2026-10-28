using System;
using System.Collections.Generic;

using UnityEngine;

namespace SideXP.Selection
{

    /// <summary>
    /// Ordered list of selection snapshots, with a cursor on the current one. Works like a web browser history: adding a snapshot after
    /// going back discards the "forward" entries.
    /// </summary>
    [Serializable]
    public class SelectionHistoryBuffer
    {

        #region Fields

        /// <summary>
        /// The default maximum number of entries in the history.
        /// </summary>
        public const int DefaultMaxCount = 50;

        /// <summary>
        /// The entries of the history, from the oldest to the most recent.
        /// </summary>
        [SerializeField]
        private List<SelectionSnapshot> _entries = new List<SelectionSnapshot>();

        /// <summary>
        /// The index of the current entry, or -1 if the history is empty.
        /// </summary>
        [SerializeField]
        private int _currentIndex = -1;

        /// <summary>
        /// The maximum number of entries in the history. The oldest entries are discarded when this limit is exceeded.
        /// </summary>
        [SerializeField]
        private int _maxCount = DefaultMaxCount;

        #endregion


        #region Public API

        /// <inheritdoc cref="_entries"/>
        public IReadOnlyList<SelectionSnapshot> Entries => _entries;

        /// <summary>
        /// The number of entries in the history.
        /// </summary>
        public int Count => _entries.Count;

        /// <inheritdoc cref="_currentIndex"/>
        public int CurrentIndex => _currentIndex;

        /// <summary>
        /// The current entry, or null if the history is empty.
        /// </summary>
        public SelectionSnapshot Current => _currentIndex >= 0 && _currentIndex < _entries.Count ? _entries[_currentIndex] : null;

        /// <inheritdoc cref="_maxCount"/>
        /// <remarks>Can't be lower than 1. Setting a lower value than the current number of entries discards the oldest ones.</remarks>
        public int MaxCount
        {
            get => _maxCount;
            set
            {
                _maxCount = Mathf.Max(1, value);
                Trim();
            }
        }

        /// <summary>
        /// Adds a snapshot after the current entry, and makes it the current one. The entries after the current one are discarded.
        /// </summary>
        /// <param name="snapshot">The snapshot to add.</param>
        /// <returns>Returns true if the snapshot has been added, or false if it matches the current entry.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the given snapshot is null.</exception>
        public bool Push(SelectionSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            SelectionSnapshot current = Current;
            if (current != null && current.Matches(snapshot))
                return false;

            int firstForwardIndex = _currentIndex + 1;
            if (firstForwardIndex < _entries.Count)
                _entries.RemoveRange(firstForwardIndex, _entries.Count - firstForwardIndex);

            _entries.Add(snapshot);
            _currentIndex = _entries.Count - 1;
            Trim();
            return true;
        }

        /// <summary>
        /// Finds the entry to move to when going back or forward from the current entry. Entries that can't be resolved are skipped.
        /// </summary>
        /// <param name="steps">The number of entries to move by. Negative values go back, positive values go forward.</param>
        /// <returns>
        /// Returns the index of the found entry. If there are fewer valid entries than the given number of steps, returns the furthest
        /// valid entry in that direction. Returns -1 if there's no valid entry in that direction.
        /// </returns>
        public int FindTarget(int steps)
        {
            if (steps == 0)
                return -1;

            int direction = steps > 0 ? 1 : -1;
            int remainingSteps = Math.Abs(steps);
            int target = -1;

            for (int i = _currentIndex + direction; i >= 0 && i < _entries.Count && remainingSteps > 0; i += direction)
            {
                if (!_entries[i].CanResolve())
                    continue;

                target = i;
                remainingSteps--;
            }

            return target;
        }

        /// <summary>
        /// Makes the entry at the given index the current one.
        /// </summary>
        /// <param name="index">The index of the entry.</param>
        /// <returns>Returns true if the current entry has changed, or false if the index is out of range.</returns>
        public bool SetCurrentIndex(int index)
        {
            if (index < 0 || index >= _entries.Count)
                return false;

            _currentIndex = index;
            return true;
        }

        /// <summary>
        /// Removes all the entries.
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
            _currentIndex = -1;
        }

        /// <summary>
        /// Removes the objects that don't exist anymore from the entries, and the entries that become empty. If the current entry is
        /// removed, the previous remaining entry becomes the current one.
        /// </summary>
        /// <inheritdoc cref="SelectionSnapshot.Clean" path="/remarks"/>
        /// <returns>Returns true if at least one object or entry has been removed.</returns>
        public bool Clean()
        {
            bool hasChanged = false;
            int newCurrentIndex = -1;
            List<SelectionSnapshot> remainingEntries = new List<SelectionSnapshot>(_entries.Count);

            for (int i = 0; i < _entries.Count; i++)
            {
                SelectionSnapshot entry = _entries[i];
                if (entry.Clean())
                    hasChanged = true;

                if (entry.IsEmpty)
                    hasChanged = true;
                else
                    remainingEntries.Add(entry);

                // If the current entry has been removed, this points to the previous remaining entry
                if (i == _currentIndex)
                    newCurrentIndex = remainingEntries.Count - 1;
            }

            if (newCurrentIndex < 0 && remainingEntries.Count > 0)
                newCurrentIndex = 0;

            _entries.Clear();
            _entries.AddRange(remainingEntries);
            _currentIndex = newCurrentIndex;
            return hasChanged;
        }

        /// <summary>
        /// Captures again the identifiers of the objects that are still alive in all the entries.
        /// </summary>
        /// <inheritdoc cref="ObjectRef.Refresh" path="/remarks"/>
        public void Refresh()
        {
            foreach (SelectionSnapshot entry in _entries)
                entry.Refresh();
        }

        #endregion


        #region Private API

        /// <summary>
        /// Discards the oldest entries if the history exceeds its maximum number of entries.
        /// </summary>
        private void Trim()
        {
            int excess = _entries.Count - _maxCount;
            if (excess <= 0)
                return;

            _entries.RemoveRange(0, excess);
            _currentIndex = Mathf.Max(0, _currentIndex - excess);
        }

        #endregion

    }

}
