using System;
using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

using Object = UnityEngine.Object;

namespace SideXP.Selection.Tests
{

    /// <summary>
    /// Tests for <see cref="ObjectRef"/>.
    /// </summary>
    /// <remarks>
    /// For edit mode runs, the Test Runner stores the scene setup, opens a new untitled scene and restores the original setup once the
    /// run is finished. The tests use that untitled scene, and destroy the objects they create on teardown. The loss of the original
    /// instance is simulated with <see cref="ObjectRef.ClearCache"/>, or by destroying the object. Edit/play mode switches and objects
    /// saved in a scene asset are not covered here.
    /// </remarks>
    public class ObjectRefTests
    {

        private const string TempAssetPath = "Assets/__SideXP_Selection_ObjectRefTests__.anim";

        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            // Never create test objects in a user scene, in case these tests are run outside of the Test Runner's own scene
            if (!string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                Assert.Ignore("These tests are meant to run in the untitled scene opened by the Test Runner.");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in _createdObjects)
            {
                if (gameObject != null)
                    Object.DestroyImmediate(gameObject);
            }
            _createdObjects.Clear();

            if (AssetDatabase.LoadAssetAtPath<Object>(TempAssetPath) != null)
                AssetDatabase.DeleteAsset(TempAssetPath);
        }

        /// <summary>
        /// Creates a GameObject in the active scene, destroyed on teardown.
        /// </summary>
        /// <param name="name">The name of the GameObject.</param>
        /// <param name="parent">The parent of the GameObject. If null, the GameObject is created at the root of the active scene.</param>
        /// <returns>Returns the created GameObject.</returns>
        private GameObject CreateGameObject(string name, GameObject parent = null)
        {
            GameObject gameObject = new GameObject(name);
            if (parent != null)
                gameObject.transform.SetParent(parent.transform, false);

            _createdObjects.Add(gameObject);
            return gameObject;
        }

        /// <summary>
        /// Creates a temporary asset on disk, deleted on teardown.
        /// </summary>
        /// <returns>Returns the created asset.</returns>
        private static AnimationClip CreateTempAsset()
        {
            AnimationClip clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, TempAssetPath);
            return clip;
        }

        #region Construction

        [Test]
        public void Constructor_NullObject_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ObjectRef(null));
        }

        [Test]
        public void Constructor_SceneObject_CapturesSceneInfo()
        {
            GameObject root = CreateGameObject("Root");
            ObjectRef objRef = new ObjectRef(root);

            Assert.IsTrue(objRef.IsSceneObject);
            Assert.IsFalse(objRef.IsPersistent, "An object in an untitled scene can't have a stable id.");
            Assert.AreEqual(string.Empty, objRef.ScenePath ?? string.Empty);
            Assert.AreEqual("Root", objRef.Name);
            Assert.AreEqual(typeof(GameObject), objRef.ObjectType);
        }

        [Test]
        public void Constructor_Asset_CapturesStableId()
        {
            ObjectRef objRef = new ObjectRef(CreateTempAsset());

            Assert.IsFalse(objRef.IsSceneObject);
            Assert.IsTrue(objRef.IsPersistent);
            Assert.AreEqual(string.Empty, objRef.HierarchyPath);
        }

        #endregion


        #region Hierarchy path resolution

        [Test]
        public void Resolve_AliveObject_ReturnsSameObject()
        {
            GameObject root = CreateGameObject("Root");

            Assert.AreSame(root, new ObjectRef(root).Resolve());
        }

        [Test]
        public void Resolve_NestedObjectCacheCleared_FindsSameObject()
        {
            GameObject child = CreateGameObject("C", CreateGameObject("B", CreateGameObject("A")));
            ObjectRef objRef = new ObjectRef(child);

            objRef.ClearCache();

            Assert.AreSame(child, objRef.Resolve());
        }

        [Test]
        public void Resolve_ObjectRecreatedAtSamePath_FindsNewObject()
        {
            GameObject parent = CreateGameObject("Parent");
            GameObject child = CreateGameObject("Child", parent);
            ObjectRef objRef = new ObjectRef(child);

            Object.DestroyImmediate(child);
            GameObject newChild = CreateGameObject("Child", parent);

            Assert.AreSame(newChild, objRef.Resolve());
        }

        [Test]
        public void Resolve_DuplicateSiblingNames_FindsRightOccurrence()
        {
            GameObject parent = CreateGameObject("Parent");
            CreateGameObject("Item", parent);
            CreateGameObject("Other", parent);
            GameObject secondItem = CreateGameObject("Item", parent);
            CreateGameObject("Item", parent);
            ObjectRef objRef = new ObjectRef(secondItem);

            objRef.ClearCache();

            Assert.AreSame(secondItem, objRef.Resolve());
        }

        [Test]
        public void Resolve_DuplicateRootNames_FindsRightOccurrence()
        {
            CreateGameObject("Root");
            GameObject secondRoot = CreateGameObject("Root");
            ObjectRef objRef = new ObjectRef(secondRoot);

            objRef.ClearCache();

            Assert.AreSame(secondRoot, objRef.Resolve());
        }

        [Test]
        public void Resolve_ComponentWithSameTypeSiblings_FindsRightOccurrence()
        {
            GameObject gameObject = CreateGameObject("Root");
            // The sphere collider shares a base type with the box colliders, but must not be counted as one of them
            gameObject.AddComponent<SphereCollider>();
            gameObject.AddComponent<BoxCollider>();
            BoxCollider secondBox = gameObject.AddComponent<BoxCollider>();
            ObjectRef objRef = new ObjectRef(secondBox);

            objRef.ClearCache();

            Assert.AreSame(secondBox, objRef.Resolve());
        }

        [Test]
        public void Resolve_RenamedObject_ReturnsNull()
        {
            GameObject root = CreateGameObject("Root");
            ObjectRef objRef = new ObjectRef(root);

            root.name = "Renamed";
            objRef.ClearCache();

            Assert.IsNull(objRef.Resolve());
        }

        [Test]
        public void Refresh_RenamedObject_ResolvesWithNewPath()
        {
            GameObject root = CreateGameObject("Root");
            ObjectRef objRef = new ObjectRef(root);

            root.name = "Renamed";
            Assert.IsTrue(objRef.Refresh());
            objRef.ClearCache();

            Assert.AreSame(root, objRef.Resolve());
            Assert.AreEqual("Renamed", objRef.Name);
        }

        [Test]
        public void Refresh_LostObject_ReturnsFalse()
        {
            GameObject root = CreateGameObject("Root");
            ObjectRef objRef = new ObjectRef(root);

            Object.DestroyImmediate(root);

            Assert.IsFalse(objRef.Refresh());
        }

        #endregion


        #region Asset resolution

        [Test]
        public void Resolve_AssetCacheCleared_FindsSameAssetByGlobalId()
        {
            AnimationClip clip = CreateTempAsset();
            ObjectRef objRef = new ObjectRef(clip);

            objRef.ClearCache();

            Assert.AreSame(clip, objRef.Resolve());
        }

        #endregion


        #region States

        [Test]
        public void GetState_AliveObject_ReturnsResolved()
        {
            GameObject root = CreateGameObject("Root");

            Assert.AreEqual(EObjectRefState.Resolved, new ObjectRef(root).GetState(out Object obj));
            Assert.AreSame(root, obj);
        }

        [Test]
        public void GetState_DestroyedObject_ReturnsMissing()
        {
            GameObject root = CreateGameObject("Root");
            ObjectRef objRef = new ObjectRef(root);

            Object.DestroyImmediate(root);

            Assert.AreEqual(EObjectRefState.Missing, objRef.GetState(out Object obj));
            Assert.IsNull(obj);
        }

        [Test]
        public void GetState_UnsavedObjectSceneReplaced_ReturnsMissing()
        {
            ObjectRef objRef = new ObjectRef(CreateGameObject("Root"));

            // Simulates a scene reloaded without saving: the new untitled scene has the same (empty) path, but not the object
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Assert.AreEqual(EObjectRefState.Missing, objRef.GetState());
        }

        [Test]
        public void GetState_DeletedAsset_ReturnsMissing()
        {
            ObjectRef objRef = new ObjectRef(CreateTempAsset());

            AssetDatabase.DeleteAsset(TempAssetPath);

            Assert.AreEqual(EObjectRefState.Missing, objRef.GetState());
        }

        #endregion


        #region Display

        [Test]
        public void HierarchyPath_NestedObject_JoinsNames()
        {
            GameObject child = CreateGameObject("Child", CreateGameObject("Root"));

            Assert.AreEqual("Root/Child", new ObjectRef(child).HierarchyPath);
        }

        [Test]
        public void HierarchyPath_Component_AppendsTypeName()
        {
            BoxCollider collider = CreateGameObject("Child", CreateGameObject("Root")).AddComponent<BoxCollider>();

            Assert.AreEqual("Root/Child (BoxCollider)", new ObjectRef(collider).HierarchyPath);
        }

        [Test]
        public void Is_SameAndOtherObjects_ComparesResolvedObject()
        {
            GameObject root = CreateGameObject("Root");
            GameObject other = CreateGameObject("Other");
            ObjectRef objRef = new ObjectRef(root);

            Assert.IsTrue(objRef.Is(root));
            Assert.IsFalse(objRef.Is(other));
            Assert.IsFalse(objRef.Is(null));
        }

        #endregion

    }

}
