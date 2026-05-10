using NUnit.Framework;
using FloatingOffset.Runtime;
using UnityEngine;
using System;

namespace FloatingOffset.Runtime
{
    // A simple mock handler to satisfy the IOffsetHandler interface during testing
    public class MockOffsetHandler : IOffsetHandler<int>
    {
        private int _sceneKey;
        public MockOffsetHandler(int sceneKey) => _sceneKey = sceneKey;

        public void Clone(Action<(int scene, float delta)> onSceneReady)
        {
            throw new NotImplementedException();
        }

        public Vector3d GetOffset()
        {
            throw new NotImplementedException();
        }

        public int GetSceneKey() => _sceneKey;

        public void MoveAllTo(OffsetScene<int> scene)
        {
            throw new NotImplementedException();
        }

        public void MoveTo(IOffsetObject<int> offsettable, OffsetScene<int> scene)
        {
            throw new NotImplementedException();
        }

        public void RegisterOffsettable(IOffsettable offsettable)
        {
            throw new NotImplementedException();
        }

        public void UnregisterOffsettable(IOffsettable offsettable)
        {
            throw new NotImplementedException();
        }

        public void UpdateOffset(OffsetScene<int> scene, float delta = 0)
        {
            throw new NotImplementedException();
        }

        public void UpdateOffset(Vector3d offset, Vector3d velocity, float delta = 0)
        {
            throw new NotImplementedException();
        }

        // Add any other required interface methods here...
    }

    // A simple mock object to satisfy the IOffsetObject interface
    public class MockOffsetObject : IOffsetObject<int>
    {
        private int _sceneKey;
        public MockOffsetObject(int sceneKey) => _sceneKey = sceneKey;

        public float EngineVelocitySquaredMagnitude()
        {
            throw new NotImplementedException();
        }

        public Vector3d GetEnginePosition()
        {
            throw new NotImplementedException();
        }

        public float GetEnginePositionSquareMagnitude()
        {
            throw new NotImplementedException();
        }

        public Vector3d GetEngineVelocity()
        {
            throw new NotImplementedException();
        }

        public Vector3d GetRealPosition()
        {
            throw new NotImplementedException();
        }

        public Vector3d GetRealVelocity()
        {
            throw new NotImplementedException();
        }

        public int GetSceneKey() => _sceneKey;

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
            _collection.Register(INITIAL_SCENE_KEY, _initialHandler);
        }

        [TearDown]
        public void Teardown()
        {
            // Clean up any loose references if necessary
            _collection = null;
        }

        #region Registration & Unregistration

        [Test]
        public void AddSceneHandler_WhenValidHandler_AddsToCollectionAndReturnsIndex()
        {
            // Arrange
            var newHandler = new MockOffsetHandler(101);

            // Act
            _collection.Register(101, newHandler);

            // Assert
            Assert.AreEqual(2, _collection.Count, "Collection should have expanded to hold the new scene.");
            Assert.AreEqual(newHandler, _collection.GetHandler(101), "The handler retrieved should match the one added.");
        }

        [Test]
        public void UnregisterHandler_WhenSceneExists_MovesToDeadZoneAndClearsData()
        {
            // Arrange
            // (Setup is already done, INITIAL_SCENE_KEY exists)

            // Act
            _collection.Unregister(INITIAL_SCENE_KEY);

            // Assert
            Assert.IsNull(_collection.GetHandler(0), "Handler should be null after being unregistered.");
            // Add assertion here to verify active/alive counts if you expose them for testing
        }

        #endregion

        #region View Management

        [Test]
        public void AddView_WhenValidView_IncrementsViewCount()
        {
            // Arrange
            var view = new MockOffsetObject(INITIAL_SCENE_KEY);
            int initialCount = _collection.GetViewCount(INITIAL_SCENE_KEY);

            // Act
            _collection.AddView(view);

            // Assert
            Assert.AreEqual(initialCount + 1, _collection.GetViewCount(INITIAL_SCENE_KEY));
        }

        [Test]
        public void RemoveView_WhenValidView_DecrementsViewCount()
        {
            // Arrange
            var view = new MockOffsetObject(INITIAL_SCENE_KEY);
            _collection.AddView(view); // Add it first
            int countAfterAdd = _collection.GetViewCount(INITIAL_SCENE_KEY);

            // Act
            _collection.RemoveView(view);

            // Assert
            Assert.AreEqual(countAfterAdd - 1, _collection.GetViewCount(INITIAL_SCENE_KEY));
        }

        [Test]
        public void SetEmpty_WhenSceneIsActive_SetsViewCountToZeroAndMovesToEmptyZone()
        {
            // Arrange
            var view = new MockOffsetObject(INITIAL_SCENE_KEY);
            _collection.AddView(view); // Force it to be active
            
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

        [Test]
        public void GetVelocity_WhenRequested_ReturnsCorrectVector3d()
        {
            // Arrange
            Vector3d expectedVelocity = Vector3d.zero;

            // Act
            Vector3d actualVelocity = _collection.GetVelocity(INITIAL_SCENE_KEY);

            // Assert
            Assert.AreEqual(expectedVelocity, actualVelocity);
        }

        #endregion
        
        #region Array Resizing

        [Test]
        public void RegisterHandler_WhenCapacityExceeded_SuccessfullyResizesArray()
        {
            // Arrange
            int initialCapacity = _collection.Count;
            
            // Act
            // Force the collection to double in size
            for (int i = 0; i < initialCapacity + 1; i++)
            {
                _collection.Register(200 + i, new MockOffsetHandler(200 + i));
            }

            // Assert
            Assert.Greater(_collection.Count, initialCapacity, "The array should have doubled in size to accommodate new elements.");
        }

        #endregion
    }
}