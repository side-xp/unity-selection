using UnityEngine;
using UnityEditor;

namespace SideXP.Selection
{

    /// <summary>
    /// Holds the selection slots, so they survive domain reloads and editor restarts.
    /// </summary>
    /// <remarks>
    /// Saved in the /UserSettings folder, since the saved selections are personal. References to scene objects are not written on disk
    /// (only their identifiers are), so they're resolved again from their <see cref="GlobalObjectId"/> or hierarchy path once the editor
    /// is restarted.
    /// </remarks>
    [FilePath("UserSettings/SideXP.Selection.SelectionSlots.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class SelectionSlotsState : ScriptableSingleton<SelectionSlotsState>
    {

        #region Fields

        /// <summary>
        /// The selection slots.
        /// </summary>
        [SerializeField]
        private SelectionSlotCollection _slots = new SelectionSlotCollection();

        #endregion


        #region Public API

        /// <inheritdoc cref="_slots"/>
        /// <remarks>Resized to <see cref="SelectionSlotCollection.DefaultCount"/> if the saved data has another number of slots.</remarks>
        public SelectionSlotCollection Slots
        {
            get
            {
                if (_slots.Count != SelectionSlotCollection.DefaultCount)
                    _slots.Resize(SelectionSlotCollection.DefaultCount);
                return _slots;
            }
        }

        /// <summary>
        /// Writes the selection slots on disk.
        /// </summary>
        public void SaveToDisk()
        {
            Save(true);
        }

        #endregion

    }

}
