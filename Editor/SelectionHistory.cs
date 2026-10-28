using System;
using System.Collections.Generic;

using UnityEditor;

using Object = UnityEngine.Object;
using UnitySelection = UnityEditor.Selection;

namespace SideXP.Selection
{

    /// <summary>
    /// Tracks the selection changes in the editor, and allows navigating through the previous selections.
    /// </summary>
    /// <remarks>
    /// Empty selections are not recorded. A selection is recorded only once it has remained unchanged for
    /// <see cref="SelectionEditorConfig.CommitDelay"/>, so interactions that change the selection many times in a row (like rectangle
    /// selection in the Scene view) produce a single entry.<br/>
    /// The folders browsed from the folder tree of the Project window (two columns layout) are recorded the same way, as if they were
    /// selected (see <see cref="SelectionEditorConfig.ShouldRecordBrowsedFolders"/>).
    /// </remarks>
    [InitializeOnLoad]
    public static class SelectionHistory
    {

        #region Fields

        /// <summary>
        /// The path of the browsed folder reported by the Project window while searching. Since it's also the path of the root folder,
        /// browsing that folder can't be told apart from searching, so it's never recorded.
        /// </summary>
        private const string SearchFolderPath = "Assets";

        /// <summary>
        /// The delay (in seconds) during which the current selection must remain unchanged before being recorded. Read from the editor
        /// config when the selection changes, so the config is not queried on every editor update.
        /// </summary>
        private static double s_commitDelay = SelectionEditorConfig.DefaultCommitDelay;

        /// <summary>
        /// Indicates if the selection (or the browsed folder) has changed since it has been recorded for the last time.
        /// </summary>
        private static bool s_isCommitPending = false;

        /// <summary>
        /// The time of the last selection change (or browsed folder change), from <see cref="EditorApplication.timeSinceStartup"/>.
        /// </summary>
        private static double s_lastChangeTime = 0;

        /// <summary>
        /// The path of the folder browsed in the Project window on the last check, or null if it couldn't be read.
        /// </summary>
        private static string s_browsedFolderPath = null;

        /// <summary>
        /// The folder browsed from the Project window that is waiting to be recorded instead of the current selection, or null if the
        /// pending change is a selection change.
        /// </summary>
        private static Object s_pendingFolder = null;

        /// <summary>
        /// Indicates if the selection has changed since the browsed folder has been checked for the last time.
        /// </summary>
        private static bool s_hasSelectionChangedSinceFolderCheck = false;

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
        /// If the current selection has not been recorded yet, it's discarded, so the given index still targets the expected entry. The
        /// objects of the entry that can't be resolved are skipped, and a warning is logged to list them.
        /// </remarks>
        /// <param name="index">The index of the entry to select.</param>
        /// <returns>
        /// Returns true if the entry has been selected, or false if the index is out of range or the entry can't be resolved.
        /// </returns>
        public static bool GoTo(int index)
        {
            if (index < 0 || index >= Buffer.Count)
                return false;

            SelectionSnapshot entry = Buffer.Entries[index];
            bool isApplied = entry.Apply();
            SelectionDisplayUtility.LogUnavailableObjects(entry);
            if (!isApplied)
                return false;

            // The resulting selection change matches the new current entry, so it won't be recorded
            s_isCommitPending = false;
            s_pendingFolder = null;
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
        /// Records the current selection in the history, or the browsed folder waiting to be recorded if any.
        /// </summary>
        /// <returns>Returns true if a new entry has been added.</returns>
        private static bool Commit()
        {
            s_isCommitPending = false;

            // Applied here so changes made from the Preferences window are taken into account without being notified
            Buffer.MaxCount = SelectionEditorConfig.I.HistoryMaxCount;

            // The pending folder may have been deleted since it has been browsed, in which case the current selection is recorded
            bool isFolderPending = s_pendingFolder != null;
            Object[] objects = isFolderPending ? new Object[] { s_pendingFolder } : UnitySelection.objects;
            Object activeObject = isFolderPending ? s_pendingFolder : UnitySelection.activeObject;
            s_pendingFolder = null;

            // Check before capturing a snapshot, since it's costly for large selections
            SelectionSnapshot current = Buffer.Current;
            if (current != null && current.Matches(objects))
                return false;

            SelectionSnapshot snapshot = new SelectionSnapshot(objects, activeObject);
            return !snapshot.IsEmpty && Buffer.Push(snapshot);
        }

        /// <summary>
        /// Schedules the recording of the current selection (or of <see cref="s_pendingFolder"/>), once it has remained unchanged for
        /// <see cref="s_commitDelay"/>.
        /// </summary>
        private static void ScheduleCommit()
        {
            s_isCommitPending = true;
            s_lastChangeTime = EditorApplication.timeSinceStartup;
            s_commitDelay = SelectionEditorConfig.I.CommitDelay;
        }

        /// <summary>
        /// Checks if another folder has been browsed in the Project window, and schedules its recording if it has been browsed from the
        /// folder tree.
        /// </summary>
        /// <remarks>
        /// Browsing a folder from the folder tree of the Project window (two columns layout) doesn't change the selection, so it can't be
        /// detected from <see cref="UnitySelection.selectionChanged"/>.
        /// </remarks>
        private static void CheckBrowsedFolder()
        {
            bool hasSelectionChanged = s_hasSelectionChangedSinceFolderCheck;
            s_hasSelectionChangedSinceFolderCheck = false;

            if (!ProjectWindowInternals.TryGetBrowsedFolderPath(out string path))
            {
                // The next path read is used as reference, so opening a Project window doesn't record the folder it displays
                s_browsedFolderPath = null;
                return;
            }

            // The search path is not kept as reference, so ending a search doesn't record the folder displayed before it
            if (path == s_browsedFolderPath || path == SearchFolderPath)
                return;

            string previousPath = s_browsedFolderPath;
            s_browsedFolderPath = path;
            if (previousPath == null)
                return;

            /**
             * @note The Project window also displays the folder of the assets selected or pinged from anywhere (including when navigating
             * through this history), which must not be recorded. Pinging an asset (like clicking an object field in the Inspector) doesn't
             * change the selection, but is done from another window. Selecting an asset while the Project window is focused (like with
             * the Back shortcut) is notified in the same editor update as the browsed folder change.
             */
            if (hasSelectionChanged || !ProjectWindowInternals.IsProjectWindowFocused())
                return;

            if (!SelectionEditorConfig.I.ShouldRecordBrowsedFolders)
                return;

            // Some browsable folders are not assets (like the "Packages" root folder)
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (folder == null)
                return;

            s_pendingFolder = folder;
            ScheduleCommit();
        }

        /// <summary>
        /// Called when the selection changes in the editor.
        /// </summary>
        private static void HandleSelectionChanged()
        {
            // A folder browsed just before selecting objects is discarded, like any selection that changes before being recorded
            s_pendingFolder = null;
            s_hasSelectionChangedSinceFolderCheck = true;
            ScheduleCommit();
        }

        /// <summary>
        /// Called on each editor update, to record the selection once it has remained unchanged for <see cref="s_commitDelay"/>, and to
        /// detect the folders browsed from the Project window.
        /// </summary>
        private static void HandleEditorUpdate()
        {
            if (s_isCommitPending && EditorApplication.timeSinceStartup - s_lastChangeTime >= s_commitDelay)
                Flush();

            // Checked after recording, so a browsed folder is not recorded before a selection change that may have caused it is notified
            CheckBrowsedFolder();
        }

        #endregion

    }

}
