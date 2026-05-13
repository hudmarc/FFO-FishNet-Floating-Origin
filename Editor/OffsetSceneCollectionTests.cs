using FloatingOffset.Runtime.Types;
using NUnit.Framework;
using System;

namespace FloatingOffset.Runtime
{
    // A simple mock handler to satisfy the IOffsetHandler interface during testing
    public class MockOffsetHandler : IOffsetHandler<int>
    {
        private int _sceneKey;
        public MockOffsetHandler(int sceneKey) => _sceneKey = sceneKey;

        public void Clone(int scene, Action<(int scene, float delta)> onSceneReady)
        {
            throw new NotImplementedException();
        }

        public void Clone(int scene, Action<int> onSceneReady)
        {
            throw new NotImplementedException();
        }

        public void RegisterOffsettable(IOffsettable<int> offsettable, int scene)
        {
            throw new NotImplementedException();
        }

        public void TransferAllTo(OffsetScene<int> from, OffsetScene<int> to)
        {
            throw new NotImplementedException();
        }

        public void TransferTo(IOffsetObject<int> offsettable, OffsetScene<int> from, OffsetScene<int> to, bool reposition = false)
        {
            throw new NotImplementedException();
        }

        public void UpdateOffset(OffsetScene<int> scene)
        {
            throw new NotImplementedException();
        }
    }

    // A simple mock object to satisfy the IOffsetObject interface
    public class MockOffsetObject : IOffsetObject<int>
    {
        private int _sceneKey;
        public MockOffsetObject(int sceneKey) => _sceneKey = sceneKey;

        public Vector3d GetEnginePosition()
        {
            throw new NotImplementedException();
        }

        public float GetEnginePositionSquareMagnitude()
        {
            throw new NotImplementedException();
        }

        public Vector3d GetRealPosition()
        {
            throw new NotImplementedException();
        }

        public int GetSceneKey() => _sceneKey;

        public bool IsValid()
        {
            throw new NotImplementedException();
        }

        public bool IsView()
        {
            throw new NotImplementedException();
        }

        public void MoveTo(int scene)
        {
            throw new NotImplementedException();
        }

        public void SetEnginePosition(Vector3d vector3d)
        {
            throw new NotImplementedException();
        }
    }

    [TestFixture]
    public class OffsetSceneCollectionTests
    {
        private OffsetSceneCollection<int> _collection;
        private MockOffsetHandler _initialHandler;
        private const int INITIAL_SCENE_KEY = 100;

        [SetUp]
        public void Setup()
        {
            // Runs before every single test to guarantee a clean, isolated state
            _initialHandler = new MockOffsetHandler(INITIAL_SCENE_KEY);
            _collection = new OffsetSceneCollection<int>();
            _collection.Register(INITIAL_SCENE_KEY);
        }

        [TearDown]
        public void Teardown()
        {
            // Clean up any loose references if necessary
            _collection = null;
        }
        #region View Management

        [Test]
        public void AddView_WhenValidView_IncrementsViewCount()
        {
            // Arrange
            var view = new MockOffsetObject(INITIAL_SCENE_KEY);
            int initialCount = _collection.GetViewCount(INITIAL_SCENE_KEY);

            // Act
            _collection.AddView(view.GetSceneKey());

            // Assert
            Assert.AreEqual(initialCount + 1, _collection.GetViewCount(INITIAL_SCENE_KEY));
        }

        [Test]
        public void RemoveView_WhenValidView_DecrementsViewCount()
        {
            // Arrange
            var view = new MockOffsetObject(INITIAL_SCENE_KEY);
            _collection.AddView(view.GetSceneKey()); // Add it first
            int countAfterAdd = _collection.GetViewCount(INITIAL_SCENE_KEY);

            // Act
            _collection.RemoveView(view.GetSceneKey());

            // Assert
            Assert.AreEqual(countAfterAdd - 1, _collection.GetViewCount(INITIAL_SCENE_KEY));
        }

        [Test]
        public void SetEmpty_WhenSceneIsActive_SetsViewCountToZeroAndMovesToEmptyZone()
        {
            // Arrange
            var view = new MockOffsetObject(INITIAL_SCENE_KEY);
            _collection.AddView(view.GetSceneKey()); // Force it to be active

            // Act
            _collection.SetEmpty(0); // Assuming it's at index 0

            // Assert
            Assert.AreEqual(0, _collection.GetViewCount(INITIAL_SCENE_KEY));
        }

        #endregion

        #region Data Retrieval

        [Test]
        public void GetOffset_WhenRequested_ReturnsCorrectVector3d()
        {
            // Arrange (Requires a way to set the offset first, assume it defaults to zero)
            Vector3d expectedOffset = Vector3d.zero;

            // Act
            Vector3d actualOffset = _collection.GetOffset(INITIAL_SCENE_KEY);

            // Assert
            Assert.AreEqual(expectedOffset, actualOffset);
        }

        #endregion

        #region Array Resizing

        [Test]
        public void RegisterHandler_WhenCapacityExceeded_SuccessfullyResizesArray()
        {
            // Arrange
            int initialCapacity = _collection.Capacity;

            // Act
            // Force the collection to double in size
            for (int i = 0; i < initialCapacity + 1; i++)
            {
                _collection.Register(200 + i);
            }

            // Assert
            Assert.Greater(_collection.Capacity, initialCapacity, "The array should have doubled in size to accommodate new elements.");
        }

        #endregion
    }
}