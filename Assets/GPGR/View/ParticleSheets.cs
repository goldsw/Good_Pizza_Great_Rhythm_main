using UnityEngine;

namespace GPGR.View
{
    static class ParticleSheets
    {
        static Material shared;

        public static Material Shared()
        {
            if (shared != null) return shared;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            shared = shader != null ? new Material(shader) : null;
            return shared;
        }

        public static ParticleSystem Ensure(GameObject host, int sortingOrder)
        {
            ParticleSystem system = host.GetComponent<ParticleSystem>();
            if (system == null) system = host.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.maxParticles = 256;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;

            ParticleSystemRenderer renderer = host.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Shared();
            renderer.sortingOrder = sortingOrder;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return system;
        }
    }
}
