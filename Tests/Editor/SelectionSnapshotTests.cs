using NUnit.Framework;

using UnityEngine;

using Object = UnityEngine.Object;
using UnitySelection = UnityEditor.Selection;

namespace SideXP.Selection.Tests
{

    /// <summary>
    /// Tests for <see cref="SelectionSnapshot"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="SelectionSnapshot.Apply"/> tests change the editor selection, which is restored on teardown. Note that these
    /// changes are recorded by <see cref="SelectionHistory"/> like any other selection change.
    /// </remarks>
    public class SelectionSnapshotTests : SceneTestsBase
    {

        private Object[] _previousSelection = null;

        [SetUp]
        public void StoreSelection()
        {
            _previousSelection = UnitySelection.objects;
        }

        [TearDown]
        public void RestoreSelection()
        {
            UnitySelection.objects = _previousSelection;
        }

        #region Construction

        [Test]
        public void Constructor_NullsAndDuplicates_AreIgnored()
        {
            GameObject a = CreateGameObject("A");
            GameObject b = CreateGameObject("B");

            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a, null, b, a });

            Assert.AreEqual(2, snapshot.Count);
            Assert.IsTrue(snapshot.Objects[0].Is(a));
            Assert.IsTrue(snapshot.Objects[1].Is(b));
        }

        [Test]
        public void Constructor_NullObjects_CreatesEmptySnapshot()
        {
            SelectionSnapshot snapshot = new SelectionSnapshot(null);

            Assert.IsTrue(snapshot.IsEmpty);
            Assert.IsNull(snapshot.Active);
        }

        [Test]
        public void Constructor_ActiveObjectInSelection_IsTracked()
        {
            GameObject a = CreateGameObject("A");
            GameObject b = CreateGameObject("B");

            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a, b }, b);

            Assert.IsNotNull(snapshot.Active);
            Assert.IsTrue(snapshot.Active.Is(b));
        }

        [Test]
        public void Constructor_ActiveObjectNotInSelection_IsIgnored()
        {
            GameObject a = CreateGameObject("A");
            GameObject other = CreateGameObject("Other");

            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a }, other);

            Assert.IsNull(snapshot.Active);
            Assert.AreEqual(1, snapshot.Count);
        }

        #endregion


        #region Resolution

        [Test]
        public void Resolve_DestroyedObject_IsSkipped()
        {
            GameObject a = CreateGameObject("A");
            GameObject b = CreateGameObject("B");
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a, b }, b);

            Object.DestroyImmediate(b);
            Object[] resolved = snapshot.Resolve(out Object activeObject);

            CollectionAssert.AreEqual(new Object[] { a }, resolved);
            Assert.IsNull(activeObject, "The active object has been destroyed, so it can't be resolved.");
        }

        [Test]
        public void CanResolve_SomeObjectsDestroyed_ReturnsTrue()
        {
            GameObject a = CreateGameObject("A");
            GameObject b = CreateGameObject("B");
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a, b });

            Object.DestroyImmediate(a);

            Assert.IsTrue(snapshot.CanResolve());
        }

        [Test]
        public void CanResolve_AllObjectsDestroyed_ReturnsFalse()
        {
            GameObject a = CreateGameObject("A");
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a });

            Object.DestroyImmediate(a);

            Assert.IsFalse(snapshot.CanResolve());
        }

        #endregion


        #region Comparison

        [Test]
        public void Matches_SameObjectsInDifferentOrder_ReturnsTrue()
        {
            GameObject a = CreateGameObject("A");
            GameObject b = CreateGameObject("B");

            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a, b });

            Assert.IsTrue(snapshot.Matches(new Object[] { b, a }));
        }

        [Test]
        public void Matches_DifferentObjects_ReturnsFalse()
        {
            GameObject a = CreateGameObject("A");
            GameObject b = CreateGameObject("B");

            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a, b });

            Assert.IsFalse(snapshot.Matches(new Object[] { a }));
            Assert.IsFalse(snapshot.Matches(new Object[] { a, b, CreateGameObject("C") }));
        }

        [Test]
        public void Matches_DestroyedObjects_AreIgnored()
        {
            GameObject a = CreateGameObject("A");
            GameObject b = CreateGameObject("B");
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a, b });

            Object.DestroyImmediate(b);

            Assert.IsTrue(snapshot.Matches(new Object[] { a }));
        }

        [Test]
        public void Matches_OtherSnapshot_ComparesResolvedObjects()
        {
            GameObject a = CreateGameObject("A");
            GameObject b = CreateGameObject("B");

            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a, b }, a);

            Assert.IsTrue(snapshot.Matches(new SelectionSnapshot(new Object[] { b, a }, b)), "The active object should not be compared.");
            Assert.IsFalse(snapshot.Matches(new SelectionSnapshot(new Object[] { a })));
            Assert.IsFalse(snapshot.Matches((SelectionSnapshot)null));
        }

        #endregion


        #region Application

        [Test]
        public void Apply_WithActiveObject_SelectsObjectsAndActiveObject()
        {
            GameObject a = CreateGameObject("A");
            GameObject b = CreateGameObject("B");
            GameObject c = CreateGameObject("C");
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a, b, c }, b);

            Assert.IsTrue(snapshot.Apply());

            CollectionAssert.AreEquivalent(new Object[] { a, b, c }, UnitySelection.objects);
            Assert.AreSame(b, UnitySelection.activeObject);
        }

        [Test]
        public void Apply_SomeObjectsDestroyed_SelectsRemainingObjects()
        {
            GameObject a = CreateGameObject("A");
            GameObject b = CreateGameObject("B");
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a, b });

            Object.DestroyImmediate(a);

            Assert.IsTrue(snapshot.Apply());
            CollectionAssert.AreEquivalent(new Object[] { b }, UnitySelection.objects);
        }

        [Test]
        public void Apply_AllObjectsDestroyed_ReturnsFalseAndKeepsSelection()
        {
            GameObject a = CreateGameObject("A");
            GameObject other = CreateGameObject("Other");
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a });
            UnitySelection.objects = new Object[] { other };

            Object.DestroyImmediate(a);

            Assert.IsFalse(snapshot.Apply());
            CollectionAssert.AreEqual(new Object[] { other }, UnitySelection.objects);
        }

        #endregion


        #region Cloning

        [Test]
        public void Clone_CopiesObjectsAndActiveObject()
        {
            GameObject a = CreateGameObject("A");
            GameObject b = CreateGameObject("B");
            SelectionSnapshot clone = new SelectionSnapshot(new Object[] { a, b }, b).Clone();

            Assert.AreEqual(2, clone.Count);
            Assert.IsTrue(clone.Matches(new Object[] { a, b }));
            Assert.IsTrue(clone.Active.Is(b));
        }

        [Test]
        public void Clone_CleanedClone_DoesNotChangeOriginal()
        {
            GameObject a = CreateGameObject("A");
            GameObject b = CreateGameObject("B");
            SelectionSnapshot original = new SelectionSnapshot(new Object[] { a, b }, b);
            SelectionSnapshot clone = original.Clone();

            Object.DestroyImmediate(b);
            clone.Clean();

            Assert.AreEqual(1, clone.Count);
            Assert.AreEqual(2, original.Count, "The objects list of the original should not be shared with the clone.");
            Assert.IsNotNull(original.Active, "The active object of the original should not be affected by cleaning the clone.");
        }

        #endregion


        #region Cleaning

        [Test]
        public void Clean_DestroyedObjects_AreRemoved()
        {
            GameObject a = CreateGameObject("A");
            GameObject b = CreateGameObject("B");
            GameObject c = CreateGameObject("C");
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a, b, c });

            Object.DestroyImmediate(b);

            Assert.IsTrue(snapshot.Clean());
            Assert.AreEqual(2, snapshot.Count);
            Assert.IsTrue(snapshot.Objects[0].Is(a));
            Assert.IsTrue(snapshot.Objects[1].Is(c));
        }

        [Test]
        public void Clean_NothingMissing_ReturnsFalse()
        {
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { CreateGameObject("A"), CreateGameObject("B") });

            Assert.IsFalse(snapshot.Clean());
            Assert.AreEqual(2, snapshot.Count);
        }

        [Test]
        public void Clean_ActiveObjectDestroyed_ClearsActiveObject()
        {
            GameObject a = CreateGameObject("A");
            GameObject b = CreateGameObject("B");
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a, b }, b);

            Object.DestroyImmediate(b);
            snapshot.Clean();

            Assert.IsNull(snapshot.Active);
        }

        [Test]
        public void Clean_ObjectBeforeActiveObjectDestroyed_KeepsActiveObject()
        {
            GameObject a = CreateGameObject("A");
            GameObject b = CreateGameObject("B");
            GameObject c = CreateGameObject("C");
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a, b, c }, c);

            Object.DestroyImmediate(a);
            snapshot.Clean();

            Assert.IsNotNull(snapshot.Active);
            Assert.IsTrue(snapshot.Active.Is(c));
        }

        [Test]
        public void Clean_AllObjectsDestroyed_LeavesEmptySnapshot()
        {
            GameObject a = CreateGameObject("A");
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a }, a);

            Object.DestroyImmediate(a);

            Assert.IsTrue(snapshot.Clean());
            Assert.IsTrue(snapshot.IsEmpty);
            Assert.IsNull(snapshot.Active);
        }

        #endregion

    }

}
