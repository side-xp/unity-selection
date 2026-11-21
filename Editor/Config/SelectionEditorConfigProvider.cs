using UnityEditor;

using SideXP.Core.EditorOnly;

namespace SideXP.Selection
{

    /// <summary>
    /// Generate menus to edit the Selection package editor config.
    /// </summary>
    public static class SelectionEditorConfigProvider
    {

        /// <summary>
        /// Registers the menu to edit the Selection package settings from the Preferences window.
        /// </summary>
        /// <returns>Returns the created settings provider.</returns>
        [SettingsProvider]
        private static SettingsProvider RegisterUserSettingsMenu()
        {
            return EditorConfigSettingsProvider.Create<SelectionEditorConfig>(
                EditorConstants.Preferences + "/Selection",
                SettingsScope.User
            );
        }

    }

}
