using UnityEditor;

using SideXP.Core.EditorOnly;

namespace SideXP.Selection
{

    /// <summary>
    /// Generate menus to edit the Selection package editor config.
    /// </summary>
    public class SelectionEditorConfigProvider : DefaultConfigSettingsProvider
    {

        /// <summary>
        /// Registers the menu to edit the Selection package settings from the Preferences window.
        /// </summary>
        /// <returns>Returns the created settings provider.</returns>
        [SettingsProvider]
        private static SettingsProvider RegisterUserSettingsMenu()
        {
            return MakeSettingsProvider(SelectionEditorConfig.I, EditorConstants.Preferences + "/Selection", SettingsScope.User);
        }

    }

}
