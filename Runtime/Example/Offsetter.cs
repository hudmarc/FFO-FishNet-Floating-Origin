using FishNet.FloatingOrigin.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FishNet.FloatingOrigin.Example
{
    // Loosely based on the Unity Wiki FloatingOrigin script by Peter Stirling
    // URL: http://wiki.unity3d.com/index.php/Floating_Origin
    public class Offsetter : MonoBehaviour, IOffsetter
    {
        [Tooltip("Should ParticleSystems be moved with an origin shift.")]
        public bool UpdateParticles = true;

        [Tooltip("Should TrailRenderers be moved with an origin shift.")]
        public bool UpdateTrailRenderers = true;

        [Tooltip("Should LineRenderers be moved with an origin shift.")]
        public bool UpdateLineRenderers = true;

        private ParticleSystem.Particle[] parts = null;

        private void MoveRootTransforms(Scene scene, Vector3 offset)
        {
            var objects = scene.GetRootGameObjects();
            foreach (GameObject g in objects)
                g.transform.position += offset;
        }
        private void MoveTrailRenderers(Scene scene, Vector3 offset)
        {
            var trails = FindObjectsOfType<TrailRenderer>() as TrailRenderer[];
            foreach (var trail in trails)
            {
                if (trail.gameObject.scene != scene)
                    continue;

                Vector3[] positions = new Vector3[trail.positionCount];

                int positionCount = trail.GetPositions(positions);
                for (int i = 0; i < positionCount; ++i)
                    positions[i] += offset;

                trail.SetPositions(positions);
            }
        }

        private void MoveLineRenderers(Scene scene, Vector3 offset)
        {
            var lines = FindObjectsOfType<LineRenderer>() as LineRenderer[];
            foreach (var line in lines)
            {
                if (line.gameObject.scene != scene)
                    continue;

                Vector3[] positions = new Vector3[line.positionCount];

                int positionCount = line.GetPositions(positions);
                for (int i = 0; i < positionCount; ++i)
                    positions[i] += offset;

                line.SetPositions(positions);
            }
        }

        private void MoveParticles(Scene scene, Vector3 offset)
        {
            var particles = FindObjectsOfType<ParticleSystem>() as ParticleSystem[];
            foreach (ParticleSystem system in particles)
            {
                if (system.gameObject.scene != scene)
                    continue;

                if (system.main.simulationSpace != ParticleSystemSimulationSpace.World)
                    continue;

                int particlesNeeded = system.main.maxParticles;

                if (particlesNeeded <= 0)
                    continue;

                // ensure a sufficiently large array in which to store the particles
                if (parts == null || parts.Length < particlesNeeded)
                {
                    parts = new ParticleSystem.Particle[particlesNeeded];
                }

                // now get the particles
                int num = system.GetParticles(parts);

                for (int i = 0; i < num; i++)
                {
                    parts[i].position += offset;
                }

                system.SetParticles(parts, num);
            }
        }

        void IOffsetter.Offset(Scene scene, Vector3 offset)
        {
            // Debug.Log($"Offset {offset.ToString()}");
            MoveRootTransforms(scene, offset);

            if (UpdateParticles)
                MoveParticles(scene, offset);

            if (UpdateTrailRenderers)
                MoveTrailRenderers(scene, offset);

            if (UpdateLineRenderers)
                MoveLineRenderers(scene, offset);
            // Debug.Break();
        }
    }
}
