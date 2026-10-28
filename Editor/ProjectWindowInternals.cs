using System;
using System.Reflection;

using UnityEngine;
using UnityEditor;

namespace SideXP.Selection
{

    /// <summary>
    /// Reads the state of the Project window that Unity doesn't expose publicly, using reflection.
    /// </summary>
    /// <remarks>
    /// The members used here are internal to Unity, so they may change between versions. If one of them can't be found or throws when
    /// it's used, these features are disabled until the next domain reload, and a warning is logged (once per editor session).
    /// </remarks>
    internal static class ProjectWindowInternals
    {

        #region Fields

        private const string ProjectBrowserTypeName = "UnityEditor.ProjectBrowser";
        private const string TryGetActiveFolderPathMethodName = "TryGetActiveFolderPath";

        /// <summary>
        /// The <see cref="SessionState"/> key used to log the warning about missing members only once per editor session, instead of
        /// after each domain reload.
        /// </summary>
        private const string HasLoggedWarningKey = "SideXP.Selection.ProjectWindowInternals.HasLoggedWarning";

        /// <summary>
        /// The signature of the internal <c>ProjectWindowUtil.TryGetActiveFolderPath(out string)</c> function.
        /// </summary>
        /// <param name="path">Outputs the path of the folder browsed in the Project window.</param>
        /// <returns>Returns true if there's a Project window that uses the two columns layout.</returns>
        private delegate bool TryGetActiveFolderPathDelegate(out string path);

        /// <summary>
        /// Indicates if the internal members have been looked for.
        /// </summary>
        private static bool s_isInitialized = false;

        /// <summary>
        /// The internal type of the Project window, or null if it can't be found.
        /// </summary>
        private static Type s_projectBrowserType = null;

        /// <inheritdoc cref="TryGetActiveFolderPathDelegate"/>
        private static TryGetActiveFolderPathDelegate s_tryGetActiveFolderPath = null;

        #endregion


        #region Internal API

        /// <summary>
        /// Checks if the state of the Project window can be read with this Unity version.
        /// </summary>
        internal static bool IsAvailable
        {
            get
            {
                Initialize();
                return s_projectBrowserType != null && s_tryGetActiveFolderPath != null;
            }
        }

        /// <summary>
        /// Gets the path of the folder browsed in the last Project window the user interacted with.
        /// </summary>
        /// <remarks>
        /// While searching in the Project window, the browsed path is "Assets", whatever the folder displayed before the search.
        /// </remarks>
        /// <param name="path">Outputs the path of the browsed folder (like "Assets/Prefabs"), or null if it can't be read.</param>
        /// <returns>
        /// Returns true if the path has been read, or false if there's no Project window, if it uses the one column layout, or if the
        /// Project window can't be read with this Unity version.
        /// </returns>
        internal static bool TryGetBrowsedFolderPath(out string path)
        {
            path = null;
            if (!IsAvailable)
                return false;

            try
            {
                return s_tryGetActiveFolderPath(out path);
            }
            catch (Exception exception)
            {
                Disable($"reading the browsed folder failed ({exception.GetType().Name}: {exception.Message})");
                path = null;
                return false;
            }
        }

        /// <summary>
        /// Checks if a Project window is the focused window.
        /// </summary>
        /// <returns>
        /// Returns true if a Project window is focused, or false if not or if it can't be identified with this Unity version.
        /// </returns>
        internal static bool IsProjectWindowFocused()
        {
            EditorWindow focusedWindow = EditorWindow.focusedWindow;
            return IsAvailable && focusedWindow != null && focusedWindow.GetType() == s_projectBrowserType;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Looks for the internal members used by this class, if not already done.
        /// </summary>
        private static void Initialize()
        {
            if (s_isInitialized)
                return;

            s_isInitialized = true;
            Assembly editorAssembly = typeof(ProjectWindowUtil).Assembly;
            s_projectBrowserType = editorAssembly.GetType(ProjectBrowserTypeName, false);
            if (s_projectBrowserType == null)
            {
                Disable($"the type {ProjectBrowserTypeName} can't be found");
                return;
            }

            MethodInfo method = typeof(ProjectWindowUtil).GetMethod(
                TryGetActiveFolderPathMethodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(string).MakeByRefType() },
                null);

            // Returns null instead of throwing if the signature of the method has changed
            s_tryGetActiveFolderPath = method != null
                ? (TryGetActiveFolderPathDelegate)Delegate.CreateDelegate(typeof(TryGetActiveFolderPathDelegate), method, false)
                : null;

            if (s_tryGetActiveFolderPath == null)
                Disable($"the function {nameof(ProjectWindowUtil)}.{TryGetActiveFolderPathMethodName}(out string) can't be found");
        }

        /// <summary>
        /// Disables the features of this class until the next domain reload, and logs a warning if not already done in this editor
        /// session.
        /// </summary>
        /// <param name="reason">The reason why the Project window can't be read.</param>
        private static void Disable(string reason)
        {
            s_isInitialized = true;
            s_projectBrowserType = null;
            s_tryGetActiveFolderPath = null;

            if (SessionState.GetBool(HasLoggedWarningKey, false))
                return;

            SessionState.SetBool(HasLoggedWarningKey, true);
            Debug.LogWarning("The folders browsed from the Project window can't be recorded in the selection history with this Unity " +
                $"version: {reason}.");
        }

        #endregion

    }

}
