using System;

using NUnit.Framework;

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

using Object = UnityEngine.Object;

namespace SideXP.Selection.Tests
{

    /// <summary>
    /// Tests for <see cref="ObjectRef"/>.
    /// </summary>
    /// <remarks>
    /// The loss of the original instance is simulated with <see cref="ObjectRef.ClearCache"/>, or by destroying the object. Edit/play
    /// mode switches and objects saved in a scene asset are not covered here.
    /// </remarks>
    public class ObjectRefTests : SceneTestsBase
    {

        private const string TempAssetPath = "Assets/__SideXP_Selection_ObjectRefTests__.anim";

        [TearDown]
        public void DeleteTempAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(TempAssetPath) != null)
                AssetDatabase.DeleteAsset(TempAssetPath);
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
