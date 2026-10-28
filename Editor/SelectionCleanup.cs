using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SideXP.Selection
{

    /// <summary>
    /// Cleans the recorded selections when objects may have been lost (scenes opened, closed or created, edit/play mode switches), and
    /// refreshes the object references when a scene is saved.
    /// </summary>
    /// <remarks>
    /// Cleaning during play mode only happens on edit/play mode switches (or after a domain reload), so objects spawned during the play
    /// session can still be selected until play mode is exited.
    /// </remarks>
    [InitializeOnLoad]
    internal static class SelectionCleanup
    {

        #region Fields

        /// <summary>
        /// Indicates if a cleaning pass is already scheduled for the next editor update.
        /// </summary>
        private static bool s_isCleanPending = false;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Static constructor, called when the editor is loaded.
        /// </summary>
        static SelectionCleanup()
        {
            EditorSceneManager.sceneOpened += HandleSceneOpened;
            EditorSceneManager.sceneClosed += HandleSceneClosed;
            EditorSceneManager.newSceneCreated += HandleNewSceneCreated;
            EditorSceneManager.sceneSaved += HandleSceneSaved;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;

            // The slots saved on disk may reference temporary objects from a previous session
            ScheduleClean();
        }

        #endregion


        #region Private API

        /// <summary>
        /// Schedules a cleaning pass for the next editor update. Opening a multi-scene setup triggers several events in a row, which
        /// results in a single cleaning pass.
        /// </summary>
        private static void ScheduleClean()
        {
            if (s_isCleanPending)
                return;

            s_isCleanPending = true;
            EditorApplication.delayCall += Clean;
        }

        /// <summary>
        /// Removes the objects that don't exist anymore from the recorded selections.
        /// </summary>
        private static void Clean()
        {
            s_isCleanPending = false;
            SelectionHistory.Clean();
            SelectionSlots.Clean();
        }

        /// <inheritdoc cref="EditorSceneManager.SceneOpenedCallback"/>
        private static void HandleSceneOpened(Scene scene, OpenSceneMode mode)
        {
            ScheduleClean();
        }

        /// <inheritdoc cref="EditorSceneManager.SceneClosedCallback"/>
        private static void HandleSceneClosed(Scene scene)
        {
            ScheduleClean();
        }

        /// <inheritdoc cref="EditorSceneManager.NewSceneCreatedCallback"/>
        private static void HandleNewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            ScheduleClean();
        }

        /// <summary>
        /// Called when a scene is saved, so the objects that were not saved before get a stable id.
        /// </summary>
        /// <param name="scene">The saved scene.</param>
        private static void HandleSceneSaved(Scene scene)
        {
            SelectionHistory.RefreshReferences();
            SelectionSlots.RefreshReferences();
        }

        /// <summary>
        /// Called when the editor switches between edit and play mode.
        /// </summary>
        /// <param name="state">The new play mode state.</param>
        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.EnteredPlayMode)
                ScheduleClean();
        }

        #endregion

    }

}
