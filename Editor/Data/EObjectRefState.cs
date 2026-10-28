namespace SideXP.Selection
{

    /// <summary>
    /// Defines the state of an <see cref="ObjectRef"/> when trying to resolve it.
    /// </summary>
    public enum EObjectRefState
    {

        /// <summary>
        /// The referenced object has been found.
        /// </summary>
        Resolved,

        /// <summary>
        /// The referenced object is saved in a scene that is not loaded. It may be resolved again once that scene is loaded.
        /// </summary>
        Unloaded,

        /// <summary>
        /// The referenced object doesn't exist anymore, or it can't be identified anymore.
        /// </summary>
        Missing,

    }

}
