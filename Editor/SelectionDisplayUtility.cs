using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace SideXP.Selection
{

    /// <summary>
    /// Utility functions for displaying selections on GUI.
    /// </summary>
    public static class SelectionDisplayUtility
    {

        #region Fields

        /// <summary>
        /// The maximum number of object names used to name a selection.
        /// </summary>
        public const int MaxNamedObjectsCount = 3;

        /// <summary>
        /// The maximum length of a selection name, including <see cref="Ellipsis"/> if the name is truncated.
        /// </summary>
        public const int MaxNameLength = 40;

        /// <summary>
        /// The text appended to a truncated name.
        /// </summary>
        public const string Ellipsis = "...";

        private const string NameSeparator = ", ";

        /// <summary>
        /// The characters considered as word boundaries when truncating a name.
        /// </summary>
        private static readonly char[] WordSeparators = { ' ', ',' };

        #endregion


        #region Public API

        /// <summary>
        /// Gets the objects used to name and illustrate a given selection: the objects that are not temporary (assets and objects saved in
        /// a scene), or all the objects if they're all temporary.
        /// </summary>
        /// <param name="snapshot">The selection to name.</param>
        /// <returns>Returns the objects to use, in selection order.</returns>
        public static List<ObjectRef> GetNamedObjects(SelectionSnapshot snapshot)
        {
            List<ObjectRef> namedObjects = new List<ObjectRef>();
            if (snapshot == null)
                return namedObjects;

            foreach (ObjectRef objRef in snapshot.Objects)
            {
                if (objRef.IsPersistent)
                    namedObjects.Add(objRef);
            }

            if (namedObjects.Count <= 0)
                namedObjects.AddRange(snapshot.Objects);

            return namedObjects;
        }

        /// <summary>
        /// Gets the object that represents a given selection, for example to display its icon.
        /// </summary>
        /// <param name="snapshot">The selection.</param>
        /// <returns>
        /// Returns the first of the objects returned by <see cref="GetNamedObjects(SelectionSnapshot)"/>, or null if the selection is
        /// empty.
        /// </returns>
        public static ObjectRef GetMainObject(SelectionSnapshot snapshot)
        {
            List<ObjectRef> namedObjects = GetNamedObjects(snapshot);
            return namedObjects.Count > 0 ? namedObjects[0] : null;
        }

        /// <summary>
        /// Gets the display name of a given selection: the names of its first objects separated by commas, truncated by word if it's too
        /// long.
        /// </summary>
        /// <remarks>
        /// Uses <see cref="GetNamedObjects(SelectionSnapshot)"/> to choose the objects, with their names as they were when the selection
        /// was captured, at most <see cref="MaxNamedObjectsCount"/> of them, and truncated to <see cref="MaxNameLength"/> characters.
        /// </remarks>
        /// <param name="snapshot">The selection to name.</param>
        /// <returns>Returns the display name, or an empty string if the selection is empty.</returns>
        public static string GetName(SelectionSnapshot snapshot)
        {
            List<ObjectRef> namedObjects = GetNamedObjects(snapshot);
            int count = namedObjects.Count < MaxNamedObjectsCount ? namedObjects.Count : MaxNamedObjectsCount;

            string[] names = new string[count];
            for (int i = 0; i < count; i++)
                names[i] = namedObjects[i].Name;

            return TruncateByWord(string.Join(NameSeparator, names), MaxNameLength);
        }

        /// <summary>
        /// Gets a message describing the objects of a given selection that can't be selected, grouped by the scene that must be loaded
        /// to select them.
        /// </summary>
        /// <param name="snapshot">The selection to check.</param>
        /// <returns>Returns the message, or an empty string if all the objects of the selection can be selected.</returns>
        public static string GetUnavailableObjectsMessage(SelectionSnapshot snapshot)
        {
            if (snapshot == null)
                return string.Empty;

            // Using a list of scene names alongside the dictionary to keep the order in which scenes are found
            List<string> unloadedScenes = new List<string>();
            Dictionary<string, int> unloadedCounts = new Dictionary<string, int>();
            int missingCount = 0;

            foreach (ObjectRef objRef in snapshot.Objects)
            {
                EObjectRefState state = objRef.GetState();
                if (state == EObjectRefState.Resolved)
                    continue;

                if (state == EObjectRefState.Missing)
                {
                    missingCount++;
                    continue;
                }

                string sceneName = string.IsNullOrEmpty(objRef.SceneName) ? objRef.ScenePath : objRef.SceneName;
                if (unloadedCounts.TryGetValue(sceneName, out int count))
                {
                    unloadedCounts[sceneName] = count + 1;
                }
                else
                {
                    unloadedScenes.Add(sceneName);
                    unloadedCounts[sceneName] = 1;
                }
            }

            if (unloadedScenes.Count <= 0 && missingCount <= 0)
                return string.Empty;

            StringBuilder builder = new StringBuilder("Some objects of the restored selection can't be selected:");
            foreach (string sceneName in unloadedScenes)
                builder.Append($"\n- {CountObjects(unloadedCounts[sceneName])} in the scene \"{sceneName}\", which is not loaded");

            if (missingCount > 0)
            {
                string verb = missingCount > 1 ? "don't" : "doesn't";
                builder.Append($"\n- {CountObjects(missingCount)} that {verb} exist anymore");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Truncates a given text at the end of the last whole word that fits, and appends <see cref="Ellipsis"/> to it.
        /// </summary>
        /// <param name="text">The text to truncate.</param>
        /// <param name="maxLength">The maximum length of the result, including <see cref="Ellipsis"/>.</param>
        /// <returns>
        /// Returns the text as is if it's not longer than the given length, or the truncated text. A single word longer than the available
        /// space is cut in the middle.
        /// </returns>
        public static string TruncateByWord(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text ?? string.Empty;

            int availableLength = maxLength - Ellipsis.Length;
            if (availableLength <= 0)
                return Ellipsis.Substring(0, maxLength > 0 ? maxLength : 0);

            // If the character right after the available space is a separator, the last word fits entirely
            int cutIndex = IsWordSeparator(text[availableLength])
                ? availableLength
                : text.LastIndexOfAny(WordSeparators, availableLength - 1) + 1;

            string truncated = cutIndex > 0
                ? text.Substring(0, cutIndex).TrimEnd(WordSeparators)
                : string.Empty;

            // A single word is longer than the available space
            if (truncated.Length <= 0)
                truncated = text.Substring(0, availableLength);

            return $"{truncated}{Ellipsis}";
        }

        #endregion


        #region Internal API

        /// <summary>
        /// Logs a warning if some objects of a given selection can't be selected. Meant to be called after restoring a selection.
        /// </summary>
        /// <param name="snapshot">The restored selection.</param>
        internal static void LogUnavailableObjects(SelectionSnapshot snapshot)
        {
            string message = GetUnavailableObjectsMessage(snapshot);
            if (!string.IsNullOrEmpty(message))
                Debug.LogWarning(message);
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gets the given number of objects as text (for example "1 object" or "3 objects").
        /// </summary>
        /// <param name="count">The number of objects.</param>
        /// <returns>Returns the number of objects as text.</returns>
        private static string CountObjects(int count)
        {
            return count > 1 ? $"{count} objects" : $"{count} object";
        }

        /// <summary>
        /// Checks if the given character is considered as a word boundary.
        /// </summary>
        /// <param name="character">The character to check.</param>
        /// <returns>Returns true if the character is a word separator.</returns>
        private static bool IsWordSeparator(char character)
        {
            return Array.IndexOf(WordSeparators, character) >= 0;
        }

        #endregion

    }

}
