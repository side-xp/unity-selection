using UnityEngine;
using UnityEditor;

namespace SideXP.Selection
{

    /// <summary>
    /// Holds the selection history, so it survives domain reloads.
    /// </summary>
    /// <remarks>Not saved on disk: the history is reset when the editor is restarted.</remarks>
    internal class SelectionHistoryState : ScriptableSingleton<SelectionHistoryState>
    {

        #region Fields

        /// <summary>
        /// The selection history.
        /// </summary>
        [SerializeField]
        private SelectionHistoryBuffer _buffer = new SelectionHistoryBuffer();

        #endregion


        #region Public API

        /// <inheritdoc cref="_buffer"/>
        public SelectionHistoryBuffer Buffer => _buffer;

        #endregion

    }

}
