﻿using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;
public class ParticleBootstrap : MonoBehaviour
{
    [Header("ParticleSystem Settings")]
    [SerializeField]
    protected ParticleSystemDataObj sysObj;
    [SerializeField]
    protected ParticleEntityDataObj entityObj;
    public bool isEmitting { protected set; get; }

    protected EntityManager em;
    protected Entity emitter;
    protected ParticleSystemData data;

    private void OnDestroy()
    {
        DestroyEntities();
    }

    protected void Init(ComponentType systemTag)
    {
        em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityArchetype arch = em.CreateArchetype(
            //for transform
            typeof(Translation),
            typeof(Rotation),
            typeof(Scale),
            //for rendering
            typeof(LocalToWorld),
            typeof(RenderMesh),  
            typeof(RenderBounds),
            typeof(PerInstanceCullingTag), //added this cuz converted GameObjects have this
            typeof(FrozenRenderSceneTag), //this is disable rendering of the entities

            //custom
            typeof(ParticleEntityData), 
            systemTag
        );

        NativeArray<Entity> entArr = new NativeArray<Entity>(sysObj.maxNumParticles, Allocator.Temp);
        em.CreateEntity(arch, entArr);

        foreach (Entity e in entArr)
        {

            em.SetSharedComponentData(e, new RenderMesh { mesh = entityObj.mesh, material = entityObj.material, });
            em.SetComponentData(e, ParticleEntityData.Create(entityObj));
        }

        entArr.Dispose();

        //emitter entity
        emitter = em.CreateEntity(typeof(ParticleSystemData), systemTag);
        data = ParticleSystemData.Create(sysObj);
        em.SetComponentData(emitter, data);
        if (sysObj.enabledOnAwake)
            Emit();
    }

    protected virtual void DestroyEntities() 
    {
        Debug.LogError("You must implement your own system to clean up the particles!");   
    }

    public void SetEmitterSource(Vector3 source, Vector3 dir)
    {
        data.pos = source;
        data.dir = dir.normalized;
        em.SetComponentData(emitter, data);
    }

    public void Emit()
    {
        em.AddComponent(emitter, typeof(ParticleEmitTag));
        isEmitting = true;
    }
    public void StopEmit()
    {
        em.RemoveComponent(emitter, typeof(ParticleEmitTag));
        isEmitting = false;
    }
}
