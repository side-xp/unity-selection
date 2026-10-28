using NUnit.Framework;

using UnityEngine;
using UnityEditor.SceneManagement;

using Object = UnityEngine.Object;

namespace SideXP.Selection.Tests
{

    /// <summary>
    /// Tests for <see cref="ObjectRef.RefersToSameObject(ObjectRef)"/> and
    /// <see cref="SelectionSnapshot.HasSameObjects(SelectionSnapshot)"/>.
    /// </summary>
    /// <remarks>
    /// Objects that can't be resolved are simulated by replacing the Test Runner's untitled scene with a new one: the references keep
    /// their identifiers, but the objects don't exist anymore.
    /// </remarks>
    public class SelectionComparisonTests : SceneTestsBase
    {

        /// <summary>
        /// Replaces the active scene with a new untitled scene, so the objects of the previous one can't be resolved anymore.
        /// </summary>
        private static void ReplaceScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        #region Object references

        [Test]
        public void RefersToSameObject_ResolvedObjects_ComparesObjects()
        {
            GameObject a = CreateGameObject("A");
            ObjectRef objRef = new ObjectRef(a);

            Assert.IsTrue(objRef.RefersToSameObject(new ObjectRef(a)));
            Assert.IsFalse(objRef.RefersToSameObject(new ObjectRef(CreateGameObject("B"))));
            Assert.IsFalse(objRef.RefersToSameObject(null));
        }

        [Test]
        public void RefersToSameObject_UnresolvedObjectsWithSamePath_ReturnsTrue()
        {
            GameObject child = CreateGameObject("Child", CreateGameObject("Root"));
            ObjectRef objRef = new ObjectRef(child);
            ObjectRef otherRef = new ObjectRef(child);

            ReplaceScene();

            Assert.IsTrue(objRef.RefersToSameObject(otherRef));
        }

        [Test]
        public void RefersToSameObject_UnresolvedObjectsWithDifferentPaths_ReturnsFalse()
        {
            ObjectRef objRef = new ObjectRef(CreateGameObject("A"));
            ObjectRef otherRef = new ObjectRef(CreateGameObject("B"));

            ReplaceScene();

            Assert.IsFalse(objRef.RefersToSameObject(otherRef));
        }

        [Test]
        public void RefersToSameObject_SameNameDifferentOccurrence_ReturnsFalse()
        {
            ObjectRef objRef = new ObjectRef(CreateGameObject("Item"));
            ObjectRef otherRef = new ObjectRef(CreateGameObject("Item"));

            ReplaceScene();

            Assert.IsFalse(objRef.RefersToSameObject(otherRef));
        }

        #endregion


        #region Snapshots

        [Test]
        public void HasSameObjects_SameObjectsInDifferentOrder_ReturnsTrue()
        {
            GameObject a = CreateGameObject("A");
            GameObject b = CreateGameObject("B");
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a, b }, a);

            Assert.IsTrue(snapshot.HasSameObjects(new SelectionSnapshot(new Object[] { b, a }, b)));
        }

        [Test]
        public void HasSameObjects_DifferentObjects_ReturnsFalse()
        {
            GameObject a = CreateGameObject("A");
            GameObject b = CreateGameObject("B");
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a, b });

            Assert.IsFalse(snapshot.HasSameObjects(new SelectionSnapshot(new Object[] { a })));
            Assert.IsFalse(snapshot.HasSameObjects(new SelectionSnapshot(new Object[] { a, CreateGameObject("C") })));
            Assert.IsFalse(snapshot.HasSameObjects(null));
        }

        [Test]
        public void HasSameObjects_UnresolvedSameObjects_ReturnsTrue()
        {
            GameObject a = CreateGameObject("A");
            GameObject b = CreateGameObject("B");
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a, b });
            SelectionSnapshot other = new SelectionSnapshot(new Object[] { b, a });

            ReplaceScene();

            Assert.IsTrue(snapshot.HasSameObjects(other));
        }

        [Test]
        public void HasSameObjects_UnresolvedDifferentObjects_ReturnsFalse()
        {
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { CreateGameObject("A") });
            SelectionSnapshot other = new SelectionSnapshot(new Object[] { CreateGameObject("B") });

            ReplaceScene();

            // Both snapshots resolve to no object, which is why Matches() can't be used to compare saved selections
            Assert.IsTrue(snapshot.Matches(other));
            Assert.IsFalse(snapshot.HasSameObjects(other));
        }

        [Test]
        public void HasSameObjects_Clone_ReturnsTrue()
        {
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { CreateGameObject("A"), CreateGameObject("B") });

            Assert.IsTrue(snapshot.HasSameObjects(snapshot.Clone()));
        }

        #endregion

    }

}
