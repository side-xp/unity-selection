using System;

using NUnit.Framework;

using UnityEngine;

using Object = UnityEngine.Object;

namespace SideXP.Selection.Tests
{

    /// <summary>
    /// Tests for <see cref="SelectionSlotCollection"/>.
    /// </summary>
    public class SelectionSlotCollectionTests : SceneTestsBase
    {

        /// <summary>
        /// Creates a snapshot of the given objects.
        /// </summary>
        /// <param name="objects">The objects to capture.</param>
        /// <returns>Returns the created snapshot.</returns>
        private static SelectionSnapshot CreateSnapshot(params Object[] objects)
        {
            return new SelectionSnapshot(objects);
        }

        #region Construction

        [Test]
        public void Constructor_Default_HasDefaultCountOfUnassignedSlots()
        {
            SelectionSlotCollection slots = new SelectionSlotCollection();

            Assert.AreEqual(SelectionSlotCollection.DefaultCount, slots.Count);
            for (int i = 0; i < slots.Count; i++)
                Assert.IsFalse(slots.IsAssigned(i), $"Slot {i} should not be assigned.");
        }

        [Test]
        public void Resize_Larger_AddsUnassignedSlots()
        {
            SelectionSlotCollection slots = new SelectionSlotCollection(2);
            slots.Set(0, CreateSnapshot(CreateGameObject("A")));

            slots.Resize(4);

            Assert.AreEqual(4, slots.Count);
            Assert.IsTrue(slots.IsAssigned(0), "Existing slots should be kept.");
            Assert.IsFalse(slots.IsAssigned(3));
        }

        [Test]
        public void Resize_Smaller_DiscardsLastSlots()
        {
            SelectionSlotCollection slots = new SelectionSlotCollection(4);
            slots.Set(0, CreateSnapshot(CreateGameObject("A")));

            slots.Resize(2);

            Assert.AreEqual(2, slots.Count);
            Assert.IsTrue(slots.IsAssigned(0));
        }

        #endregion


        #region Assignment

        [Test]
        public void Set_Snapshot_AssignsSlot()
        {
            SelectionSlotCollection slots = new SelectionSlotCollection();
            SelectionSnapshot snapshot = CreateSnapshot(CreateGameObject("A"));

            slots.Set(3, snapshot);

            Assert.IsTrue(slots.IsAssigned(3));
            Assert.AreSame(snapshot, slots.Get(3));
        }

        [Test]
        public void Set_AssignedSlot_ReplacesSelection()
        {
            SelectionSlotCollection slots = new SelectionSlotCollection();
            SelectionSnapshot snapshot = CreateSnapshot(CreateGameObject("B"));
            slots.Set(0, CreateSnapshot(CreateGameObject("A")));

            slots.Set(0, snapshot);

            Assert.AreSame(snapshot, slots.Get(0));
        }

        [Test]
        public void Set_NullOrEmptySnapshot_UnassignsSlot()
        {
            SelectionSlotCollection slots = new SelectionSlotCollection();
            slots.Set(0, CreateSnapshot(CreateGameObject("A")));
            slots.Set(1, CreateSnapshot(CreateGameObject("B")));

            slots.Set(0, null);
            slots.Set(1, CreateSnapshot());

            Assert.IsFalse(slots.IsAssigned(0));
            Assert.IsFalse(slots.IsAssigned(1));
        }

        [Test]
        public void Get_UnassignedSlot_ReturnsNull()
        {
            Assert.IsNull(new SelectionSlotCollection().Get(0));
        }

        [Test]
        public void Clear_AssignedSlot_UnassignsSlotAndReturnsTrue()
        {
            SelectionSlotCollection slots = new SelectionSlotCollection();
            slots.Set(0, CreateSnapshot(CreateGameObject("A")));

            Assert.IsTrue(slots.Clear(0));
            Assert.IsFalse(slots.IsAssigned(0));
        }

        [Test]
        public void Clear_UnassignedSlot_ReturnsFalse()
        {
            Assert.IsFalse(new SelectionSlotCollection().Clear(0));
        }

        [Test]
        public void ClearAll_AssignedSlots_UnassignsAllSlots()
        {
            SelectionSlotCollection slots = new SelectionSlotCollection();
            slots.Set(0, CreateSnapshot(CreateGameObject("A")));
            slots.Set(5, CreateSnapshot(CreateGameObject("B")));

            slots.ClearAll();

            Assert.IsFalse(slots.IsAssigned(0));
            Assert.IsFalse(slots.IsAssigned(5));
            Assert.AreEqual(SelectionSlotCollection.DefaultCount, slots.Count);
        }

        [Test]
        public void Methods_IndexOutOfRange_Throw()
        {
            SelectionSlotCollection slots = new SelectionSlotCollection(2);

            Assert.Throws<ArgumentOutOfRangeException>(() => slots.IsAssigned(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => slots.Get(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => slots.Set(2, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => slots.Clear(2));
        }

        #endregion


        #region Cleaning

        [Test]
        public void Clean_NothingMissing_ReturnsFalse()
        {
            SelectionSlotCollection slots = new SelectionSlotCollection();
            slots.Set(0, CreateSnapshot(CreateGameObject("A")));

            Assert.IsFalse(slots.Clean());
            Assert.IsTrue(slots.IsAssigned(0));
        }

        [Test]
        public void Clean_SomeObjectsDestroyed_KeepsSlotAssigned()
        {
            GameObject a = CreateGameObject("A");
            SelectionSlotCollection slots = new SelectionSlotCollection();
            slots.Set(0, CreateSnapshot(a, CreateGameObject("B")));

            Object.DestroyImmediate(a);

            Assert.IsTrue(slots.Clean());
            Assert.IsTrue(slots.IsAssigned(0));
            Assert.AreEqual(1, slots.Get(0).Count);
        }

        [Test]
        public void Clean_AllObjectsDestroyed_UnassignsSlot()
        {
            GameObject a = CreateGameObject("A");
            SelectionSlotCollection slots = new SelectionSlotCollection();
            slots.Set(0, CreateSnapshot(a));
            slots.Set(1, CreateSnapshot(CreateGameObject("B")));

            Object.DestroyImmediate(a);

            Assert.IsTrue(slots.Clean());
            Assert.IsFalse(slots.IsAssigned(0));
            Assert.IsTrue(slots.IsAssigned(1));
        }

        #endregion

    }

}
