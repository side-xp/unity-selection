using System;
using System.Collections.Generic;

using UnityEngine;

using Object = UnityEngine.Object;
using UnitySelection = UnityEditor.Selection;

namespace SideXP.Selection
{

    /// <summary>
    /// Saved state of a selection, which can be applied again later.
    /// </summary>
    [Serializable]
    public class SelectionSnapshot
    {

        #region Fields

        /// <summary>
        /// The selected objects.
        /// </summary>
        [SerializeField]
        private List<ObjectRef> _objects = new List<ObjectRef>();

        /// <summary>
        /// The index of the active object in <see cref="_objects"/>, or -1 if there's no active object.
        /// </summary>
        [SerializeField]
        private int _activeIndex = -1;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Creates a snapshot of the given selection.
        /// </summary>
        /// <param name="objects">The selected objects. Null entries and duplicates are ignored.</param>
        /// <param name="activeObject">The active object of the selection. Ignored if it's not one of the selected objects.</param>
        public SelectionSnapshot(IEnumerable<Object> objects, Object activeObject = null)
        {
            if (objects == null)
                return;

            HashSet<Object> addedObjects = new HashSet<Object>();
            foreach (Object obj in objects)
            {
                if (obj == null || !addedObjects.Add(obj))
                    continue;

                if (obj == activeObject)
                    _activeIndex = _objects.Count;

                _objects.Add(new ObjectRef(obj));
            }
        }

        /// <summary>
        /// Creates a snapshot of the current selection in the editor.
        /// </summary>
        /// <returns>Returns the created snapshot.</returns>
        public static SelectionSnapshot FromCurrentSelection()
        {
            return new SelectionSnapshot(UnitySelection.objects, UnitySelection.activeObject);
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_objects"/>
        public IReadOnlyList<ObjectRef> Objects => _objects;

        /// <summary>
        /// The number of selected objects.
        /// </summary>
        public int Count => _objects.Count;

        /// <summary>
        /// Checks if this snapshot doesn't contain any object.
        /// </summary>
        public bool IsEmpty => _objects.Count <= 0;

        /// <summary>
        /// The reference to the active object of the selection, or null if there's no active object.
        /// </summary>
        public ObjectRef Active => _activeIndex >= 0 && _activeIndex < _objects.Count ? _objects[_activeIndex] : null;

        /// <summary>
        /// Resolves the selected objects.
        /// </summary>
        /// <param name="activeObject">Outputs the active object, or null if there's no active object or if it can't be resolved.</param>
        /// <returns>Returns the selected objects that can be resolved.</returns>
        public Object[] Resolve(out Object activeObject)
        {
            activeObject = null;
            List<Object> resolvedObjects = new List<Object>(_objects.Count);
            for (int i = 0; i < _objects.Count; i++)
            {
                Object obj = _objects[i].Resolve();
                if (obj == null)
                    continue;

                resolvedObjects.Add(obj);
                if (i == _activeIndex)
                    activeObject = obj;
            }
            return resolvedObjects.ToArray();
        }

        /// <inheritdoc cref="Resolve(out Object)"/>
        public Object[] Resolve()
        {
            return Resolve(out _);
        }

        /// <summary>
        /// Checks if at least one of the selected objects can be resolved.
        /// </summary>
        /// <returns>Returns true if this snapshot can be applied.</returns>
        public bool CanResolve()
        {
            foreach (ObjectRef objRef in _objects)
            {
                if (objRef.Resolve() != null)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the resolved objects of this snapshot are the given ones, regardless of their order. The active object is not
        /// compared.
        /// </summary>
        /// <param name="objects">The objects to compare. Null entries are ignored.</param>
        /// <returns>Returns true if the resolved objects are the same as the given ones.</returns>
        public bool Matches(IEnumerable<Object> objects)
        {
            HashSet<Object> otherObjects = new HashSet<Object>();
            if (objects != null)
            {
                foreach (Object obj in objects)
                {
                    if (obj != null)
                        otherObjects.Add(obj);
                }
            }

            return otherObjects.SetEquals(Resolve());
        }

        /// <param name="other">The snapshot to compare.</param>
        /// <inheritdoc cref="Matches(IEnumerable{Object})"/>
        public bool Matches(SelectionSnapshot other)
        {
            return other != null && Matches(other.Resolve());
        }

        /// <summary>
        /// Selects the objects of this snapshot in the editor. Objects that can't be resolved are skipped.
        /// </summary>
        /// <returns>Returns true if the selection has been applied, or false if none of the objects can be resolved.</returns>
        public bool Apply()
        {
            Object[] objects = Resolve(out Object activeObject);
            if (objects.Length <= 0)
                return false;

            if (activeObject != null)
            {
                /**
                 * @note Setting the active object first, and placing it first in the selected objects array, so it remains the active
                 * object once the whole selection is applied.
                 */
                UnitySelection.activeObject = activeObject;
                int activeIndex = Array.IndexOf(objects, activeObject);
                (objects[0], objects[activeIndex]) = (objects[activeIndex], objects[0]);
            }

            UnitySelection.objects = objects;
            return true;
        }

        /// <summary>
        /// Removes the objects that don't exist anymore, or that can't be identified anymore (objects not saved in a scene that has been
        /// closed or reloaded, objects spawned in play mode once back to edit mode, ...). Objects saved in a scene that is not loaded are
        /// kept, since they can be resolved again once that scene is loaded.
        /// </summary>
        /// <remarks>
        /// The objects that don't have a stable id but are still resolved are refreshed, so they get one if possible (for example, an
        /// object selected in play mode that also exists in edit mode).
        /// </remarks>
        /// <returns>Returns true if at least one object has been removed.</returns>
        public bool Clean()
        {
            bool hasChanged = false;
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                EObjectRefState state = _objects[i].GetState();
                if (state == EObjectRefState.Missing)
                {
                    _objects.RemoveAt(i);
                    if (i == _activeIndex)
                        _activeIndex = -1;
                    else if (i < _activeIndex)
                        _activeIndex--;

                    hasChanged = true;
                }
                else if (state == EObjectRefState.Resolved && !_objects[i].IsPersistent)
                {
                    _objects[i].Refresh();
                }
            }
            return hasChanged;
        }

        /// <summary>
        /// Captures again the identifiers of the objects that are still alive.
        /// </summary>
        /// <inheritdoc cref="ObjectRef.Refresh" path="/remarks"/>
        public void Refresh()
        {
            foreach (ObjectRef objRef in _objects)
                objRef.Refresh();
        }

        #endregion

    }

}
