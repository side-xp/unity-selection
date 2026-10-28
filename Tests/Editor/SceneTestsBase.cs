using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.SceneManagement;

using Object = UnityEngine.Object;

namespace SideXP.Selection.Tests
{

    /// <summary>
    /// Base class for tests that create objects in the active scene.
    /// </summary>
    /// <remarks>
    /// For edit mode runs, the Test Runner stores the scene setup, opens a new untitled scene and restores the original setup once the
    /// run is finished. The tests use that untitled scene, and the objects they create are destroyed on teardown.
    /// </remarks>
    public abstract class SceneTestsBase
    {

        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [SetUp]
        public void SetUpScene()
        {
            // Never create test objects in a user scene, in case these tests are run outside of the Test Runner's own scene
            if (!string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                Assert.Ignore("These tests are meant to run in the untitled scene opened by the Test Runner.");
        }

        [TearDown]
        public void TearDownScene()
        {
            foreach (GameObject gameObject in _createdObjects)
            {
                if (gameObject != null)
                    Object.DestroyImmediate(gameObject);
            }
            _createdObjects.Clear();
        }

        /// <summary>
        /// Creates a GameObject in the active scene, destroyed on teardown.
        /// </summary>
        /// <param name="name">The name of the GameObject.</param>
        /// <param name="parent">The parent of the GameObject. If null, the GameObject is created at the root of the active scene.</param>
        /// <returns>Returns the created GameObject.</returns>
        protected GameObject CreateGameObject(string name, GameObject parent = null)
        {
            GameObject gameObject = new GameObject(name);
            if (parent != null)
                gameObject.transform.SetParent(parent.transform, false);

            _createdObjects.Add(gameObject);
            return gameObject;
        }

    }

}
