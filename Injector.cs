using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DependencyInjection
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public sealed class InjectAttribute : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ProvideAttribute : Attribute
    {
    }
    
    public interface IDependencyProvider {}

    [DefaultExecutionOrder(-1000)]
    public class Injector : Singleton<Injector> 
    {
        const BindingFlags k_bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        
        readonly Dictionary<Type, object> registry = new Dictionary<Type, object>();

        protected override void Awake()
        {
            base.Awake();
            
            // find all modules implementing IDependencyProvider
            var providers = FindMonoBehaviours().OfType<IDependencyProvider>();
            foreach (var provider in providers)
            {
                RegisterProvider(provider);
            }
            
            var injectables = FindMonoBehaviours().Where(IsInjectable);
            foreach (var injectable in injectables)
            {
                Inject(injectable);
            }
            
            
        }

        void Inject(object instance)
        {
            var type = instance.GetType();
            var injectableFields = type.GetFields(k_bindingFlags)
                .Where(member => Attribute.IsDefined(member, typeof(InjectAttribute)));
            foreach (var injectableField in injectableFields)
            {
                var fieldType = injectableField.FieldType;
                var resolveInstance = Resolve(fieldType);
                if (resolveInstance == null)
                {
                    Debug.LogError($"Can't resolve {fieldType.Name} for {type.Name}");
                }
                
                injectableField.SetValue(instance, resolveInstance);
            }
            
            var injectableMethods = type.GetMethods(k_bindingFlags)
                .Where(member => Attribute.IsDefined(member, typeof(InjectAttribute)));
            foreach (var injectableMethod in injectableMethods)
            {
                var requiredParameters = injectableMethod.GetParameters().Select(p => p.ParameterType).ToArray();
                var resolveInstances = requiredParameters.Select(Resolve).ToArray();
                if (resolveInstances.Any(resolveInstance => resolveInstance == null))
                {
                    throw new Exception($"Failed to resolve {type.Name} for {injectableMethod.Name}");
                }
                injectableMethod.Invoke(instance, resolveInstances);
            }
        }

        object Resolve(Type type)
        {
            registry.TryGetValue(type, out var resultInstance);
            return resultInstance;
        }

        static bool IsInjectable(MonoBehaviour obj)
        {
            var members = obj.GetType().GetMembers(k_bindingFlags);
            return members.Any(member => Attribute.IsDefined(member, typeof(InjectAttribute)));
        }

        void RegisterProvider(IDependencyProvider provider)
        {
            var methods = provider.GetType().GetMethods(k_bindingFlags);
            foreach (var method in methods)
            {
                if (!Attribute.IsDefined(method, typeof(ProvideAttribute))) continue;
                var returnType = method.ReturnType;
                var providerInstance = method.Invoke(provider, null);
                if (providerInstance != null)
                {
                    registry.Add(returnType, providerInstance);
                }
                else
                {
                    throw new Exception($"Provider:{provider.GetType().Name} return null for{returnType.Name}");
                }
            }
        }

        static MonoBehaviour[] FindMonoBehaviours()
        {
            return FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.InstanceID);
        }
    }
}