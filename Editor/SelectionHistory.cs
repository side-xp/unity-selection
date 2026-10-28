using System;
using System.Collections.Generic;

using UnityEditor;

using UnitySelection = UnityEditor.Selection;

namespace SideXP.Selection
{

    /// <summary>
    /// Tracks the selection changes in the editor, and allows navigating through the previous selections.
    /// </summary>
    /// <remarks>
    /// Empty selections are not recorded. A selection is recorded only once it has remained unchanged for
    /// <see cref="SelectionEditorConfig.CommitDelay"/>, so interactions that change the selection many times in a row (like rectangle
    /// selection in the Scene view) produce a single entry.
    /// </remarks>
    [InitializeOnLoad]
    public static class SelectionHistory
    {

        #region Fields

        /// <summary>
        /// The delay (in seconds) during which the current selection must remain unchanged before being recorded. Read from the editor
        /// config when the selection changes, so the config is not queried on every editor update.
        /// </summary>
        private static double s_commitDelay = SelectionEditorConfig.DefaultCommitDelay;

        /// <summary>
        /// Indicates if the selection has changed since it has been recorded for the last time.
        /// </summary>
        private static bool s_isCommitPending = false;

        /// <summary>
        /// The time of the last selection change, from <see cref="EditorApplication.timeSinceStartup"/>.
        /// </summary>
        private static double s_lastChangeTime = 0;

        /// <summary>
        /// Called when the history has changed (new entry, navigation, ...).
        /// </summary>
        public static event Action OnChanged;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Static constructor, called when the editor is loaded.
        /// </summary>
        static SelectionHistory()
        {
            UnitySelection.selectionChanged += HandleSelectionChanged;
            EditorApplication.update += HandleEditorUpdate;

            // Record the current selection, in case it's not already in the history
            HandleSelectionChanged();
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="SelectionHistoryBuffer.Entries"/>
        public static IReadOnlyList<SelectionSnapshot> Entries => Buffer.Entries;

        /// <inheritdoc cref="SelectionHistoryBuffer.CurrentIndex"/>
        public static int CurrentIndex => Buffer.CurrentIndex;

        /// <inheritdoc cref="SelectionHistoryBuffer.Current"/>
        public static SelectionSnapshot Current => Buffer.Current;

        /// <summary>
        /// Checks if there's a previous entry that can be selected.
        /// </summary>
        public static bool CanGoBack => Buffer.FindTarget(-1) >= 0;

        /// <summary>
        /// Checks if there's a next entry that can be selected.
        /// </summary>
        public static bool CanGoForward => Buffer.FindTarget(1) >= 0;

        /// <summary>
        /// Selects a previous entry of the history. Entries that can't be resolved are skipped.
        /// </summary>
        /// <param name="count">The number of entries to go back by. If there are fewer previous entries, selects the oldest one.</param>
        /// <returns>Returns true if a previous entry has been selected.</returns>
        public static bool Back(int count = 1)
        {
            return count > 0 && Move(-count);
        }

        /// <summary>
        /// Selects a next entry of the history. Entries that can't be resolved are skipped.
        /// </summary>
        /// <param name="count">The number of entries to go forward by. If there are fewer next entries, selects the most recent one.</param>
        /// <returns>Returns true if a next entry has been selected.</returns>
        public static bool Forward(int count = 1)
        {
            return count > 0 && Move(count);
        }

        /// <summary>
        /// Selects the entry at the given index of the history.
        /// </summary>
        /// <remarks>
        /// If the current selection has not been recorded yet, it's discarded, so the given index still targets the expected entry.
        /// </remarks>
        /// <param name="index">The index of the entry to select.</param>
        /// <returns>
        /// Returns true if the entry has been selected, or false if the index is out of range or the entry can't be resolved.
        /// </returns>
        public static bool GoTo(int index)
        {
            if (index < 0 || index >= Buffer.Count)
                return false;

            if (!Buffer.Entries[index].Apply())
                return false;

            // The resulting selection change matches the new current entry, so it won't be recorded
            s_isCommitPending = false;
            Buffer.SetCurrentIndex(index);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Removes all the entries of the history, and records the current selection as the first entry.
        /// </summary>
        public static void Clear()
        {
            Buffer.Clear();
            Commit();
            OnChanged?.Invoke();
        }

        /// <inheritdoc cref="SelectionHistoryBuffer.Clean"/>
        public static bool Clean()
        {
            if (!Buffer.Clean())
                return false;

            OnChanged?.Invoke();
            return true;
        }

        /// <inheritdoc cref="SelectionHistoryBuffer.Refresh"/>
        public static void RefreshReferences()
        {
            Buffer.Refresh();
        }

        /// <summary>
        /// Records the current selection now if it has changed, instead of waiting for <see cref="SelectionEditorConfig.CommitDelay"/>.
        /// </summary>
        public static void Flush()
        {
            if (s_isCommitPending && Commit())
                OnChanged?.Invoke();
        }

        #endregion


        #region Private API

        /// <inheritdoc cref="SelectionHistoryState.Buffer"/>
        private static SelectionHistoryBuffer Buffer => SelectionHistoryState.instance.Buffer;

        /// <summary>
        /// Selects the entry at the given number of steps from the current one.
        /// </summary>
        /// <param name="steps">The number of entries to move by. Negative values go back, positive values go forward.</param>
        /// <returns>Returns true if an entry has been selected.</returns>
        private static bool Move(int steps)
        {
            // Record the current selection first, so it's possible to come back to it
            Flush();

            int target = Buffer.FindTarget(steps);
            return target >= 0 && GoTo(target);
        }

        /// <summary>
        /// Records the current selection in the history.
        /// </summary>
        /// <returns>Returns true if a new entry has been added.</returns>
        private static bool Commit()
        {
            s_isCommitPending = false;

            // Applied here so changes made from the Preferences window are taken into account without being notified
            Buffer.MaxCount = SelectionEditorConfig.I.HistoryMaxCount;

            // Check before capturing a snapshot, since it's costly for large selections
            SelectionSnapshot current = Buffer.Current;
            if (current != null && current.Matches(UnitySelection.objects))
                return false;

            SelectionSnapshot snapshot = SelectionSnapshot.FromCurrentSelection();
            return !snapshot.IsEmpty && Buffer.Push(snapshot);
        }

        /// <summary>
        /// Called when the selection changes in the editor.
        /// </summary>
        private static void HandleSelectionChanged()
        {
            s_isCommitPending = true;
            s_lastChangeTime = EditorApplication.timeSinceStartup;
            s_commitDelay = SelectionEditorConfig.I.CommitDelay;
        }

        /// <summary>
        /// Called on each editor update, to record the selection once it has remained unchanged for <see cref="s_commitDelay"/>.
        /// </summary>
        private static void HandleEditorUpdate()
        {
            if (s_isCommitPending && EditorApplication.timeSinceStartup - s_lastChangeTime >= s_commitDelay)
                Flush();
        }

        #endregion

    }

}
