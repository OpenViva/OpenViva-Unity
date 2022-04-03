using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace RoslynCSharp
{    
    /// <summary>
    /// The method calling convention used when invoking a method.
    /// </summary>
    public enum ProxyCallConvention
    {
        /// <summary>
        /// Call the method as normal.
        /// </summary>
        StandardMethod,
        /// <summary>
        /// Call the method as a Unity coroutine. 
        /// The method should return an 'IEnumerator' to be invoked as a coroutine.
        /// The method will be invoked and managed by a game object and updated every frame.
        /// </summary>
        UnityCoroutine,
        /// <summary>
        /// Call the method based on its return type. 
        /// Methods that return 'IEnumerator' will be automatically invoked as a Unity coroutine.
        /// </summary>
        Any,
    }

    /// <summary>
    /// A <see cref="ScriptProxy"/> acts as a wrapper for a type instance and allows non-concrete communication if the type is unknown at compile time. 
    /// </summary>
    public class ScriptProxy : IDisposable
    {
        // Private
        private ScriptType scriptType = null;
        private ScriptFieldProxy fields = null;
        private ScriptPropertyProxy properies = null;
        private object instance = null;
        
        // Properties
        /// <summary>
        /// Get the <see cref="ScriptType"/> of this proxy object.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The proxy has already been disposed</exception>
        public ScriptType ScriptType
        {
            get
            {
                // Make sure the object has not already been disposed
                CheckDisposed();

                return scriptType;
            }
        }

        /// <summary>
        /// Returns the <see cref="IScriptMemberProxy"/> that provides access to the fields of the wrapped instance. 
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">The proxy has already been disposed</exception>
        public IScriptMemberProxy Fields
        {
            get
            {
                // Make sure the object has not already been disposed.
                CheckDisposed();

                fields.throwOnError = true;
                return fields;
            }
        }

        /// <summary>
        /// Returns the <see cref="IScriptMemberProxy"/> that provides access to the fields of the wrapped instance. 
        /// Any exceptions thrown when locating or accessing the propery will be handled.
        /// </summary>
        public IScriptMemberProxy SafeFields
        {
            get
            {
                // Make sure the object has not already been disposed.
                CheckDisposed();

                fields.throwOnError = false;
                return fields;
            }
        }

        /// <summary>
        /// Returns the <see cref="IScriptMemberProxy"/> that provides access to the properties of the wrapped instance. 
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">The proxy has already been disposed</exception>
        public IScriptMemberProxy Properties
        {
            get
            {
                // Make sure the object has not already been disposed
                CheckDisposed();

                properies.throwOnError = true;
                return properies;
            }
        }

        /// <summary>
        /// Returns the <see cref="IScriptMemberProxy"/> that provides access to the properties of the wrapped instance.
        /// Any exceptions thrown when locating or accessing the propery will be handled.
        /// </summary>
        public IScriptMemberProxy SafeProperties
        {
            get
            {
                // Make sure the object has not already been disposed
                CheckDisposed();

                properies.throwOnError = false;
                return properies;
            }
        }

        /// <summary>
        /// Get the instance of the script as an object.
        /// Use this property to access the managed instance.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The proxy has already been disposed</exception>
        public object Instance
        {
            get
            {
                // Make sure the object has not already been disposed
                CheckDisposed();

                return instance;
            }
        }

        /// <summary>
        /// Access the wrapped instance as a unity <see cref="UnityEngine.Object"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The proxy has already been disposed</exception>
        public UnityEngine.Object UnityInstance
        {
            get
            {
                // Make sure the object has not already been disposed
                CheckDisposed();

                // Get as object
                return instance as UnityEngine.Object;
            }
        }

        /// <summary>
        /// Get the instance of the script as a <see cref="MonoBehaviour"/>.
        /// This property will return null if the wrapped type does not inherit from <see cref="MonoBehaviour"/>. 
        /// </summary>
        /// <exception cref="ObjectDisposedException">The proxy has already been disposed</exception>
        public MonoBehaviour BehaviourInstance
        {
            get
            {
                // Make sure the object has not already been disposed
                CheckDisposed();

                // Get as monobehaviour
                return instance as MonoBehaviour;
            }
        }

        /// <summary>
        /// Get the instance of the script as a <see cref="ScriptableObject"/>.
        /// This property will return null if the wrapped type does not inherit from <see cref="ScriptableObject"/>.  
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">The proxy has already been disposed</exception>
        public ScriptableObject ScriptableInstance
        {
            get
            {
                // Make sure the object has not already been disposed
                CheckDisposed();

                // Get as scriptable object
                return instance as ScriptableObject;
            }
        }

        /// <summary>
        /// Returns true if the <see cref="ScriptType"/> inherits from <see cref="UnityEngine.Object"/>.
        /// If this value is true then it is safe to cast this proxy into a <see cref="ScriptProxy"/> for Unity specific operations. 
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">The proxy has already been disposed</exception>
        public bool IsUnityObject
        {
            get
            {
                // Make sure the object has not already been disposed
                CheckDisposed();

                // Check for unity object
                return scriptType.IsUnityObject;
            }
        }

        /// <summary>
        /// Returns true if the managed type inherits from <see cref="MonoBehaviour"/>.
        /// This is equivilent of calling <see cref="ScriptType.IsMonoBehaviour"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The proxy has already been disposed</exception>
        public bool IsMonoBehaviour
        {
            get
            {
                // Make sure the object has not already been disposed
                CheckDisposed();

                // Check for mono behaviour
                return scriptType.IsMonoBehaviour;
            }
        }

        /// <summary>
        /// Returns true if the managed type inherits from <see cref="ScriptableObject"/>.
        /// This is equivilent of calling <see cref="ScriptType.IsScriptableObject"/>. 
        /// </summary>
        /// <exception cref="ObjectDisposedException">The proxy has already been disposed</exception>
        public bool IsScriptableObject
        {
            get
            {
                // Make sure the object has not already been disposed
                CheckDisposed();

                // Check for scriptable object
                return scriptType.IsScriptableObject;
            }
        }

        /// <summary>
        /// Returns true if the proxy has been disposed. 
        /// Be careful, the proxy can become disposed automatically if the managed type is destroyed by Unity. 
        /// This can occur during scene changes for MonoBehaviour components and cause the proxy to become invalid.
        /// If you want to make sure the wrapped type is not dispoed automatically then you can call <see cref="MakePersistent"/>.
        /// </summary>
        public bool IsDisposed
        {
            get { return instance == null; }
        }

        // Constructor
        /// <summary>
        /// Create a new instance of a <see cref="ScriptProxy"/>. 
        /// </summary>
        /// <param name="scriptType">The <see cref="ScriptType"/> to create an instance from</param>
        /// <param name="instance">The raw instance</param>
        internal ScriptProxy(ScriptType scriptType, object instance)
        {
            this.scriptType = scriptType;
            this.instance = instance;

            // Create member proxies
            fields = new ScriptFieldProxy(false, scriptType, this);
            properies = new ScriptPropertyProxy(false, scriptType, this);
        }

        // Methods
        /// <summary>
        /// Attempt to call a method on the managed instance with the specifie name.
        /// This works in a similar way as <see cref="UnityEngine.GameObject.SendMessage(string)"/> where the method name is specified.
        /// The target method must not accept any arguments.
        /// The target method will be called using <see cref="ProxyCallConvention.Any"/>
        /// </summary>
        /// <param name="methodName">The name of the method to call</param>
        /// <returns>The value returned from the target method or null if the target method does not return a value</returns>
        /// <returns>The value returned from the target method or null if the target method does not return a value</returns>
        /// <exception cref="ObjectDisposedException">The proxy has already been disposed</exception>
        /// <exception cref="TargetException">The target method could not be found on the managed type</exception>
        public object Call(string methodName)
        {
            return Call(methodName, ProxyCallConvention.Any);
        }

        /// <summary>
        /// Attempt to call a method on the managed instance with the specified name.
        /// This works in a similar way as <see cref="UnityEngine.GameObject.SendMessage(string)"/> where the method name is specified.
        /// The target method must not accept any arguments.
        /// </summary>
        /// <param name="methodName">The name of the method to call</param>
        /// <param name="callConvention">The method calling convention</param>
        /// <returns>The value returned from the target method or null if the target method does not return a value</returns>
        /// <exception cref="ObjectDisposedException">The proxy has already been disposed</exception>
        /// <exception cref="TargetException">The target method could not be found on the managed type</exception>
        public object Call(string methodName, ProxyCallConvention callConvention)
        {
            // Make sure the object has not already been disposed
            CheckDisposed();

            // Find the method
            MethodInfo method = scriptType.FindCachedMethod(methodName, false);

            // Check for error
            if (method == null)
                throw new TargetException(string.Format("Type '{0}' does not define a method called '{1}'", ScriptType, methodName));

            // Call the method
            object result = method.Invoke(instance, null);

            // Check for coroutine
            if((result is IEnumerator) && (callConvention == ProxyCallConvention.Any || callConvention == ProxyCallConvention.UnityCoroutine)  == true)
            {
                // Get the coroutine method
                IEnumerator routine = result as IEnumerator;

                // Check if the calling object is a mono behaviour
                if(IsMonoBehaviour == true)
                {
                    // Get the proxy as a mono behaviour
                    MonoBehaviour mono = GetInstanceAs<MonoBehaviour>(false);

                    // Register the coroutine with the behaviour so that it will be called after yield
                    mono.StartCoroutine(routine);
                }
            }

            // Get the return value
            return result;
        }

        /// <summary>
        /// Attempt to call a method on the managed instance with the specified name and arguments.
        /// This works in a similar was as <see cref="UnityEngine.GameObject.SendMessage(string)"/> where the method name is specified.
        /// Any number of arguments may be specified but the target method must expect the arguments.
        /// The target method will be called using <see cref="ProxyCallConvention.Any"/>
        /// </summary>
        /// <param name="methodName">The name of the method to call</param>
        /// <param name="arguments">The arguments passed to the method</param>
        /// <returns>The value returned from the target method or null if the target method does not return a value</returns>
        /// <exception cref="ObjectDisposedException">The proxy has already been disposed</exception>
        /// <exception cref="TargetException">The target method could not be found on the managed type</exception>
        public object Call(string methodName, params object[] arguments)
        {
            return Call(methodName, ProxyCallConvention.Any, arguments);
        }

        /// <summary>
        /// Attempt to call a method on the managed instance with the specified name and arguments.
        /// This works in a similar was as <see cref="UnityEngine.GameObject.SendMessage(string)"/> where the method name is specified.
        /// Any number of arguments may be specified but the target method must expect the arguments.
        /// </summary>
        /// <param name="methodName">The name of the method to call</param>
        /// <param name="callConvention">The method calling convention</param>
        /// <param name="arguments">The arguments passed to the method</param>
        /// <returns>The value returned from the target method or null if the target method does not return a value</returns>
        /// <exception cref="ObjectDisposedException">The proxy has already been disposed</exception>
        /// <exception cref="TargetException">The target method could not be found on the managed type</exception>
        public object Call(string methodName, ProxyCallConvention callConvention, params object[] arguments)
        {
            // Make sure the object has not already been disposed
            CheckDisposed();

            // Find the method
            MethodInfo method = scriptType.FindCachedMethod(methodName, false);

            // Check for error
            if (method == null)
                throw new TargetException(string.Format("Type '{0}' does not define a method called '{1}'", ScriptType, methodName));
            
            // Call the method
            object result = method.Invoke(instance, arguments);

            // Check for coroutine
            if ((result is IEnumerator) && (callConvention == ProxyCallConvention.Any || callConvention == ProxyCallConvention.UnityCoroutine) == true)
            {
                // Get the coroutine method
                IEnumerator routine = result as IEnumerator;

                // Check if the calling object is a mono behaviour
                if (IsMonoBehaviour == true)
                {
                    // Get the proxy as a mono behaviour
                    MonoBehaviour mono = GetInstanceAs<MonoBehaviour>(false);

                    // Register the coroutine with the behaviour so that it will be called after yield
                    mono.StartCoroutine(routine);
                }
            }

            // Get the return value
            return result;
        }

        /// <summary>
        /// Attempt to call a method on the managed instance with the specified name.
        /// Any exceptions thrown as a result of locating or calling the method will be caught silently.
        /// This works in a similar was as <see cref="UnityEngine.GameObject.SendMessage(string)"/> where the target method name is specified.
        /// The target method must not accept any arguments.
        /// The target method will be called using <see cref="ProxyCallConvention.Any"/>
        /// </summary>
        /// <param name="method">The name of the method to call</param>
        /// <returns>The value returned from the target method or null if the target method does not return a value</returns>
        public object SafeCall(string method)
        {
            return SafeCall(method, ProxyCallConvention.Any);
        }

        /// <summary>
        /// Attempt to call a method on the managed instance with the specified name.
        /// Any exceptions thrown as a result of locating or calling the method will be caught silently.
        /// This works in a similar was as <see cref="UnityEngine.GameObject.SendMessage(string)"/> where the target method name is specified.
        /// The target method must not accept any arguments.
        /// </summary>
        /// <param name="method">The name of the method to call</param>
        /// <param name="callConvention">The method calling convention</param>
        /// <returns>The value returned from the target method or null if the target method does not return a value</returns>
        public object SafeCall(string method, ProxyCallConvention callConvention)
        {
            try
            {
                // Make sure the object has not already been disposed
                CheckDisposed();

                // Catch any exceptions
                return Call(method, callConvention);
            }
            catch
            {
                // Exception - Maybe caused by the target method
                return null;
            }
        }

        /// <summary>
        /// Attempt to call a method on the managed instance with the specified name.
        /// Any exceptions thrown as a result of locating or calling the method will be caught silently.
        /// This works in a similar was as <see cref="UnityEngine.GameObject.SendMessage(string)"/> where the target method name is specified.
        /// Any number of arguments may be specified but the target method must expect the arguments.
        /// The target method will be called using <see cref="ProxyCallConvention.Any"/>
        /// </summary>
        /// <param name="method">The name of the method to call</param>
        /// <param name="arguments">The arguments passed to the method</param>
        /// <returns>The value returned from the target method or null if the target method does not return a value</returns>
        public object SafeCall(string method, params object[] arguments)
        {
            return SafeCall(method, ProxyCallConvention.Any, arguments);
        }

        /// <summary>
        /// Attempt to call a method on the managed instance with the specified name.
        /// Any exceptions thrown as a result of locating or calling the method will be caught silently.
        /// This works in a similar was as <see cref="UnityEngine.GameObject.SendMessage(string)"/> where the target method name is specified.
        /// Any number of arguments may be specified but the target method must expect the arguments.
        /// </summary>
        /// <param name="method">The name of the method to call</param>
        /// <param name="callConvention">The method calling convention</param>
        /// <param name="arguments">The arguments passed to the method</param>
        /// <returns>The value returned from the target method or null if the target method does not return a value</returns>
        public object SafeCall(string method, ProxyCallConvention callConvention, params object[] arguments)
        {
            try
            {
                // Make sure the object has not already been disposed
                CheckDisposed();

                // Catch any exceptions
                return Call(method, callConvention);
            }
            catch
            {
                // Exception - Maybe cause by the target method
                return null;
            }            
        }

        /// <summary>
        /// Get the system type of the managed script type.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">The proxy has already been disposed</exception>
        public Type GetInstanceType()
        {
            // Make sure the object has not already been disposed
            CheckDisposed();

            return instance.GetType();
        }

        /// <summary>
        /// Attempts to get the managed instance as the specified generic type.
        /// </summary>
        /// <typeparam name="T">The generic type to return the instance as</typeparam>
        /// <param name="throwOnError">When false, any exceptions caused by the conversion will be caught and will result in a default value being returned. When true, any exceptions will not be handled.</param>
        /// <param name="errorValue">The value to return when 'throwOnError' is false and an error occurs"/></param>
        /// <returns>The managed instance as the specified generic type or the default value for the generic type if an error occured</returns>
        public T GetInstanceAs<T>(bool throwOnError, T errorValue = default(T))
        {
            // Try a direct cast
            if (throwOnError == true)
                return (T)instance;

            try
            {
                // Try to cast and catch any InvalidCast exceptions.
                T result = (T)instance;

                // Return the result
                return result;
            }
            catch
            {
                // Error value
                return errorValue;
            }
        }

        /// <summary>
        /// Dispose of the proxy and its managed script instance.
        /// Once disposed, the proxy should never be accessed again.
        /// This will also correctly call the 'Dispose' method of the managed object if it implements it.
        /// Only call this method once you are sure you will never need the instance again.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The proxy has already been disposed</exception>
        public virtual void Dispose()
        {
            // Make sure the object has not already been disposed
            CheckDisposed();

            // Check for Unity object
            if (IsUnityObject == true)
            {
                if (Application.isPlaying == true)
                    UnityEngine.Object.Destroy(UnityInstance);
                else
                    UnityEngine.Object.DestroyImmediate(UnityInstance, false);
            }

            // Call the dispose method correctly
            if (instance is IDisposable)
                (instance as IDisposable).Dispose();

            // Unset reference
            scriptType = null;
            instance = null;
        }

        /// <summary>
        /// If the managed object is a Unity type then this method will call 'DontDestroyOnLoad' to ensure that the object is able to survie scene loads.
        /// </summary>
        public void MakePersistent()
        {
            // Make the instance survive scene loads
            if (IsUnityObject == true)
                UnityEngine.Object.DontDestroyOnLoad(UnityInstance);
        }

        /// <summary>
        /// Checks whether the object has already been disposed and raises and exception if it has.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The proxy has already been disposed</exception>
        private void CheckDisposed()
        {
            // Check for already disposed
            if (instance == null)
                throw new ObjectDisposedException("The script has already been disposed. Unity types can be disposed automatically when the wrapped type is destroyed");
        }
    }
}