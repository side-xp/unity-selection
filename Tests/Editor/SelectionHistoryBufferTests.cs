using System;

using NUnit.Framework;

using UnityEngine;
using UnityEditor.SceneManagement;

using Object = UnityEngine.Object;

namespace SideXP.Selection.Tests
{

    /// <summary>
    /// Tests for <see cref="SelectionHistoryBuffer"/>.
    /// </summary>
    public class SelectionHistoryBufferTests : SceneTestsBase
    {

        /// <summary>
        /// Creates a snapshot of a new GameObject.
        /// </summary>
        /// <param name="name">The name of the GameObject.</param>
        /// <returns>Returns the created snapshot.</returns>
        private SelectionSnapshot CreateSnapshot(string name)
        {
            return new SelectionSnapshot(new Object[] { CreateGameObject(name) });
        }

        /// <summary>
        /// Creates a history containing a snapshot of a new GameObject for each given name, in order.
        /// </summary>
        /// <param name="names">The names of the GameObjects.</param>
        /// <returns>Returns the created history, with the last entry as the current one.</returns>
        private SelectionHistoryBuffer CreateHistory(params string[] names)
        {
            SelectionHistoryBuffer history = new SelectionHistoryBuffer();
            foreach (string name in names)
                history.Push(CreateSnapshot(name));
            return history;
        }

        #region Push

        [Test]
        public void Push_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SelectionHistoryBuffer().Push(null));
        }

        [Test]
        public void Push_FirstSnapshot_BecomesCurrent()
        {
            SelectionHistoryBuffer history = new SelectionHistoryBuffer();
            SelectionSnapshot snapshot = CreateSnapshot("A");

            Assert.IsTrue(history.Push(snapshot));
            Assert.AreEqual(1, history.Count);
            Assert.AreEqual(0, history.CurrentIndex);
            Assert.AreSame(snapshot, history.Current);
        }

        [Test]
        public void Push_SnapshotMatchingCurrent_IsIgnored()
        {
            GameObject a = CreateGameObject("A");
            SelectionHistoryBuffer history = new SelectionHistoryBuffer();
            history.Push(new SelectionSnapshot(new Object[] { a }));

            Assert.IsFalse(history.Push(new SelectionSnapshot(new Object[] { a })));
            Assert.AreEqual(1, history.Count);
        }

        [Test]
        public void Push_AfterGoingBack_DiscardsForwardEntries()
        {
            SelectionHistoryBuffer history = CreateHistory("A", "B", "C", "D");
            history.SetCurrentIndex(1);

            SelectionSnapshot snapshot = CreateSnapshot("E");
            history.Push(snapshot);

            Assert.AreEqual(3, history.Count);
            Assert.AreEqual(2, history.CurrentIndex);
            Assert.AreSame(snapshot, history.Current);
        }

        [Test]
        public void Push_OverMaxCount_DiscardsOldestEntries()
        {
            SelectionHistoryBuffer history = new SelectionHistoryBuffer();
            history.MaxCount = 2;
            SelectionSnapshot b = CreateSnapshot("B");
            SelectionSnapshot c = CreateSnapshot("C");

            history.Push(CreateSnapshot("A"));
            history.Push(b);
            history.Push(c);

            Assert.AreEqual(2, history.Count);
            Assert.AreSame(b, history.Entries[0]);
            Assert.AreSame(c, history.Current);
        }

        #endregion


        #region Max count

        [Test]
        public void MaxCount_Reduced_DiscardsOldestEntriesAndKeepsCurrent()
        {
            SelectionHistoryBuffer history = CreateHistory("A", "B", "C", "D");
            SelectionSnapshot current = history.Entries[2];
            history.SetCurrentIndex(2);

            history.MaxCount = 3;

            Assert.AreEqual(3, history.Count);
            Assert.AreEqual(1, history.CurrentIndex);
            Assert.AreSame(current, history.Current);
        }

        [Test]
        public void MaxCount_BelowOne_IsClampedToOne()
        {
            SelectionHistoryBuffer history = CreateHistory("A", "B");

            history.MaxCount = 0;

            Assert.AreEqual(1, history.MaxCount);
            Assert.AreEqual(1, history.Count);
            Assert.AreEqual(0, history.CurrentIndex);
        }

        #endregion


        #region Navigation

        [Test]
        public void FindTarget_Back_ReturnsPreviousEntries()
        {
            SelectionHistoryBuffer history = CreateHistory("A", "B", "C");

            Assert.AreEqual(1, history.FindTarget(-1));
            Assert.AreEqual(0, history.FindTarget(-2));
        }

        [Test]
        public void FindTarget_Forward_ReturnsNextEntries()
        {
            SelectionHistoryBuffer history = CreateHistory("A", "B", "C");
            history.SetCurrentIndex(0);

            Assert.AreEqual(1, history.FindTarget(1));
            Assert.AreEqual(2, history.FindTarget(2));
        }

        [Test]
        public void FindTarget_UnresolvableEntries_AreSkipped()
        {
            GameObject b = CreateGameObject("B");
            SelectionHistoryBuffer history = new SelectionHistoryBuffer();
            history.Push(CreateSnapshot("A"));
            history.Push(new SelectionSnapshot(new Object[] { b }));
            history.Push(CreateSnapshot("C"));

            Object.DestroyImmediate(b);

            Assert.AreEqual(0, history.FindTarget(-1));
        }

        [Test]
        public void FindTarget_MoreStepsThanEntries_ReturnsFurthestValidEntry()
        {
            SelectionHistoryBuffer history = CreateHistory("A", "B", "C");

            Assert.AreEqual(0, history.FindTarget(-10));
        }

        [Test]
        public void FindTarget_NoValidEntryInDirection_ReturnsMinusOne()
        {
            SelectionHistoryBuffer history = CreateHistory("A", "B");

            Assert.AreEqual(-1, history.FindTarget(1), "The current entry is the most recent one.");
            Assert.AreEqual(-1, new SelectionHistoryBuffer().FindTarget(-1), "An empty history has no entry to go back to.");
        }

        [Test]
        public void FindTarget_ZeroSteps_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, CreateHistory("A", "B").FindTarget(0));
        }

        [Test]
        public void SetCurrentIndex_OutOfRange_ReturnsFalse()
        {
            SelectionHistoryBuffer history = CreateHistory("A", "B");

            Assert.IsFalse(history.SetCurrentIndex(-1));
            Assert.IsFalse(history.SetCurrentIndex(2));
            Assert.AreEqual(1, history.CurrentIndex);
        }

        [Test]
        public void Clear_FilledHistory_RemovesAllEntries()
        {
            SelectionHistoryBuffer history = CreateHistory("A", "B");

            history.Clear();

            Assert.AreEqual(0, history.Count);
            Assert.AreEqual(-1, history.CurrentIndex);
            Assert.IsNull(history.Current);
        }

        #endregion


        #region Cleaning

        /// <summary>
        /// Destroys the object referenced by the first object of the given entry.
        /// </summary>
        /// <param name="entry">The entry of which you want to destroy the first object.</param>
        private static void DestroyFirstObject(SelectionSnapshot entry)
        {
            Object.DestroyImmediate(entry.Objects[0].Resolve());
        }

        [Test]
        public void Clean_NothingMissing_ReturnsFalse()
        {
            SelectionHistoryBuffer history = CreateHistory("A", "B");

            Assert.IsFalse(history.Clean());
            Assert.AreEqual(2, history.Count);
        }

        [Test]
        public void Clean_EntryWithDestroyedObjects_IsRemoved()
        {
            SelectionHistoryBuffer history = CreateHistory("A", "B", "C");
            SelectionSnapshot a = history.Entries[0];
            SelectionSnapshot c = history.Entries[2];

            DestroyFirstObject(history.Entries[1]);

            Assert.IsTrue(history.Clean());
            Assert.AreEqual(2, history.Count);
            Assert.AreSame(a, history.Entries[0]);
            Assert.AreSame(c, history.Entries[1]);
        }

        [Test]
        public void Clean_EntryBeforeCurrentRemoved_KeepsCurrentEntry()
        {
            SelectionHistoryBuffer history = CreateHistory("A", "B", "C");
            SelectionSnapshot current = history.Current;

            DestroyFirstObject(history.Entries[0]);
            history.Clean();

            Assert.AreEqual(1, history.CurrentIndex);
            Assert.AreSame(current, history.Current);
        }

        [Test]
        public void Clean_CurrentEntryRemoved_PreviousEntryBecomesCurrent()
        {
            SelectionHistoryBuffer history = CreateHistory("A", "B", "C");
            SelectionSnapshot b = history.Entries[1];

            DestroyFirstObject(history.Current);
            history.Clean();

            Assert.AreEqual(1, history.CurrentIndex);
            Assert.AreSame(b, history.Current);
        }

        [Test]
        public void Clean_FirstEntryRemovedWhileCurrent_NextEntryBecomesCurrent()
        {
            SelectionHistoryBuffer history = CreateHistory("A", "B");
            SelectionSnapshot b = history.Entries[1];
            history.SetCurrentIndex(0);

            DestroyFirstObject(history.Current);
            history.Clean();

            Assert.AreEqual(0, history.CurrentIndex);
            Assert.AreSame(b, history.Current);
        }

        [Test]
        public void Clean_AllEntriesRemoved_EmptiesHistory()
        {
            SelectionHistoryBuffer history = CreateHistory("A", "B");

            DestroyFirstObject(history.Entries[0]);
            DestroyFirstObject(history.Entries[1]);

            Assert.IsTrue(history.Clean());
            Assert.AreEqual(0, history.Count);
            Assert.AreEqual(-1, history.CurrentIndex);
        }

        [Test]
        public void Clean_UnsavedObjectsAfterSceneReload_AreRemoved()
        {
            SelectionHistoryBuffer history = CreateHistory("A", "B");

            // Simulates a scene reloaded without saving: the new untitled scene has the same (empty) path, but not the objects
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Assert.IsTrue(history.Clean());
            Assert.AreEqual(0, history.Count);
        }

        #endregion

    }

}
