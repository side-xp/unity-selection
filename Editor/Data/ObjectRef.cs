using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Selection
{

    /// <summary>
    /// Reference to an object that can be resolved again after its original instance has been lost (after a domain reload, a scene
    /// reload, a switch between edit and play mode or an editor restart).
    /// </summary>
    /// <remarks>
    /// The referenced object is resolved using, in order:<br/>
    /// - the cached reference, as long as the original instance is still alive<br/>
    /// - its <see cref="GlobalObjectId"/>, for assets and objects saved in a scene (edit mode only)<br/>
    /// - its hierarchy path, for scene objects (including objects not saved yet, or spawned in play mode)
    /// </remarks>
    [Serializable]
    public class ObjectRef
    {

        #region Subclasses

        /// <summary>
        /// Identifies an element of a hierarchy path by its name and its position among the siblings that have the same name.
        /// </summary>
        [Serializable]
        private struct PathNode
        {

            /// <summary>
            /// The name of the element.
            /// </summary>
            public string Name;

            /// <summary>
            /// The index of the element among its siblings that have the same name, so duplicate names can still be told apart.
            /// </summary>
            public int Occurrence;

            /// <summary>
            /// Creates a hierarchy path element.
            /// </summary>
            /// <param name="name">The name of the element.</param>
            /// <param name="occurrence">The index of the element among its siblings that have the same name.</param>
            public PathNode(string name, int occurrence)
            {
                Name = name;
                Occurrence = occurrence;
            }

        }

        #endregion


        #region Fields

        /// <summary>
        /// The value of <see cref="GlobalObjectId.identifierType"/> for an invalid id.
        /// </summary>
        private const int NullIdentifierType = 0;

        private const string HierarchyPathSeparator = "/";

        /// <summary>
        /// The cached reference to the object. It's used as long as the original instance is alive.
        /// </summary>
        [SerializeField]
        private Object _object = null;

        /// <summary>
        /// The <see cref="GlobalObjectId"/> of the object, as string. Empty if the object doesn't have a stable id (objects not saved in a
        /// scene yet, or captured while in play mode).
        /// </summary>
        [SerializeField]
        private string _globalId = null;

        /// <summary>
        /// The display name of the object when it has been captured.
        /// </summary>
        [SerializeField]
        private string _name = null;

        /// <summary>
        /// The assembly qualified name of the object's type.
        /// </summary>
        [SerializeField]
        private string _typeName = null;

        /// <summary>
        /// The GUID of the scene asset that contains the object. Empty if the object is not a scene object, or if the scene is not saved.
        /// Used to find the scene even if it has been moved or renamed.
        /// </summary>
        [SerializeField]
        private string _sceneGuid = null;

        /// <summary>
        /// The path of the scene that contains the object when it has been captured. Empty for objects in an untitled scene.
        /// </summary>
        [SerializeField]
        private string _scenePath = null;

        /// <summary>
        /// The name of the scene that contains the object when it has been captured.
        /// </summary>
        [SerializeField]
        private string _sceneName = null;

        /// <summary>
        /// Indicates if the object is in a scene.
        /// </summary>
        [SerializeField]
        private bool _isSceneObject = false;

        /// <summary>
        /// The hierarchy path to the object's GameObject, from the root of its scene. Empty if the object is not a scene object.
        /// </summary>
        [SerializeField]
        private List<PathNode> _hierarchyPath = new List<PathNode>();

        /// <summary>
        /// If the referenced object is a component, defines its type and its index among the components of the same type on the
        /// GameObject. The name is empty if the object is not a component.
        /// </summary>
        [SerializeField]
        private PathNode _component = default;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Creates a reference to the given object.
        /// </summary>
        /// <param name="obj">The object to reference.</param>
        /// <exception cref="ArgumentNullException">Thrown if the given object is null.</exception>
        public ObjectRef(Object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            Capture(obj);
        }

        #endregion


        #region Public API

        /// <summary>
        /// The display name of the object when it has been captured.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// The type of the object, or null if that type doesn't exist anymore.
        /// </summary>
        public Type ObjectType => Type.GetType(_typeName, false);

        /// <inheritdoc cref="_isSceneObject"/>
        public bool IsSceneObject => _isSceneObject;

        /// <summary>
        /// Checks if this reference uses a stable id, so it can be resolved even after the editor has been restarted. Assets and objects
        /// saved in a scene have a stable id, while objects not saved yet or captured while in play mode don't.
        /// </summary>
        public bool IsPersistent => !string.IsNullOrEmpty(_globalId);

        /// <summary>
        /// The current path of the scene that contains the object. Empty for objects in an untitled scene or for non-scene objects.
        /// </summary>
        public string ScenePath
        {
            get
            {
                if (!string.IsNullOrEmpty(_sceneGuid))
                {
                    string path = AssetDatabase.GUIDToAssetPath(_sceneGuid);
                    if (!string.IsNullOrEmpty(path))
                        return path;
                }
                return _scenePath;
            }
        }

        /// <inheritdoc cref="_sceneName"/>
        public string SceneName => _sceneName;

        /// <summary>
        /// The hierarchy path to the object from the root of its scene, as a readable string. Empty if the object is not a scene object.
        /// </summary>
        public string HierarchyPath
        {
            get
            {
                if (!_isSceneObject)
                    return string.Empty;

                StringBuilder builder = new StringBuilder();
                foreach (PathNode node in _hierarchyPath)
                {
                    if (builder.Length > 0)
                        builder.Append(HierarchyPathSeparator);
                    builder.Append(node.Name);
                }

                if (!string.IsNullOrEmpty(_component.Name))
                {
                    Type componentType = Type.GetType(_component.Name, false);
                    builder.Append($" ({(componentType != null ? componentType.Name : _component.Name)})");
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// Gets the referenced object, and caches it if it has been found.
        /// </summary>
        /// <returns>Returns the found object, or null if it can't be resolved.</returns>
        public Object Resolve()
        {
            if (_object != null)
                return _object;

            Object obj = null;

            // In play mode, scene objects are resolved by hierarchy path only, since their stable ids target their edit mode version
            if (!string.IsNullOrEmpty(_globalId) && !(_isSceneObject && EditorApplication.isPlaying))
            {
                if (GlobalObjectId.TryParse(_globalId, out GlobalObjectId id))
                    obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
            }

            if (obj == null && _isSceneObject)
                obj = ResolveFromHierarchy();

            if (obj != null)
                _object = obj;

            return obj;
        }

        /// <summary>
        /// Tries to resolve the referenced object, and gets the state of this reference.
        /// </summary>
        /// <param name="obj">Outputs the found object, or null if it can't be resolved.</param>
        /// <returns>Returns the state of this reference.</returns>
        public EObjectRefState GetState(out Object obj)
        {
            obj = Resolve();
            if (obj != null)
                return EObjectRefState.Resolved;

            // A persistent scene object can't be resolved if its scene is not loaded, but it may be resolved again later
            if (_isSceneObject && IsPersistent && !FindLoadedScene().IsValid())
                return EObjectRefState.Unloaded;

            return EObjectRefState.Missing;
        }

        /// <inheritdoc cref="GetState(out Object)"/>
        public EObjectRefState GetState()
        {
            return GetState(out _);
        }

        /// <summary>
        /// Checks if this reference targets the given object.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>Returns true if the referenced object is the given one.</returns>
        public bool Is(Object obj)
        {
            return obj != null && Resolve() == obj;
        }

        /// <summary>
        /// Captures again the identifiers of the referenced object, if it's still alive. This is meant to be called after a scene has
        /// been saved (so objects not saved before get a stable id), or after an object has been renamed or moved in the hierarchy.
        /// </summary>
        /// <remarks>Does nothing in play mode, so the stable ids captured in edit mode are not lost.</remarks>
        /// <returns>Returns true if the identifiers have been captured again.</returns>
        public bool Refresh()
        {
            if (EditorApplication.isPlaying)
                return false;

            Object obj = Resolve();
            if (obj == null)
                return false;

            Capture(obj);
            return true;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return _isSceneObject
                ? $"{_name} ({_sceneName}: {HierarchyPath})"
                : _name;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Captures the identifiers of the given object.
        /// </summary>
        /// <param name="obj">The object to capture.</param>
        private void Capture(Object obj)
        {
            _object = obj;
            _name = obj.name;
            _typeName = obj.GetType().AssemblyQualifiedName;

            _globalId = null;
            _sceneGuid = null;
            _scenePath = null;
            _sceneName = null;
            _isSceneObject = false;
            _hierarchyPath.Clear();
            _component = default;

            GameObject gameObject = GetGameObject(obj);
            _isSceneObject = gameObject != null && gameObject.scene.IsValid();

            // Ids generated in play mode for scene objects are not reliable once back to edit mode
            if (!_isSceneObject || !EditorApplication.isPlaying)
            {
                GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                if (IsStableId(id))
                    _globalId = id.ToString();
            }

            if (!_isSceneObject)
                return;

            Scene scene = gameObject.scene;
            _scenePath = scene.path;
            _sceneName = scene.name;
            if (!string.IsNullOrEmpty(scene.path))
                _sceneGuid = AssetDatabase.AssetPathToGUID(scene.path);

            for (Transform current = gameObject.transform; current != null; current = current.parent)
                _hierarchyPath.Insert(0, new PathNode(current.name, GetOccurrence(current)));

            if (obj is Component component)
                _component = new PathNode(component.GetType().AssemblyQualifiedName, GetOccurrence(component));
        }

        /// <summary>
        /// Finds the referenced object in the loaded scenes using its hierarchy path.
        /// </summary>
        /// <returns>Returns the found object, or null if it can't be found.</returns>
        private Object ResolveFromHierarchy()
        {
            if (_hierarchyPath.Count <= 0)
                return null;

            Scene scene = FindLoadedScene();
            if (!scene.IsValid())
                return null;

            Transform current = FindNode(GetRootTransforms(scene), _hierarchyPath[0]);
            for (int i = 1; i < _hierarchyPath.Count && current != null; i++)
                current = FindNode(GetChildren(current), _hierarchyPath[i]);

            if (current == null)
                return null;

            if (string.IsNullOrEmpty(_component.Name))
                return current.gameObject;

            Type componentType = Type.GetType(_component.Name, false);
            if (componentType == null)
                return null;

            int occurrence = 0;
            foreach (Component component in current.GetComponents<Component>())
            {
                // Skip missing scripts and components of derived types
                if (component == null || component.GetType() != componentType)
                    continue;

                if (occurrence == _component.Occurrence)
                    return component;

                occurrence++;
            }

            return null;
        }

        /// <summary>
        /// Finds the loaded scene that contains the referenced object.
        /// </summary>
        /// <returns>Returns the found scene, or an invalid scene if it's not loaded.</returns>
        private Scene FindLoadedScene()
        {
            if (!_isSceneObject)
                return default;

            string scenePath = ScenePath ?? string.Empty;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.path == scenePath)
                    return scene;
            }

            return default;
        }

        /// <summary>
        /// Gets the GameObject of the given object, if applicable.
        /// </summary>
        /// <param name="obj">The object of which you want to get the GameObject.</param>
        /// <returns>Returns the object itself if it's a GameObject, the GameObject it's attached to if it's a component, or null.</returns>
        private static GameObject GetGameObject(Object obj)
        {
            if (obj is GameObject gameObject)
                return gameObject;

            if (obj is Component component)
                return component.gameObject;

            return null;
        }

        /// <summary>
        /// Checks if the given id can be used to resolve the object again after the editor has been restarted.
        /// </summary>
        /// <param name="id">The id to check.</param>
        /// <returns>Returns true if the id is stable.</returns>
        private static bool IsStableId(GlobalObjectId id)
        {
            // Objects not saved yet have no file id, and objects in untitled scenes have no asset GUID
            return id.identifierType != NullIdentifierType && !id.assetGUID.Empty() && id.targetObjectId != 0;
        }

        /// <summary>
        /// Gets the index of the given transform among its siblings that have the same name.
        /// </summary>
        /// <param name="transform">The transform to check.</param>
        /// <returns>Returns the computed index.</returns>
        private static int GetOccurrence(Transform transform)
        {
            IEnumerable<Transform> siblings = transform.parent != null
                ? GetChildren(transform.parent)
                : GetRootTransforms(transform.gameObject.scene);

            int occurrence = 0;
            foreach (Transform sibling in siblings)
            {
                if (sibling == transform)
                    break;

                if (sibling.name == transform.name)
                    occurrence++;
            }
            return occurrence;
        }

        /// <summary>
        /// Gets the index of the given component among the components of the same type on its GameObject.
        /// </summary>
        /// <param name="component">The component to check.</param>
        /// <returns>Returns the computed index.</returns>
        private static int GetOccurrence(Component component)
        {
            Type componentType = component.GetType();

            int occurrence = 0;
            foreach (Component other in component.GetComponents<Component>())
            {
                if (other == component)
                    break;

                if (other != null && other.GetType() == componentType)
                    occurrence++;
            }
            return occurrence;
        }

        /// <summary>
        /// Finds the element matching the given hierarchy path element among the given candidates.
        /// </summary>
        /// <param name="candidates">The transforms that may match the path element.</param>
        /// <param name="node">The path element to find.</param>
        /// <returns>Returns the found transform, or null if there's no match.</returns>
        private static Transform FindNode(IEnumerable<Transform> candidates, PathNode node)
        {
            int occurrence = 0;
            foreach (Transform candidate in candidates)
            {
                if (candidate.name != node.Name)
                    continue;

                if (occurrence == node.Occurrence)
                    return candidate;

                occurrence++;
            }
            return null;
        }

        /// <summary>
        /// Enumerates the transforms of the root GameObjects of a given scene.
        /// </summary>
        /// <param name="scene">The scene to explore.</param>
        /// <returns>Returns the transforms of the root GameObjects, in hierarchy order.</returns>
        private static IEnumerable<Transform> GetRootTransforms(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                yield return root.transform;
        }

        /// <summary>
        /// Enumerates the direct children of a given transform.
        /// </summary>
        /// <param name="parent">The transform to explore.</param>
        /// <returns>Returns the children transforms, in hierarchy order.</returns>
        private static IEnumerable<Transform> GetChildren(Transform parent)
        {
            foreach (Transform child in parent)
                yield return child;
        }

        #endregion

    }

}
