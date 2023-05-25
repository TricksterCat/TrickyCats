using System;
using System.Collections.Generic;
using System.Reflection;
using Core.Base;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class FastConvertToEntity : MonoBehaviour
{
    private Entity _entity;
    private bool _isCreate;

    [SerializeField]
    private string _tag;
    
    private class EntityArchetypeCache
    {
        public EntityArchetype EntityArchetype;
        public ComponentType[] Types;

        public EntityArchetypeCache(EntityArchetype entityArchetype, ComponentType[] types)
        {
            EntityArchetype = entityArchetype;
            Types = types;
        }
    }

    private static Dictionary<string, EntityArchetypeCache> _caches = new Dictionary<string, EntityArchetypeCache>();
    private static MethodInfo _method;

    static FastConvertToEntity()
    {
        _method = typeof(GameObjectEntity).GetMethod("CreateEntity", BindingFlags.Static | BindingFlags.NonPublic);
        
    }
    
    private void Start()
    {
        if (string.IsNullOrEmpty(_tag))
        {
            Debug.LogError("Tag is null!", gameObject);
            Destroy(this);
            return;
        }
        var world = World.DefaultGameObjectInjectionWorld;
        var entityManager = world.EntityManager;
        //var entity = _entity = GameObjectEntity.AddToEntityManager(entityManager, gameObject);

        EntityArchetypeCache entityArchetypeCache;
        List<Component> components;
        if(!TryGetArchetype(entityManager, out components, out entityArchetypeCache))
        {
            Destroy(this);
            return;
        }

        var entity = (Entity)_method.Invoke(null, new object[] {entityManager, entityArchetypeCache.EntityArchetype, components, entityArchetypeCache.Types});
        _entity = entity;
        for (int i = 0; i < components.Count; i++)
        {
            var component = components[i] as IConvertGameObjectToEntity;
            component?.Convert(entity, entityManager, null);
        }
        
        entityManager.AddComponents(entity, new ComponentTypes(ComponentType.ReadWrite<Translation>(), ComponentType.ReadWrite<Rotation>(), ComponentType.ReadWrite<LocalToWorld>()));
        TmpList<Component>.Release(components);
    }

    private void OnDisable()
    {
        if(World.DefaultGameObjectInjectionWorld != null)
            World.DefaultGameObjectInjectionWorld.EntityManager.SetEnabled(_entity, false);
    }

    private void OnEnable()
    {
        if(!_entity.Equals(Entity.Null))
            World.DefaultGameObjectInjectionWorld.EntityManager.SetEnabled(_entity, true);
    }

    private bool TryGetArchetype(EntityManager entityManager, out List<Component> components, out EntityArchetypeCache entityArchetypeCache)
    {
        components = TmpList<Component>.Get();
        if (!_caches.TryGetValue(_tag, out entityArchetypeCache))
        {
            gameObject.GetComponents(components);
            var componentCount = 0;
            for (var i = components.Count - 1; i != -1; i--)
            {
                var component = components[i];
                var componentData = component as ComponentDataProxyBase;

                if (component == null)
                    Debug.LogWarning($"The referenced script is missing on {gameObject.name}", gameObject);
                else if (componentData != null)
                    componentCount++;
                else if (!(component is GameObjectEntity))
                    componentCount++;
            }
        
            var types = new ComponentType[componentCount]; 
            var t = 0;
            for (var i = 0; i != components.Count; i++)
            {
                var com = components[i];
                var componentData = com as ComponentDataProxyBase;

                if (componentData != null)
                    types[t++] = new ComponentType(componentData.GetType());
                else if (!(com is GameObjectEntity) && com != null)
                    types[t++] = com.GetType();
            }

            try
            {
                var entityArchetype = entityManager.CreateArchetype(types);
                _caches[_tag] = entityArchetypeCache = new EntityArchetypeCache(entityArchetype, types);
            }
            catch (Exception e)
            {
                for (int i = 0; i < types.Length; ++i)
                {
                    if (Array.IndexOf(types, types[i]) != i)
                    {
                        Debug.LogWarning($"GameObject '{gameObject}' has multiple {types[i]} components and cannot be converted, skipping.");
                        goto FAILED;
                    }
                }
                
                FAILED:
                _caches[_tag] = null;
                TmpList<Component>.Release(components);
                components = null;
                return false;
            }
        }
        else
        {
            if (entityArchetypeCache == null)
            {
                TmpList<Component>.Release(components);
                components = null;
                return false;
            }
            gameObject.GetComponents(components);
        }

        return true;
    }

    private void OnDestroy()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        world?.EntityManager.DestroyEntity(_entity);
    }

    public void Convert(EntityManager entityManager, EntityCommandBuffer commands)
    {
        if (_isCreate)
            return;
        _isCreate = true;
        
        ComponentType[] types;
        GetComponents(gameObject, out types);

        EntityArchetype archetype;
        try
        {
            archetype = entityManager.CreateArchetype(types);
        }
        catch (Exception e)
        {
            for (int i = 0; i < types.Length; ++i)
            {
                if (Array.IndexOf(types, types[i]) != i)
                {
                    Debug.LogWarning($"GameObject '{gameObject}' has multiple {types[i]} components and cannot be converted, skipping.");
                    Destroy(this);
                    return;
                }
            }
            Destroy(this);
            return;
        }

        _entity = commands.CreateEntity(archetype);
    }

    private static void GetComponents(GameObject gameObject, out ComponentType[] types)
    {
        var components = TmpList<Component>.Get();
        gameObject.GetComponents(components);
        
        var componentCount = 0;
        for (var i = components.Count - 1; i != -1; i--)
        {
            var component = components[i];
            var componentData = component as ComponentDataProxyBase;

            if (component == null)
                Debug.LogWarning($"The referenced script is missing on {gameObject.name}", gameObject);
            else if (componentData != null)
                componentCount++;
            else if (!(component is GameObjectEntity))
                componentCount++;
        }
        
        types = new ComponentType[componentCount]; 
        var t = 0;
        for (var i = 0; i != components.Count; i++)
        {
            var com = components[i];
            var componentData = com as ComponentDataProxyBase;

            if (componentData != null)
                types[t++] = new ComponentType(componentData.GetType());
            else if (!(com is GameObjectEntity) && com != null)
                types[t++] = com.GetType();
        }
        
        TmpList<Component>.Release(components);
    }
}
