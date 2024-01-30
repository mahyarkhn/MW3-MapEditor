using System;
using System.Reflection;

namespace MapEdit.Core.Reflection
{
    public abstract class Singleton<T> where T : class
    {
        internal static class SingletonAllocator
        {
            internal static T instance;

            static SingletonAllocator()
            {
                CreateInstance(typeof(T));
            }

            public static T CreateInstance(Type type)
            {
                ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
                if (constructors.Length != 0)
                {
                    return instance = (T)Activator.CreateInstance(type);
                }
                ConstructorInfo constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, new ParameterModifier[0]);
                if (constructor == null)
                {
                    throw new Exception(type.FullName + " doesn't have a private/protected constructor so the property cannot be enforced.");
                }
                try
                {
                    return instance = (T)constructor.Invoke(new object[0]);
                }
                catch (Exception innerException)
                {
                    throw new Exception("The Singleton couldnt be constructed, check if " + type.FullName + " has a default constructor", innerException);
                }
            }
        }

        public static T Instance
        {
            get
            {
                return SingletonAllocator.instance;
            }
            protected set
            {
                SingletonAllocator.instance = value;
            }
        }
    }
}
