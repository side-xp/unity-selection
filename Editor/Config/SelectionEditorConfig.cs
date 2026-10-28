using UnityEngine;

using SideXP.Core.EditorOnly;

namespace SideXP.Selection
{

    /// <summary>
    /// Editor config for the Selection package features.
    /// </summary>
    /// <remarks>Stored in the user settings, since these are personal preferences.</remarks>
    [EditorConfig(EEditorConfigScope.User)]
    public class SelectionEditorConfig : ScriptableObject, IEditorConfig
    {

        #region Fields

        /// <summary>
        /// The default value of <see cref="_commitDelay"/>.
        /// </summary>
        public const float DefaultCommitDelay = .3f;

        [Header("History")]

        [SerializeField, Min(1)]
        [Tooltip("The maximum number of selections recorded in the history. The oldest ones are discarded when this limit is exceeded.")]
        private int _historyMaxCount = SelectionHistoryBuffer.DefaultMaxCount;

        [SerializeField, Min(0)]
        [Tooltip("The delay (in seconds) during which a selection must remain unchanged before being recorded in the history. Some " +
            "interactions (like rectangle selection in the Scene view) change the selection many times in a row, and only the final " +
            "selection is relevant.")]
        private float _commitDelay = DefaultCommitDelay;

        [SerializeField]
        [Tooltip("If enabled, the folders browsed from the folder tree of the Project window (two columns layout) are recorded in the " +
            "history, even though Unity doesn't select them.")]
        private bool _shouldRecordBrowsedFolders = true;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="IEditorConfig.PostLoad"/>
        public void PostLoad() { }

        #endregion


        #region Public API

        /// <summary>
        /// Gets the loaded settings or load them from disk if not already.
        /// </summary>
        public static SelectionEditorConfig Instance => EditorConfigUtility.Get<SelectionEditorConfig>();

        /// <inheritdoc cref="Instance"/>
        public static SelectionEditorConfig I => Instance;

        /// <inheritdoc cref="_historyMaxCount"/>
        /// <remarks>Can't be lower than 1.</remarks>
        public int HistoryMaxCount
        {
            get => Mathf.Max(1, _historyMaxCount);
            set => _historyMaxCount = Mathf.Max(1, value);
        }

        /// <inheritdoc cref="_commitDelay"/>
        /// <remarks>Can't be negative.</remarks>
        public float CommitDelay
        {
            get => Mathf.Max(0, _commitDelay);
            set => _commitDelay = Mathf.Max(0, value);
        }

        /// <inheritdoc cref="_shouldRecordBrowsedFolders"/>
        public bool ShouldRecordBrowsedFolders
        {
            get => _shouldRecordBrowsedFolders;
            set => _shouldRecordBrowsedFolders = value;
        }

        #endregion

    }

}
