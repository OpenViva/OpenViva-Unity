using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

namespace RoslynCSharp
{
    /// <summary>
    /// Represents a type that may or may not derive from MonoBehaviour.
    /// A <see cref="ScriptType"/> is a wrapper for <see cref="Type"/> that contains methods for Unity specific operations.
    /// The type may also be used to create instances of objects.
    /// </summary>
    public sealed class ScriptType
    {
        // Private
        private static BindingFlags memberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        private static BindingFlags memberInstanceFlags = BindingFlags.Instance | memberFlags;
        private static BindingFlags memberStaticFlags = BindingFlags.Static | memberFlags;
        private static List<ScriptType> matchedTypes = new List<ScriptType>();
        private static List<object> matchedAttributes = new List<object>();
        private HashSet<object> typeAttributes = null;
        private Dictionary<string, FieldInfo> fieldCache = new Dictionary<string, FieldInfo>();
        private Dictionary<string, PropertyInfo> propertyCache = new Dictionary<string, PropertyInfo>();
        private Dictionary<string, MethodInfo> methodCache = new Dictionary<string, MethodInfo>();

        private Type rawType = null;
        private ScriptAssembly assembly = null;
        private ScriptType parent = null;
        private ScriptType[] nestedTypes = null;
        private ScriptFieldProxy fields = null;
        private ScriptPropertyProxy properies = null;

        // Properties
        /// <summary>
        /// Get the <see cref="Type"/> that this <see cref="ScriptType"/> wraps.   
        /// </summary>
        public Type RawType
        {
            get { return rawType; }
        }

        /// <summary>
        /// Get the name of the wrapped type excluding the namespace.
        /// </summary>
        public string Name
        {
            get { return rawType.Name; }
        }

        /// <summary>
        /// Get the namespace of the wrapped type.
        /// </summary>
        public string Namespace
        {
            get { return rawType.Namespace; }
        }

        /// <summary>
        /// Get the full name of the wrapped type including namespace.
        /// </summary>
        public string FullName
        {
            get { return rawType.FullName; }
        }

        /// <summary>
        /// Returns true if the wrapped type is public or false if not.
        /// </summary>
        public bool IsPublic
        {
            get { return rawType.IsPublic; }
        }

        /// <summary>
        /// Get the <see cref="ScriptAssembly"/> that this <see cref="ScriptType"/> is defined in.  
        /// </summary>
        public ScriptAssembly Assembly
        {
            get { return assembly; }
        }

        /// <summary>
        /// Get the <see cref="ScriptType"/> parent for this type. 
        /// The return value will only be valid for nested types otherwise it will be null.
        /// </summary>
        public ScriptType Parent
        {
            get { return parent; }
        }

        /// <summary>
        /// Returns true if this <see cref="ScriptType"/> instance is a nested type.
        /// </summary>
        public bool IsNestedType
        {
            get { return parent != null; }
        }

        /// <summary>
        /// Get all nested <see cref="ScriptType"/> of this type.
        /// If this type does not define any nested types then the return value will be an empty array.
        /// </summary>
        public ScriptType[] NestedTypes
        {
            get { return nestedTypes; }
        }

        /// <summary>
        /// Returns true if this <see cref="ScriptType"/> defines one or more nested types or false if not.
        /// </summary>
        public bool HasNestedTypes
        {
            get { return nestedTypes.Length > 0; }
        }

        /// <summary>
        /// Returns the <see cref="IScriptMemberProxy"/> that provides access to the static fields of the wrapped type. 
        /// </summary>
        public IScriptMemberProxy FieldsStatic
        {
            get
            {
                fields.throwOnError = true;
                return fields;
            }
        }

        /// <summary>
        /// Returns the <see cref="IScriptMemberProxy"/> that provides access to the static fields of the wrapped type.
        /// Any exceptions thrown by locating or accessing the property will be handled.
        /// </summary>
        public IScriptMemberProxy SafeFieldsStatic
        {
            get
            {
                fields.throwOnError = false;
                return fields;
            }
        }

        /// <summary>
        /// Returns the <see cref="IScriptMemberProxy"/> that provides access to the static properties of the wrapped type. 
        /// </summary>
        public IScriptMemberProxy PropertiesStatic
        {
            get
            {
                properies.throwOnError = true;
                return properies;
            }
        }

        /// <summary>
        /// Returns the <see cref="IScriptMemberProxy"/> that provides access to the static properties of the wrapped type.
        /// Any exceptions thrown by locating or accessing the property will be handled.
        /// </summary>
        public IScriptMemberProxy SafePropertiesStatic
        {
            get
            {
                properies.throwOnError = false;
                return properies;
            }
        }

        /// <summary>
        /// Returns true if this type inherits from <see cref="UnityEngine.Object"/>.
        /// See also <see cref="IsMonoBehaviour"/>.
        /// </summary>
        public bool IsUnityObject
        {
            get { return IsSubTypeOf<UnityEngine.Object>(); }
        }

        /// <summary>
        /// Returns true if this type inherits from <see cref="MonoBehaviour"/>.
        /// </summary>
        public bool IsMonoBehaviour
        {
            get { return IsSubTypeOf<MonoBehaviour>(); }
        }

        /// <summary>
        /// Returns true if this type inherits from <see cref="ScriptableObject"/> 
        /// </summary>
        public bool IsScriptableObject
        {
            get { return IsSubTypeOf<ScriptableObject>(); }
        }

        public ICollection<object> CustomAttributes
        {
            get
            {
                // Build the attribute info
                GenerateAttributeInformation();

                return typeAttributes;
            }
        }

        // Constructor
        /// <summary>
        /// Create a <see cref="ScriptType"/> from a <see cref="Type"/>.  
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to create the <see cref="ScriptType"/> from</param>
        public ScriptType(Type type)
        {
            this.assembly = null;
            this.rawType = type;            

            // Create member proxies
            fields = new ScriptFieldProxy(true, this);
            properies = new ScriptPropertyProxy(true, this);

            // Setup nested types
            Type[] nested = type.GetNestedTypes(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            this.nestedTypes = new ScriptType[nested.Length];

            for (int i = 0; i < nested.Length; i++)
                this.nestedTypes[i] = new ScriptType(nested[i]);
        }

        internal ScriptType(ScriptAssembly assembly, ScriptType parent, Type type)
        {
            this.assembly = assembly;
            this.parent = parent;
            this.rawType = type;

            // Create member proxies
            fields = new ScriptFieldProxy(true, this);
            properies = new ScriptPropertyProxy(true, this);

            // Setup nested types
            Type[] nested = type.GetNestedTypes(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            this.nestedTypes = new ScriptType[nested.Length];

            for (int i = 0; i < nested.Length; i++)
                this.nestedTypes[i] = new ScriptType(assembly, this, nested[i]);
        }

        // Methods
        #region CreateInstance
        /// <summary>
        /// Creates an instance of this type.
        /// The type will be constructed using the appropriate method (AddComponent, CreateInstance, new).
        /// </summary>
        /// <param name="parent">The <see cref="GameObject"/> to attach the instance to or null if the type is not a <see cref="MonoBehaviour"/></param>
        /// <returns>An instance of <see cref="ScriptProxy"/></returns>
        public ScriptProxy CreateInstance(GameObject parent = null)
        {
            if (IsMonoBehaviour == true)
            {
                // Create a component instance
                return CreateBehaviourInstance(parent);
            }
            else if (IsScriptableObject == true)
            {
                // Create a scriptable object instance
                return CreateScriptableInstance();
            }

            // Create a C# instance
            return CreateCSharpInstance();
        }

        /// <summary>
        /// Creates an instance of this type.
        /// The type will be constructed using the appropriate method (AddComponent, CreateInstance, new).
        /// </summary>
        /// <param name="parent">The <see cref="GameObject"/> to attach the instance to or null if the type is not a <see cref="MonoBehaviour"/></param>
        /// <param name="parameters">The parameter list for the desired constructor. only used when the type does not inherit from <see cref="UnityEngine.Object"/></param>
        /// <returns>An instance of <see cref="ScriptProxy"/></returns>
        public ScriptProxy CreateInstance(GameObject parent = null, params object[] parameters)
        {
            if (IsMonoBehaviour == true)
            {
                // Create a component instance
                return CreateBehaviourInstance(parent);
            }
            else if (IsScriptableObject == true)
            {
                // Create a scriptable object instance
                return CreateScriptableInstance();
            }

            // Create a C# instance
            return CreateCSharpInstance(parameters);
        }

        /// <summary>
        /// Creates a raw instance of this type.
        /// A raw instance will return the actual instance of the type as opposed to a <see cref="ScriptProxy"/> which allows for more control. 
        /// The type will be constructed using the appropriate method (AddComponent, CreateInstance, new).
        /// </summary>
        /// <param name="parent">The <see cref="GameObject"/> to attach the instance to or null if the type is not a <see cref="MonoBehaviour"/></param>
        /// <returns>A raw instance that can be cast to the desired type</returns>
        public object CreateRawInstance(GameObject parent = null)
        {
            // Call through
            ScriptProxy proxy = CreateInstance(parent);

            // Check for error
            if (proxy == null)
                return null;

            // Get the instance
            return proxy.Instance;
        }

        /// <summary>
        /// Creates a raw instance of this type.
        /// A raw instance will return the actual instance of the type as opposed to a <see cref="ScriptProxy"/> which allows for more control. 
        /// The type will be constructed using the appropriate method (AddComponent, CreateInstance, new).
        /// </summary>
        /// <param name="parent">The <see cref="GameObject"/> to attach the instance to or null if the type is not a <see cref="MonoBehaviour"/></param>
        /// <param name="parameters">The parameter list for the desired constructor. only used when the type does not inherit from <see cref="UnityEngine.Object"/></param>
        /// <returns>A raw instance that can be cast to the desired type</returns>
        public object CreateRawInstance(GameObject parent = null, params object[] parameters)
        {
            // Call through
            ScriptProxy proxy = CreateInstance(parent, parameters);

            // Check for error
            if (proxy == null)
                return null;

            // Get the instance
            return proxy.Instance;
        }

        /// <summary>
        /// Creates an instance of this type and returns the result as the specified generic type.
        /// A raw instance will return the actual instance of the type as opposed to a <see cref="ScriptProxy"/> which allows for more control. 
        /// The type will be constructed using the appropriate method (AddComponent, CreateInstance, new).
        /// </summary>
        /// <typeparam name="T">The generic type to return the instance as</typeparam>
        /// <param name="parent">The <see cref="GameObject"/> to attach the instance to or null if the type is not a <see cref="MonoBehaviour"/></param>
        /// <returns>A raw instance as the specified generic type</returns>
        public T CreateRawInstance<T>(GameObject parent = null) //where T : class
        {
            // Call through
            ScriptProxy proxy = CreateInstance(parent);

            // Check for error
            if (proxy == null)
                return default(T);
            
            // Get the instance
            return proxy.GetInstanceAs<T>(false);
        }

        /// <summary>
        /// Creates an instance of this type and returns the result as the specified generic type.
        /// A raw instance will return the actual instance of the type as opposed to a <see cref="ScriptProxy"/> which allows for more control. 
        /// The type will be constructed using the appropriate method (AddComponent, CreateInstance, new).
        /// </summary>
        /// <typeparam name="T">The generic type to return the instance as</typeparam>
        /// <param name="parent">The <see cref="GameObject"/> to attach the instance to or null if the type is not a <see cref="MonoBehaviour"/></param>
        /// <param name="parameters">The parameter list for the desired constructor. only used when the type does not inherit from <see cref="UnityEngine.Object"/></param>
        /// <returns>A raw instance as the specified generic type</returns>
        public T CreateRawInstance<T>(GameObject parent = null, params object[] parameters) //where T : class
        {
            // Call through
            ScriptProxy proxy = CreateInstance(parent);

            // Check the error
            if (proxy == null)
                return default(T);

            // Get the instance
            return proxy.GetInstanceAs<T>(false);
        }


        #region MainCreateInstance
        private ScriptProxy CreateBehaviourInstance(GameObject parent)
        {
            // Check for null parent
            if (parent == null)
                throw new ArgumentNullException("parent");

            // Try to add component
            MonoBehaviour instance = parent.AddComponent(rawType) as MonoBehaviour;

            // Check for valid instance
            if (instance != null)
            {
                // Create an object proxy
                return new ScriptProxy(this, instance);
            }

            // Error
            return null;
        }

        private ScriptProxy CreateScriptableInstance()
        {
            // Allow unity to create the instance - Note we dont need to use the parent object so it can be null
            ScriptableObject instance = ScriptableObject.CreateInstance(rawType);

            // Check for valid instance
            if (instance != null)
            {
                // Create an object proxy
                return new ScriptProxy(this, instance);
            }

            // Error
            return null;
        }

        private ScriptProxy CreateCSharpInstance(params object[] args)
        {
            // Try to create the type
            object instance = null;

            try
            {
                // Try to create an instance with the default or parameter constructor
                instance = Activator.CreateInstance(rawType, BindingFlags.Default, null, args, null);
            }
            catch(MissingMethodException)
            {
                // Check for arguments
                if (args.Length > 0)
                    return null;

                // Create an instance without calling constructor
                instance = FormatterServices.GetUninitializedObject(rawType);
            }

            // Check for valid instance
            if (instance != null)
            {
                // Create the proxy for the C# instance
                return new ScriptProxy(this, instance);
            }

            // Error
            return null;
        }
        #endregion

        #endregion

        /// <summary>
        /// Returns true if this type inherits from the specified type.
        /// </summary>
        /// <param name="baseClass">The base type</param>
        /// <returns>True if this type inherits from the specified type</returns>
        public bool IsSubTypeOf(Type baseClass)
        {
            // Check for subclass
            return baseClass.IsAssignableFrom(rawType);
        }

        /// <summary>
        /// Returns true if this type inherits from the specified type.
        /// </summary>
        /// <typeparam name="T">The base type</typeparam>
        /// <returns>True if this type inherits from the specified type</returns>
        public bool IsSubTypeOf<T>()
        {
            // Call through
            return IsSubTypeOf(typeof(T));
        }

        /// <summary>
        /// Finds a field with the specified name from the cache if possible.
        /// If the field is not present in the cache then it will be added automatically so that subsequent calls will be quicker.
        /// </summary>
        /// <param name="name">The name of the field to find</param>
        /// <param name="isStatic">Is the target field a static or instance field</param>
        /// <returns>The <see cref="FieldInfo"/> for the specified field</returns>
        public FieldInfo FindCachedField(string name, bool isStatic)
        {
            // Check cache
            if (fieldCache.ContainsKey(name) == true)
                return fieldCache[name];

            // Select binding flags
            BindingFlags flags = (isStatic == true)
                ? memberStaticFlags
                : memberInstanceFlags;

            // Get field with correct flags
            FieldInfo field = rawType.GetField(name, flags);

            // Check for null
            if (field == null)
                return null;

            // Cache the field
            fieldCache.Add(name, field);

            return field;
        }

        /// <summary>
        /// Finds a property with the specified name from the cache if possible.
        /// If the property is not present in the cache then it will be added automatically so that subsequent calls will be quicker.
        /// </summary>
        /// <param name="name">The name of the property to find</param>
        /// <param name="isStatic">Is the target property a static or instance property</param>
        /// <returns>The <see cref="PropertyInfo"/> for the specified property</returns>
        public PropertyInfo FindCachedProperty(string name, bool isStatic)
        {
            // Check cache
            if (propertyCache.ContainsKey(name) == true)
                return propertyCache[name];

            // Select binding flags
            BindingFlags flags = (isStatic == true)
                ? memberStaticFlags
                : memberInstanceFlags;

            // Get property with correct flags
            PropertyInfo property = rawType.GetProperty(name, flags);

            // Check for null
            if (property == null)
                return null;

            // Cache the property
            propertyCache.Add(name, property);

            return property;
        }

        /// <summary>
        /// Finds a method with the specified name from the cache if possible.
        /// If the method is not present in the cache then it will be added automatically so that subsequent calls will be quicker.
        /// </summary>
        /// <param name="name">The name of the method to find</param>
        /// <param name="isStatic">Is the target method a static or instance method</param>
        /// <returns>The <see cref="MethodInfo"/> for the specified method</returns>
        public MethodInfo FindCachedMethod(string name, bool isStatic)
        {
            // Check cache
            if (methodCache.ContainsKey(name) == true)
                return methodCache[name];

            // Select binding flags
            BindingFlags flags = (isStatic == true)
                ? memberStaticFlags
                : memberInstanceFlags;

            // Get method with correct flags
            MethodInfo method = rawType.GetMethod(name, flags);

            // Check for null
            if (method == null)
                return null;

            // Cache the method
            methodCache.Add(name, method);

            return method;
        }

        /// <summary>
        /// Attempts to call a static method on this <see cref="ScriptType"/> with the specified name.
        /// This works in a similar way as <see cref="UnityEngine.GameObject.SendMessage(string)"/> where the method name is specified.
        /// The target method must be static and not accept any arguments.
        /// </summary>
        /// <param name="methodName">The name of the static method to call</param>
        /// <returns>The value returned from the target method or null if the target method does not return a value</returns>
        /// <exception cref="TargetException">The target method could not be found on the managed type</exception>
        /// <exception cref="TargetException">The target method is not static</exception>
        public object CallStatic(string methodName)
        {
            // Find the method
            MethodInfo method = FindCachedMethod(methodName, true);

            // Check for error
            if (method == null)
                throw new TargetException(string.Format("Type '{0}' does not define a static method called '{1}'", this, methodName));

            // Check for static
            if (method.IsStatic == false)
                throw new TargetException(string.Format("The target method '{0}' is not marked as static and must be called on an object", methodName));

            // Call the method
            return method.Invoke(null, null);
        }

        /// <summary>
        /// Attempts to call a static method on this <see cref="ScriptType"/> with the specified name.
        /// This works in a similar way as <see cref="UnityEngine.GameObject.SendMessage(string)"/> where the method name is specified.
        /// The target method must be static and not accept any arguments.
        /// </summary>
        /// <param name="methodName">The name of the static method to call</param>
        /// <param name="arguments">The arguemnts passed to the method</param>
        /// <returns>The value returned from the target method or null if the target method does not return a value</returns>
        /// <exception cref="TargetException">The target method could not be found on the managed type</exception>
        /// <exception cref="TargetException">The target method is not static</exception>
        public object CallStatic(string methodName, params object[] arguments)
        {
            // Find the method
            MethodInfo method = FindCachedMethod(methodName, true);

            // Check for error
            if (method == null)
                throw new TargetException(string.Format("Type '{0}' does not define a static method called '{1}'", this, methodName));

            // Check for static
            if (method.IsStatic == false)
                throw new TargetException(string.Format("The target method '{0}' is not marked as static and must be called on an object", methodName));

            // Call the method
            return method.Invoke(null, arguments);
        }

        /// <summary>
        /// Attempts to call a static method on this <see cref="ScriptType"/> with the specified name.
        /// Any exceptions throw as a result of locating or calling the method will be caught silently
        /// This works in a similar way as <see cref="UnityEngine.GameObject.SendMessage(string)"/> where the method name is specified.
        /// The target method must be static and not accept any arguments.
        /// </summary>
        /// <param name="method">The name of the static method to call</param>
        /// <returns>The value returned from the target method or null if the target method does not return a value</returns>
        public object SafeCallStatic(string method)
        {
            try
            {
                // Call the method and catch any exceptions
                return CallStatic(method);
            }
            catch
            {
                // Exception - Fail silently
                return null;
            }
        }

        /// <summary>
        /// Attempts to call a static method on this <see cref="ScriptType"/> with the specified name.
        /// Any exceptions throw as a result of locating or calling the method will be caught silently
        /// This works in a similar way as <see cref="UnityEngine.GameObject.SendMessage(string)"/> where the method name is specified.
        /// The target method must be static and not accept any arguments.
        /// </summary>
        /// <param name="method">The name of the static method to call</param>
        /// <param name="arguments">The arguments passed to the method</param>
        /// <returns>The value returned from the target method or null if the target method does not return a value</returns>
        public object SafeCallStatic(string method, params object[] arguments)
        {
            try
            {
                // Call the method and catch any exceptions
                return CallStatic(method, arguments);
            }
            catch
            {
                // Exception - Fail silently
                return null;
            }
        }

        /// <summary>
        /// Returns true if the
        /// </summary>
        /// <param name="type"></param>
        /// <param name="includeSubTypes"></param>
        /// <returns></returns>
        public bool HasAttribute(Type type, bool includeSubTypes = false)
        {
            foreach(object attribute in typeAttributes)
            {
                if (includeSubTypes == false)
                {
                    // Check for matching type
                    if (attribute.GetType() == type)
                        return true;
                }
                else
                {
                    // Check for matching type or sub type
                    if (type.IsAssignableFrom(attribute.GetType()) == true)
                        return true;
                }
            }
            return false;
        }

        public bool HasAttribute<T>(bool includeSubTypes = false) where T : Attribute
        {
            return HasAttribute(typeof(T), includeSubTypes);
        }

        public object GetAttribute(Type type, bool includeSubTypes = false)
        {
            foreach(object attribute in typeAttributes)
            {
                if (includeSubTypes == false)
                {
                    // Get matching attribute
                    if (attribute.GetType() == type)
                        return attribute;
                }
                else
                {
                    // Check for matching type or sub type
                    if (type.IsAssignableFrom(attribute.GetType()) == true)
                        return attribute;
                }
            }
            return null;
        }

        public T GetAttribute<T>(bool includeSubTypes = false) where T : Attribute
        {
            return GetAttribute(typeof(T), includeSubTypes) as T;
        }

        public object[] GetAttributes(Type type, bool includeSubTypes = false)
        {
            matchedAttributes.Clear();

            foreach (object attribute in typeAttributes)
            {
                if (includeSubTypes == false)
                {
                    // Get matching attribute
                    if (attribute.GetType() == type)
                        matchedAttributes.Add(attribute);
                }
                else
                {
                    // Check for matching type or sub type
                    if (type.IsAssignableFrom(attribute.GetType()) == true)
                        matchedAttributes.Add(attribute);
                }
            }
            return matchedAttributes.ToArray();
        }

        public T[] GetAttributes<T>(bool includeSubTypes = false) where T : Attribute
        {
            return GetAttributes(typeof(T), includeSubTypes) as T[];
        }

        /// <summary>
        /// Override ToString implementation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("ScriptType({0})", rawType.Name);
        }

        private void GenerateAttributeInformation()
        {
            if(typeAttributes == null)
            {
                // Create the set
                typeAttributes = new HashSet<object>();

                // Get atrributes
                object[] attributes = rawType.GetCustomAttributes(false);

                // Add all to set
                foreach (object attrib in attributes)
                    typeAttributes.Add(attrib);
            }
        }

        /// <summary>
        /// Attempt to find a nested <see cref="ScriptType"/> of this type with the specified name.
        /// </summary>
        /// <param name="nestedTypeName">The name of the nested type to search for</param>
        /// <returns>A <see cref="ScriptType"/> instance representing the matching nested type if found or null</returns>
        public ScriptType FindNestedType(string nestedTypeName)
        {
            return Array.Find(nestedTypes, t => t.Name == nestedTypeName);
        }

        /// <summary>
        /// Attempt to find a nested <see cref="ScriptType"/> of this type with the specified full name.
        /// Note that the full name is the reflection path to the nested type including parent type names separated by the '+' character as per reflection specification. For example 'MyNamespace.MyParentType+MyNestedType'.
        /// </summary>
        /// <param name="nestedTypeFullName">The full name path of the nested type</param>
        /// <returns>An <see cref="ScriptType"/> instance representing the matching nested type if found or null</returns>
        public ScriptType FindNestedTypeFullName(string nestedTypeFullName)
        {
            return Array.Find(nestedTypes, t => t.FullName == nestedTypeFullName);
        }

        /// <summary>
        /// Attempt to find a type with the specified name in the specified <see cref="ScriptDomain"/>.
        /// </summary>
        /// <param name="typeName">The name of the type to find</param>
        /// <param name="searchDomain">The domain to search or null if the active domain should be used</param>
        /// <returns>A <see cref="ScriptType"/> matching the specified type name or null if the type was not found</returns>
        public static ScriptType FindType(string typeName, ScriptDomain searchDomain = null)
        {
            // Try to resolve domain
            if (ResolveSearchDomain(ref searchDomain) == false)
                return null;

            // Search all assemblies
            foreach (ScriptAssembly assembly in searchDomain.Assemblies)
            {
                // Try to find type
                ScriptType type = assembly.FindType(typeName);

                // Check for success
                if (type != null)
                    return type;
            }

            // Type not found
            return null;
        }

        /// <summary>
        /// Attempt to find a type with the specified name in the specified <see cref="ScriptDomain"/> that inherits from the specified base type.
        /// </summary>
        /// <param name="typeName">The name of the type to find</param>
        /// <param name="subType">The base type that the type must inherit from</param>
        /// <param name="searchDomain">The domain to search or null if the active domain should be used</param>
        /// <returns>A <see cref="ScriptType"/> matching the specified type name and inheritance constraints or null if the type was not found</returns>
        public static ScriptType FindSubTypeOf(string typeName, Type subType, ScriptDomain searchDomain = null)
        {
            // Try to resolve domain
            if (ResolveSearchDomain(ref searchDomain) == false)
                return null;

            // Search all assemblies
            foreach (ScriptAssembly assembly in searchDomain.Assemblies)
            {
                // Try to find type
                ScriptType type = assembly.FindSubTypeOf(subType, typeName);

                // Check for success
                if (type != null)
                    return type;
            }

            // Type not found
            return null;
        }

        /// <summary>
        /// Attempt to find a type with the specified name in the specified <see cref="ScriptDomain"/> that inherits from the specified generic base type.
        /// </summary>
        /// <typeparam name="T">The generic type that the type must inherit from</typeparam>
        /// <param name="typeName">The name of the type to find</param>
        /// <param name="searchDomain">The domain to search or null if the active domain should be used</param>
        /// <returns>A <see cref="ScriptType"/> matching the specified type name and inheritance constranints or null if the type was not found</returns>
        public static ScriptType FindSubTypeOf<T>(string typeName, ScriptDomain searchDomain = null)
        {
            // Try to resolve domain
            if (ResolveSearchDomain(ref searchDomain) == false)
                return null;

            // Search all assemblies
            foreach (ScriptAssembly assembly in searchDomain.Assemblies)
            {
                // Try to find type
                ScriptType type = assembly.FindSubTypeOf<T>(typeName);

                // Check for success
                if (type != null)
                    return type;
            }

            // Type not found
            return null;
        }

        /// <summary>
        /// Attempt to find the first type that inherits from the specfieid sub type.
        /// </summary>
        /// <param name="subType">The base type that the type should inherit from</param>
        /// <param name="searchDomain">The domain to search or null if the active domain should be used</param>
        /// <returns>A <see cref="ScriptType"/> matching the specified inheritance constraints or null if the type was not found</returns>
        public static ScriptType FindSubTypeOf(Type subType, ScriptDomain searchDomain = null)
        {
            // Try to resolve domain
            if (ResolveSearchDomain(ref searchDomain) == false)
                return null;

            // Search all assemblies
            foreach (ScriptAssembly assembly in searchDomain.Assemblies)
            {
                // Try to find type
                ScriptType type = assembly.FindSubTypeOf(subType);

                // Check for success
                if (type != null)
                    return type;
            }

            // Type not found
            return null;
        }

        /// <summary>
        /// Attempt to find the first type that inherits from the specified generic sub type.
        /// </summary>
        /// <typeparam name="T">The generic type that the type must inherit from</typeparam>
        /// <param name="searchDomain">The domain to search or null if the active domain should be used</param>
        /// <returns>A <see cref="ScriptType"/> matching the specified inheritance constraints or null if the type was not found</returns>
        public static ScriptType FindSubTypeOf<T>(ScriptDomain searchDomain = null)
        {
            // Try to resolve domain
            if (ResolveSearchDomain(ref searchDomain) == false)
                return null;

            // Search all assemblies
            foreach (ScriptAssembly assembly in searchDomain.Assemblies)
            {
                // Try to find type
                ScriptType type = assembly.FindSubTypeOf<T>();

                // Check for success
                if (type != null)
                    return type;
            }

            // Type not found
            return null;
        }

        /// <summary>
        /// Attempt to find all types that inherit from the specified sub type.
        /// </summary>
        /// <param name="subType">The base type that the types must inherit from</param>
        /// <param name="includeNonPublic">Should non-public types be included in the search</param>
        /// <param name="searchDomain">The domain to search or null if the active domain should be used</param>
        /// <returns>An array of <see cref="ScriptType"/> that inherit from the specified base type or an empty array if no types were found</returns>
        public static ScriptType[] FindAllSubTypesOf(Type subType, bool includeNonPublic = true, ScriptDomain searchDomain = null)
        {
            // Try to resolve domain
            if (ResolveSearchDomain(ref searchDomain) == false)
                return new ScriptType[0];

            // Use shared types list
            matchedTypes.Clear();

            // Search all assemblies
            foreach (ScriptAssembly assembly in searchDomain.Assemblies)
            {
                // Try to find type
                ScriptType[] types = assembly.FindAllSubTypesOf(subType, includeNonPublic);

                // Add to result
                matchedTypes.AddRange(types);
            }

            // Get types array
            return matchedTypes.ToArray();
        }

        /// <summary>
        /// Attempt to find all the types that inherit from the specified generic sub type.
        /// </summary>
        /// <typeparam name="T">The generic base type that the types must inherit from</typeparam>
        /// <param name="includeNonPublic">Should non-public types be included in the search</param>
        /// <param name="searchDomain">The domain to search or null if the active domain should be used</param>
        /// <returns>An array of <see cref="ScriptType"/> that inherit from the specified base type or an empty array if no types were found</returns>
        public static ScriptType[] FindAllSubTypesOf<T>(bool includeNonPublic = true, ScriptDomain searchDomain = null)
        {
            // Try to resolve domain
            if (ResolveSearchDomain(ref searchDomain) == false)
                return new ScriptType[0];

            // Use shared types list
            matchedTypes.Clear();

            // Search all assemblies
            foreach (ScriptAssembly assembly in searchDomain.Assemblies)
            {
                // Try to find type
                ScriptType[] types = assembly.FindAllSubTypesOf<T>(includeNonPublic);

                // Add to result
                matchedTypes.AddRange(types);
            }

            // Get types array
            return matchedTypes.ToArray();
        }

        /// <summary>
        /// Attempt to find all types in the specified domain.
        /// </summary>
        /// <param name="includeNonPublic">Should non-public types be included in the search</param>
        /// <param name="searchDomain">The domai to search or null if the active domain should be used</param>
        /// <returns>An array of <see cref="ScriptType"/> that exist in the specified domain or an empty array if no types were found</returns>
        public static ScriptType[] FindAllTypes(bool includeNonPublic = true, ScriptDomain searchDomain = null)
        {
            // Try to resolve domain
            if (ResolveSearchDomain(ref searchDomain) == false)
                return new ScriptType[0];

            // Use shared types list
            matchedTypes.Clear();

            // Search all assemblies
            foreach(ScriptAssembly assembly in searchDomain.Assemblies)
            {
                // Find types
                ScriptType[] types = assembly.FindAllTypes(includeNonPublic);

                // Add to result
                matchedTypes.AddRange(types);
            }

            return matchedTypes.ToArray();
        }

        /// <summary>
        /// Attempts to find all types in the specified domain that inherit from <see cref="UnityEngine.Object"/>.
        /// </summary>
        /// <param name="includeNonPublic">Should non-public types be included in the search</param>
        /// <param name="searchDomain">The domain to search or null if the active domain should be used</param>
        /// <returns>An array of <see cref="ScriptType"/> that inherit from <see cref="UnityEngine.Object"/> or an empty array if no types were found</returns>
        public static ScriptType[] FindAllUnityTypes(bool includeNonPublic = true, ScriptDomain searchDomain = null)
        {
            // Try to resolve domain
            if (ResolveSearchDomain(ref searchDomain) == false)
                return new ScriptType[0];

            // Use shared types list
            matchedTypes.Clear();

            // Search all assemblies
            foreach (ScriptAssembly assembly in searchDomain.Assemblies)
            {
                // Find types
                ScriptType[] types = assembly.FindAllUnityTypes(includeNonPublic);

                // Add to result
                matchedTypes.AddRange(types);
            }

            return matchedTypes.ToArray();
        }

        /// <summary>
        /// Attempts to find all types in the specified domain that inherit from <see cref="UnityEngine.MonoBehaviour"/>.
        /// </summary>
        /// <param name="includeNonPublic">Should non-public types be included in the search</param>
        /// <param name="searchDomain">The domain to search or null if the active domain should be used</param>
        /// <returns>An array of <see cref="ScriptType"/> that inherit from <see cref="UnityEngine.MonoBehaviour"/> or an empty array if no types were found</returns>
        public static ScriptType[] FindAllMonoBehaviourTypes(bool includeNonPublic = true, ScriptDomain searchDomain = null)
        {
            // Try to resolve domain
            if (ResolveSearchDomain(ref searchDomain) == false)
                return new ScriptType[0];

            // Use shared types list
            matchedTypes.Clear();

            // Search all assemblies
            foreach (ScriptAssembly assembly in searchDomain.Assemblies)
            {
                // Find types
                ScriptType[] types = assembly.FindAllMonoBehaviourTypes(includeNonPublic);

                // Add to result
                matchedTypes.AddRange(types);
            }

            return matchedTypes.ToArray();
        }

        /// <summary>
        /// Attempts to find all types in the specified domain that inherit from <see cref="UnityEngine.ScriptableObject"/>.
        /// </summary>
        /// <param name="includeNonPublic">Should non-public types be included in the search</param>
        /// <param name="searchDomain">The domain to search or null if the active domain should be used</param>
        /// <returns>An array of <see cref="ScriptType"/> that inherit from <see cref="UnityEngine.ScriptableObject"/> or an empty array if no types were found</returns>
        public static ScriptType[] FindAllScriptableObjectTypes(bool includeNonPublic = true, ScriptDomain searchDomain = null)
        {
            // Try to resolve domain
            if (ResolveSearchDomain(ref searchDomain) == false)
                return new ScriptType[0];

            // Use shared types list
            matchedTypes.Clear();

            // Search all assemblies
            foreach (ScriptAssembly assembly in searchDomain.Assemblies)
            {
                // Find types
                ScriptType[] types = assembly.FindAllScriptableObjectTypes(includeNonPublic);

                // Add to result
                matchedTypes.AddRange(types);
            }

            return matchedTypes.ToArray();
        }

        /// <summary>
        /// Enumerates all types that inherit from the specified sub type.
        /// </summary>
        /// <param name="subType">The base type that the types must inherit from</param>
        /// <param name="includeNonPublic">Should non-public types be included in the search</param>
        /// <param name="searchDomain">The domain to search or null if the active domain should be used</param>
        /// <returns>Enumerable of matching results</returns>
        public static IEnumerable<ScriptType> EnumerateAllSubTypesOf(Type subType, bool includeNonPublic = true, ScriptDomain searchDomain = null)
        {
            // Try to resolve domain
            if (ResolveSearchDomain(ref searchDomain) == false)
                yield break;
            
            // Search all assemblies
            foreach (ScriptAssembly assembly in searchDomain.Assemblies)
            {
                // Try to find type
                foreach(ScriptType type in assembly.EnumerateAllSubTypesOf(subType, includeNonPublic))
                {
                    // Return the type
                    yield return type;
                }
            }
        }

        /// <summary>
        /// Enumerate all types that inherit from the specified generic sub type.
        /// </summary>
        /// <typeparam name="T">The generic base type that the types must inherit from</typeparam>
        /// <param name="includeNonPublic">Should non-public types be included in the search</param>
        /// <param name="searchDomain">The domain to search or null if the active domain should be used</param>
        /// <returns>Enumerable of matching results</returns>
        public static IEnumerable<ScriptType> EnumerateAllSubTypesOf<T>(bool includeNonPublic = true, ScriptDomain searchDomain = null)
        {
            // Try to resolve domain
            if (ResolveSearchDomain(ref searchDomain) == false)
                yield break;

            // Search all assemblies
            foreach (ScriptAssembly assembly in searchDomain.Assemblies)
            {
                // Try to find type
                foreach (ScriptType type in assembly.EnumerateAllSubTypesOf<T>(includeNonPublic))
                {
                    // Return the type
                    yield return type;
                }
            }
        }

        /// <summary>
        /// Enumerate all types in the specified domain.
        /// </summary>
        /// <param name="includeNonPublic">Should non-public types be included in the search</param>
        /// <param name="searchDomain">The domain to search or null if the active domain should be used</param>
        /// <returns>Enumerable of matching results</returns>
        public static IEnumerable<ScriptType> EnumerateAllTypes(bool includeNonPublic = true, ScriptDomain searchDomain = null)
        {
            // Try to resolve domain
            if (ResolveSearchDomain(ref searchDomain) == false)
                yield break;

            // Search all assemblies
            foreach (ScriptAssembly assembly in searchDomain.Assemblies)
            {
                // Try to find type
                foreach (ScriptType type in assembly.EnumerateAllTypes(includeNonPublic))
                {
                    // Return the type
                    yield return type;
                }
            }
        }

        /// <summary>
        /// Enumerate all types in the specified domain that inherit from <see cref="UnityEngine.Object"/>.
        /// </summary>
        /// <param name="includeNonPublic">Should non-public types be include in the search</param>
        /// <param name="searchDomain">The domain to search or null if the active domain should be used</param>
        /// <returns>Enumerable of matching results</returns>
        public static IEnumerable<ScriptType> EnumerateAllUnityTypes(bool includeNonPublic = true, ScriptDomain searchDomain = null)
        {
            // Try to resolve domain
            if (ResolveSearchDomain(ref searchDomain) == false)
                yield break;

            // Search all assemblies
            foreach(ScriptAssembly assembly in searchDomain.Assemblies)
            {
                // Try to find type
                foreach(ScriptType type in assembly.EnumerateAllUnityTypes(includeNonPublic))
                {
                    // Return the type
                    yield return type;
                }
            }
        }

        /// <summary>
        /// Enumerate all types in the specified domain that inherit from <see cref="UnityEngine.MonoBehaviour"/>.
        /// </summary>
        /// <param name="includeNonPublic">Should non-public types be included in the search</param>
        /// <param name="searchDomain">The domain to search or null if the active domain should be used</param>
        /// <returns>Enumerable of matching results</returns>
        public static IEnumerable<ScriptType> EnumerateAllMonoBehaviourTypes(bool includeNonPublic = true, ScriptDomain searchDomain = null)
        {
            // Try to resolve domain
            if (ResolveSearchDomain(ref searchDomain) == false)
                yield break;

            // Search all assemblies
            foreach (ScriptAssembly assembly in searchDomain.Assemblies)
            {
                // Try to find type
                foreach (ScriptType type in assembly.EnumerateAllMonoBehaviourTypes(includeNonPublic))
                {
                    // Return the type
                    yield return type;
                }
            }
        }

        /// <summary>
        /// Enumerate all types in the specified domain that inherit from <see cref="UnityEngine.ScriptableObject"/>.
        /// </summary>
        /// <param name="includeNonPublic">Should non-public types be include in the search</param>
        /// <param name="searchDomain">The domain to search or null if the active domain should be used</param>
        /// <returns>Enumerable of matching results</returns>
        public static IEnumerable<ScriptType> EnumerateAllScriptableObjectTypes(bool includeNonPublic = true, ScriptDomain searchDomain = null)
        {
            // Try to resolve domain
            if (ResolveSearchDomain(ref searchDomain) == false)
                yield break;

            // Search all assemblies
            foreach (ScriptAssembly assembly in searchDomain.Assemblies)
            {
                // Try to find type
                foreach (ScriptType type in assembly.EnumerateAllScriptableObjectTypes(includeNonPublic))
                {
                    // Return the type
                    yield return type;
                }
            }
        }

        private static bool ResolveSearchDomain(ref ScriptDomain searchDomain)
        {
            // Check for specified domain
            if (searchDomain == null)
            {
                // Get the active domain
                searchDomain = ScriptDomain.Active;

                // No domain found to search
                if (searchDomain == null)
                    return false;
            }
            return true;
        }
    }
}