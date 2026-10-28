using NUnit.Framework;

using UnityEngine;
using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Selection.Tests
{

    /// <summary>
    /// Tests for <see cref="SelectionDisplayUtility"/>.
    /// </summary>
    /// <remarks>
    /// Objects created in the Test Runner's untitled scene are temporary (they don't have a stable id), so a temporary asset is used as
    /// a non-temporary object.
    /// </remarks>
    public class SelectionDisplayUtilityTests : SceneTestsBase
    {

        /// <summary>
        /// The path of the temporary asset. Its name must be shorter than <see cref="SelectionDisplayUtility.MaxNameLength"/>, so it's not
        /// truncated when naming a selection.
        /// </summary>
        private const string TempAssetPath = "Assets/__SideXP_SelectionNameTest__.anim";

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

        #region Truncation

        [Test]
        public void TruncateByWord_ShortText_IsUnchanged()
        {
            Assert.AreEqual("one two", SelectionDisplayUtility.TruncateByWord("one two", 10));
            Assert.AreEqual("0123456789", SelectionDisplayUtility.TruncateByWord("0123456789", 10));
        }

        [Test]
        public void TruncateByWord_NullOrEmptyText_ReturnsEmptyString()
        {
            Assert.AreEqual(string.Empty, SelectionDisplayUtility.TruncateByWord(null, 10));
            Assert.AreEqual(string.Empty, SelectionDisplayUtility.TruncateByWord(string.Empty, 10));
        }

        [Test]
        public void TruncateByWord_WordFitsExactly_KeepsWholeWord()
        {
            // The character right after the available space is a separator
            Assert.AreEqual("one two...", SelectionDisplayUtility.TruncateByWord("one two three", 10));
        }

        [Test]
        public void TruncateByWord_WordCut_RemovesPartialWord()
        {
            Assert.AreEqual("one...", SelectionDisplayUtility.TruncateByWord("one two three", 9));
        }

        [Test]
        public void TruncateByWord_CommaSeparatedNames_RemovesTrailingSeparators()
        {
            Assert.AreEqual("A, B...", SelectionDisplayUtility.TruncateByWord("A, B, Cdefgh", 8));
        }

        [Test]
        public void TruncateByWord_SingleLongWord_IsCut()
        {
            Assert.AreEqual("abcde...", SelectionDisplayUtility.TruncateByWord("abcdefghijkl", 8));
        }

        [Test]
        public void TruncateByWord_LongText_DoesNotExceedMaxLength()
        {
            string result = SelectionDisplayUtility.TruncateByWord("Environment_Lighting_Root, Main Camera, Player", 40);

            Assert.AreEqual("Environment_Lighting_Root, Main...", result);
            Assert.LessOrEqual(result.Length, 40);
        }

        #endregion


        #region Naming

        [Test]
        public void GetName_TemporaryObjects_JoinsFirstNames()
        {
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[]
            {
                CreateGameObject("A"),
                CreateGameObject("B"),
                CreateGameObject("C"),
                CreateGameObject("D"),
            });

            Assert.AreEqual("A, B, C", SelectionDisplayUtility.GetName(snapshot));
        }

        [Test]
        public void GetName_TemporaryAndPersistentObjects_UsesPersistentObjectsOnly()
        {
            AnimationClip clip = CreateTempAsset();
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { CreateGameObject("Temporary"), clip });

            Assert.AreEqual(clip.name, SelectionDisplayUtility.GetName(snapshot));
        }

        [Test]
        public void GetName_EmptySnapshot_ReturnsEmptyString()
        {
            Assert.AreEqual(string.Empty, SelectionDisplayUtility.GetName(new SelectionSnapshot(null)));
        }

        [Test]
        public void GetMainObject_TemporaryAndPersistentObjects_ReturnsFirstPersistentObject()
        {
            AnimationClip clip = CreateTempAsset();
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { CreateGameObject("Temporary"), clip });

            Assert.IsTrue(SelectionDisplayUtility.GetMainObject(snapshot).Is(clip));
        }

        [Test]
        public void GetMainObject_TemporaryObjects_ReturnsFirstObject()
        {
            GameObject a = CreateGameObject("A");
            SelectionSnapshot snapshot = new SelectionSnapshot(new Object[] { a, CreateGameObject("B") });

            Assert.IsTrue(SelectionDisplayUtility.GetMainObject(snapshot).Is(a));
        }

        [Test]
        public void GetMainObject_EmptySnapshot_ReturnsNull()
        {
            Assert.IsNull(SelectionDisplayUtility.GetMainObject(new SelectionSnapshot(null)));
        }

        #endregion

    }

}
