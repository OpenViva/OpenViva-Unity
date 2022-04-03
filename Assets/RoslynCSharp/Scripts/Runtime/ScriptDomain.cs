using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using System.Security;
using System.Collections.Generic;

using RoslynCSharp.Compiler;
using Trivial.CodeSecurity;

namespace RoslynCSharp
{
    /// <summary>
    /// The security mode used when loading and compiling assemblies.
    /// </summary>
    public enum ScriptSecurityMode
    {
        /// <summary>
        /// Use the security mode from the Roslyn C# settings window.
        /// </summary>
        UseSettings,
        /// <summary>
        /// Security verification will be skipped.
        /// </summary>
        EnsureLoad,
        /// <summary>
        /// Secutiry verification will be used.
        /// </summary>
        EnsureSecurity,
    }

    /// <summary>
    /// A <see cref="ScriptDomain"/> acts as a container for all code that is loaded dynamically at runtime.
    /// The main responsiblility of the domin is to separate pre-compiled game code from runtime-loaded code. 
    /// As a result, you will only be able to access types from the domain that were loaded at runtime.
    /// Any pre-compiled game code will be ignored.
    /// Any assemblies or scripts that are loaded into the domain at runtime will remain until the application exits so you should be careful to avoid loading too many assemblies.
    /// You would typically load user code at statup in a 'Load' method which would then exist and execute until the game exits.
    /// Multiple domain instances may be created but you should note that all runtime code will be loaded into the current application domain. The <see cref="ScriptDomain"/> simply masks the types that are visible.
    /// </summary>
    public class ScriptDomain : IDisposable
    {
        // Private
        private static List<ScriptDomain> activeDomains = new List<ScriptDomain>();
        private static List<ScriptAssembly> matchedAssemblies = new List<ScriptAssembly>();
        private static ScriptDomain active = null;

        private string name = null;
        private AppDomain sandbox = null;
        private List<ScriptAssembly> loadedAssemblies = new List<ScriptAssembly>();
        private RoslynCSharpCompiler sharedCompiler = null;
        private CodeSecurityReport securityResult = null;
        private CompilationResult compileResult = null;

        // Properties
        /// <summary>
        /// Get the active <see cref="ScriptDomain"/>.
        /// By default the last created domain will be active but you can also make a specific domain active using <see cref="MakeDomainActive(ScriptDomain)"/>.
        /// </summary>
        public static ScriptDomain Active
        {
            get { return active; }
        }

        /// <summary>
        /// Get the name of the domain.
        /// </summary>
        public string Name
        {
            get
            {
                // Check for disposed
                CheckDisposed();
                return name;
            }
        }

        /// <summary>
        /// Get the app domain that this <see cref="ScriptDomain"/> manages.
        /// </summary>
        public AppDomain SandboxDomain
        {
            get
            {
                // Check for disposed
                CheckDisposed();
                return sandbox;
            }
        }

        /// <summary>
        /// Get all assemblies loaded into this domain.
        /// </summary>
        public ScriptAssembly[] Assemblies
        {
            get
            {
                // Check for disposed
                CheckDisposed();

                lock (this)
                {
                    return loadedAssemblies.ToArray();
                }
            }
        }

        /// <summary>
        /// Get all assemblies loaded into this domain that have been compiled at runtime using the Roslyn runtime compiler service.
        /// </summary>
        public ScriptAssembly[] CompiledAssemblies
        {
            get
            {
                // Check for disposed
                CheckDisposed();

                // Use shared list for result
                matchedAssemblies.Clear();

                lock (this)
                {
                    // Check all assemblies
                    foreach (ScriptAssembly assembly in loadedAssemblies)
                    {
                        // Add to result list
                        if (assembly.IsRuntimeCompiled == true)
                            matchedAssemblies.Add(assembly);
                    }
                }

                // Get as array
                return matchedAssemblies.ToArray();
            }
        }

        /// <summary>
        /// Enumerate all assemblies loaded into this domain.
        /// </summary>
        public IEnumerable<ScriptAssembly> EnumerateAssemblies
        {
            get
            {
                // Check for disposed
                CheckDisposed();
                lock (this)
                {
                    return loadedAssemblies;
                }
            }
        }

        /// <summary>
        /// Enumerate all assemblies loaded into this domain that have been compiled at runtime using the Roslyn runtime compiler service.
        /// </summary>
        public IEnumerable<ScriptAssembly> EnumerateCompiledAssemblies
        {
            get
            {
                // Check for disposed
                CheckDisposed();
                lock (this)
                {
                    foreach (ScriptAssembly assembly in loadedAssemblies)
                    {
                        if (assembly.IsRuntimeCompiled == true)
                            yield return assembly;
                    }
                }
            }
        }

        /// <summary>
        /// Get the Roslyn runtime compiler service associated with this domain.
        /// This value will be null if the compiler service has not been initialized.
        /// </summary>
        public RoslynCSharpCompiler RoslynCompilerService
        {
            get
            {
                // Check for disposed
                CheckDisposed();
                return sharedCompiler;
            }
        }

        /// <summary>
        /// Get the last compilation report as a result of compiling an assembly.
        /// </summary>
        public CompilationResult CompileResult
        {
            get
            {
                // Check for disposed
                CheckDisposed();
                return compileResult;
            }
        }

        /// <summary>
        /// Get the last security report as a result of loading or compiling an assembly.
        /// </summary>
        public CodeSecurityReport SecurityResult
        {
            get { return securityResult; }
        }

        /// <summary>
        /// Returns true if the Roslyn compiler service is initialized and ready to recieve compile requests.
        /// </summary>
        public bool IsCompilerServiceInitialized
        {
            get
            {
                // Check for disposed
                CheckDisposed();
                return sharedCompiler != null;
            }
        }

        /// <summary>
        /// Has this domain been disposed.
        /// </summary>
        public bool IsDisposed
        {
            get { return sandbox == null; }
        }

        // Constructor
        private ScriptDomain(string name, AppDomain sandboxDomain = null)
        {
            // Store the name
            this.name = name;

            // Store the domain
            sandbox = sandboxDomain;

            // Revert to current domain
            if (sandboxDomain == null)
            {
                // Create the app domain
                this.sandbox = AppDomain.CurrentDomain;
            }

            // Add active domain
            activeDomains.Add(this);
        }

        // Methods
        #region AssemblyLoad
        /// <summary>
        /// Attempts to load a managed assembly from the specified resources path into the sandbox app domain.
        /// The target asset must be a <see cref="TextAsset"/> in order to be loaded successfully. 
        /// Use <see cref="SecurityResult"/> to get the output from the code validation request.
        /// </summary>
        /// <param name="resourcePath">The file name of path relative to the 'Resources' folder without the file extension</param>
        /// <param name="securityMode">The security mode which determines whether code validation will run</param>
        /// <returns>An instance of <see cref="ScriptAssembly"/> representing the loaded assembly or null if an error occurs</returns>
        /// <exception cref="SecurityException">The assembly breaches the imposed security restrictions</exception>
        public ScriptAssembly LoadAssemblyFromResources(string resourcePath, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings)
        {
            // Check for disposed
            CheckDisposed();

            // Try to load resource
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);

            // Check for error
            if (asset == null)
                throw new DllNotFoundException(string.Format("Failed to load dll from resources path '{0}'", resourcePath));
            
            // Get the asset bytes and call through
            return LoadAssembly(asset.bytes, securityMode);
        }

        /// <summary>
        /// Attempts to load the specified managed assembly into the sandbox app domain.
        /// Use <see cref="SecurityResult"/> to get the output from the code validation request.
        /// </summary>
        /// <param name="fullPath">The full path the the .dll file</param>
        /// <param name="securityMode">The security mode which determines whether code validation will run</param>
        /// <returns>An instance of <see cref="ScriptAssembly"/> representing the loaded assembly or null if an error occurs</returns>
        public ScriptAssembly LoadAssembly(string fullPath, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings)
        {
            // Check for disposed
            CheckDisposed();

            // Create an assembly name object
            AssemblyName name = AssemblyName.GetAssemblyName(fullPath);// new AssemblyName();
            //name.CodeBase = fullPath;

            // Load the assembly
            Assembly assembly = sandbox.Load(name);

            // Create script assembly
            return RegisterAssembly(assembly, fullPath, null, securityMode, false);
        }

        /// <summary>
        /// Attempts to load the specified managed assembly into the sandbox app domain.
        /// Use <see cref="SecurityResult"/> to get the output from the code validation request.
        /// </summary>
        /// <param name="name">The <see cref="AssemblyName"/> representing the assembly to load</param>
        /// <param name="securityMode">The security mode which determines whether code validation will run</param>
        /// <returns>An instance of <see cref="ScriptAssembly"/> representing the loaded assembly or null if an error occurs</returns>
        public ScriptAssembly LoadAssembly(AssemblyName name, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings)
        {
            // Check for disposed
            CheckDisposed();

            // Load the assembly
            Assembly assembly = sandbox.Load(name);

            // Create script assembly
            return RegisterAssembly(assembly, assembly.Location, null, securityMode, false);
        }

        /// <summary>
        /// Attempts to load a managed assembly from the specified raw bytes.
        /// Use <see cref="SecurityResult"/> to get the output from the code validation request.
        /// </summary>
        /// <param name="assemblyBytes">The raw data representing the file structure of the managed assembly, The result of <see cref="File.ReadAllBytes(string)"/> for example.</param>
        /// <returns>An instance of <see cref="ScriptAssembly"/> representing the loaded assembly or null if an error occurs</returns>
        /// <param name="securityMode">The security mode which determines whether code validation will run</param>
        public ScriptAssembly LoadAssembly(byte[] assemblyBytes, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings)
        {
            // Check for disposed
            CheckDisposed();

            // Load the assembly
            Assembly assembly = sandbox.Load(assemblyBytes);

            // Create script assembly
            return RegisterAssembly(assembly, null, assemblyBytes, securityMode, false);
        }

        /// <summary>
        /// Attempts to load a managed assembly from the specified filepath asynchronously.
        /// Use <see cref="SecurityResult"/> of <see cref="AsyncLoadOperation.LoadDomain"/> to get the output from the code validation request.
        /// </summary>
        /// <param name="fullPath">The filepath to the managed assembly</param>
        /// <param name="securityMode">The security mode which determines whether code validation will run</param>
        /// <returns>An awaitable async operation object that contains state information for the load request</returns>
        public AsyncLoadOperation LoadAssemblyAsync(string fullPath, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings)
        {
            // Check for disposed
            CheckDisposed();

            return new AsyncLoadOperation(this, fullPath, securityMode);
        }

        /// <summary>
        /// Attempts to load a managed assembly with the specified name asynchronously.
        /// Use <see cref="SecurityResult"/> of <see cref="AsyncLoadOperation.LoadDomain"/> to get the output from the code validation request.
        /// </summary>
        /// <param name="name">The name of the assembly to load</param>
        /// <param name="securityMode">The security mode which determines whether code validation will run</param>
        /// <returns>An awaitable async operation object that contains state information for the load request</returns>
        public AsyncLoadOperation LoadAssemblyAsync(AssemblyName name, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings)
        {
            // Check for disposed
            CheckDisposed();

            return new AsyncLoadOperation(this, name, securityMode);
        }

        /// <summary>
        /// Attempts to load a managed assembly from the specified raw bytes asynchronously.
        /// Use <see cref="SecurityResult"/> of <see cref="AsyncLoadOperation.LoadDomain"/> to get the output from the code validation request.
        /// </summary>
        /// <param name="assemblyBytes">A byte array containing the managed assembly imagae data</param>
        /// <param name="securityMode">The security mode which determines whether code validation will run</param>
        /// <returns>An awaitable async operation object that contains state information for the load request</returns>
        public AsyncLoadOperation LoadAssemblyAsync(byte[] assemblyBytes, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings)
        {
            // Check for disposed
            CheckDisposed();

            return new AsyncLoadOperation(this, assemblyBytes, securityMode);
        }

        /// <summary>
        /// Attempts to load the managed assembly at the specified location.
        /// Any exceptions throw while loading will be caught.
        /// </summary>
        /// <param name="fullPath">The full path to the .dll file</param>
        /// <param name="result">An instance of <see cref="ScriptAssembly"/> representing the loaded assembly or null if the load failed</param>
        /// <param name="securityMode">The security mode which determines whether code validation will run</param>
        /// <returns>True if the assembly was loaded successfully or false if an error occurred</returns>
        public bool TryLoadAssembly(string fullPath, out ScriptAssembly result, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings)
        {
            // Even though this method is safe we cannot allow access to a disposed domain
            CheckDisposed();

            try
            {
                // Call through
                result = LoadAssembly(fullPath, securityMode);
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Attempts to load a managed assembly with the specified name.
        /// Any exceptions thrown while loading will be caught.
        /// </summary>
        /// <param name="name">The <see cref="AssemblyName"/> of the assembly to load</param>
        /// <param name="result">An instance of <see cref="ScriptAssembly"/> representing the loaded assembly or null if the load failed</param>
        /// <param name="securityMode">The security mode which determines whether code validation will run</param>
        /// <returns>True if the assembly was loaded successfully or false if an error occurred</returns>
        public bool TryLoadAssembly(AssemblyName name, out ScriptAssembly result, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings)
        {
            // Even though this method is safe we cannot allow access to a disposed domain
            CheckDisposed();

            try
            {
                // Call through
                result = LoadAssembly(name, securityMode);
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Attempts to load a managed assembly from the raw assembly data.
        /// Any exceptions thrown while loading will be caught.
        /// </summary>
        /// <param name="data">The raw data representing the file structure of the managed assembly, The result of <see cref="File.ReadAllBytes(string)"/> for example.</param>
        /// <param name="result">An instance of <see cref="ScriptAssembly"/> representing the loaded assembly or null if the load failed</param>
        /// <param name="securityMode">The security mode which determines whether code validation will run</param>
        /// <returns>True if the assembly was loaded successfully or false if an error occured</returns>
        public bool TryLoadAssembly(byte[] data, out ScriptAssembly result, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings)
        {
            // Even though this method is safe we cannot allow access to a disposed domain
            CheckDisposed();

            try
            {
                // Call through
                result = LoadAssembly(data, securityMode);
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }
        #endregion

        #region AssemblyCompile
        /// <summary>
        /// Compile and load the speciied C# source code string.
        /// Use <see cref="CompileResult"/> to get the output from the compile request.
        /// Use <see cref="SecurityResult"/> to get the output from the code validation request.
        /// This does the same as <see cref="CompileAndLoadSource(string, ScriptSecurityMode)"/> but returns the main type of the <see cref="ScriptAssembly"/> for convenience.        /// 
        /// </summary>
        /// <param name="cSharpSource">The string containing C# source code</param>
        /// <param name="securityMode">The code validation used to verify the code</param>
        /// <returns>The main type of the compiled assembly or null if the compile failed, security validation failed or there was main type</returns>
        public ScriptType CompileAndLoadMainSource(string cSharpSource, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings, IMetadataReferenceProvider[] additionalReferenceAssemblies = null)
        {
            // Send compile request
            ScriptAssembly assembly = CompileAndLoadSource(cSharpSource, securityMode, additionalReferenceAssemblies);

            // Try to get main type
            if (assembly != null && assembly.MainType != null)
                return assembly.MainType;

            return null;
        }

        /// <summary>
        /// Compile and load the specified C# source file.
        /// Use <see cref="CompileResult"/> to get the output from the compile request.
        /// Use <see cref="SecurityResult"/> to get the output from the code validation request.
        /// This does the same as <see cref="CompileAndLoadFileAsync(string, ScriptSecurityMode)"/> but returns the main type of the <see cref="ScriptAssembly"/> for convenience.
        /// </summary>
        /// <param name="cSharpFile">The filepath to a file containing C# code</param>
        /// <param name="securityMode">The code validation used to verify the code</param>
        /// <returns>The main type of the compiled assembly or null if the compile failed, security validation failed or there was no main type</returns>
        public ScriptType CompileAndLoadMainFile(string cSharpFile, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings, IMetadataReferenceProvider[] additionalReferenceAssemblies = null)
        {
            // Send compile request
            ScriptAssembly assembly = CompileAndLoadFile(cSharpFile, securityMode, additionalReferenceAssemblies);

            // Try to get main type
            if (assembly != null && assembly.MainType != null)
                return assembly.MainType;

            return null;
        }

        /// <summary>
        /// Compile and load the specified C# source code string.
        /// Use <see cref="CompileResult"/> to get the output from the compile request.
        /// Use <see cref="SecurityResult"/> to get the output from the code validation request.
        /// </summary>
        /// <param name="cSharpSource">The string containing C# source code</param>
        /// <param name="securityMode">The code validation used to verify the code</param>
        /// <returns>The compiled and loaded assembly or null if the compile or security verification failed</returns>
        public ScriptAssembly CompileAndLoadSource(string cSharpSource, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings, IMetadataReferenceProvider[] additionalReferenceAssemblies = null)
        {
            // Make sure the compiler is initialized and the domain is valid
            CheckDisposed();
            CheckCompiler();

            lock(this)
            {
                // Compile from source
                compileResult = sharedCompiler.CompileFromSource(cSharpSource, additionalReferenceAssemblies);

                // Log to console
                LogCompilerOutputToConsole();

                // Security check
                return RegisterAssembly(compileResult.OutputAssembly, compileResult.OutputFile, compileResult.OutputAssemblyImage, securityMode, true, compileResult);
            }
        }

        /// <summary>
        /// Compile and load the specified C# source file.
        /// Use <see cref="CompileResult"/> to get the output from the compile request.
        /// Use <see cref="SecurityResult"/> to get the output from the code validation request.
        /// </summary>
        /// <param name="cSharpFile">The filepath to a file containing C# code</param>
        /// <param name="securityMode">The code validation used to verify the code</param>
        /// <returns>The compiled and loaded assembly or null if the compile or security verification failed</returns>
        public ScriptAssembly CompileAndLoadFile(string cSharpFile, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings, IMetadataReferenceProvider[] additionalReferenceAssemblies = null)
        {
            // Make sure the compiler is initialized and the domain is valid
            CheckDisposed();
            CheckCompiler();

            lock (this)
            {
                // Compile from file
                compileResult = sharedCompiler.CompileFromFile(cSharpFile, additionalReferenceAssemblies);

                // Log to console
                LogCompilerOutputToConsole();

                // Security check
                return RegisterAssembly(compileResult.OutputAssembly, compileResult.OutputFile, compileResult.OutputAssemblyImage, securityMode, true, compileResult);
            }
        }

        /// <summary>
        /// Compile and load the specified C# source code strings.
        /// Use <see cref="CompileResult"/> to get the output from the compile request.
        /// Use <see cref="SecurityResult"/> to get the output from the code validation request.
        /// </summary>
        /// <param name="cSharpSources">An array of C# source code strings</param>
        /// <param name="securityMode">The code validation used to verify the code</param>
        /// <returns>The compiled and loaded assembly or null if the compile or security verification failed</returns>
        public ScriptAssembly CompileAndLoadSources(string[] cSharpSources, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings, IMetadataReferenceProvider[] additionalReferenceAssemblies = null)
        {
            // Make sure the compiler is initialized and the domain is valid
            CheckDisposed();
            CheckCompiler();

            lock (this)
            {
                // Compile from source
                compileResult = sharedCompiler.CompileFromSources(cSharpSources, additionalReferenceAssemblies);

                // Log to console
                LogCompilerOutputToConsole();
                
                // Security check
                return RegisterAssembly(compileResult.OutputAssembly, compileResult.OutputFile, compileResult.OutputAssemblyImage, securityMode, true, compileResult);
            }
        }

        /// <summary>
        /// Compile and load the specified C# source files.
        /// Use <see cref="CompileResult"/> to get the output from the compile request.
        /// Use <see cref="SecurityResult"/> to get the output from the code validation request.
        /// </summary>
        /// <param name="cSharpFiles">An array of filepaths to C# source files</param>
        /// <param name="securityMode">The code validation used to verify the code</param>
        /// <returns>The compiled and loaded assembly or null if the compil or security verification failed</returns>
        public ScriptAssembly CompileAndLoadFiles(string[] cSharpFiles, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings, IMetadataReferenceProvider[] additionalReferenceAssemblies = null)
        {
            // Make sure the compiler is initialized and the domain is valid
            CheckDisposed();
            CheckCompiler();

            lock (this)
            {
                // Compile from file
                compileResult = sharedCompiler.CompileFromFiles(cSharpFiles, additionalReferenceAssemblies);

                // Log to console
                LogCompilerOutputToConsole();

                // Security check
                return RegisterAssembly(compileResult.OutputAssembly, compileResult.OutputFile, compileResult.OutputAssemblyImage, securityMode, true, compileResult);
            }
        }

        /// <summary>
        /// Compile and load the specified C# source string asynchronously.
        /// Use <see cref="CompileResult"/> of <see cref="AsyncCompileOperation.CompileDomain"/> to get the output from the compile request.
        /// Use <see cref="SecurityResult"/> of <see cref="AsyncCompileOperation.CompileDomain"/> to get the output from the code validation request.
        /// </summary>
        /// <param name="cSharpSource">The string containing C# source code</param>
        /// <param name="securityMode">The code validation used to verify the code</param>
        /// <returns>An awaitable async operation object containing state information about the compile request</returns>
        public AsyncCompileOperation CompileAndLoadSourceAsync(string cSharpSource, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings, IMetadataReferenceProvider[] additionalReferenceAssemblies = null)
        {
            // Make sure the compiler is initialized and the domain is valid
            CheckDisposed();
            CheckCompiler();

            return new AsyncCompileOperation(this, true, securityMode, new string[] { cSharpSource }, additionalReferenceAssemblies);
        }

        /// <summary>
        /// Compile and load the specified C# source file asynchronously.
        /// Use <see cref="CompileResult"/> of <see cref="AsyncCompileOperation.CompileDomain"/> to get the output from the compile request.
        /// Use <see cref="SecurityResult"/> of <see cref="AsyncCompileOperation.CompileDomain"/> to get the output from the code validation request.
        /// </summary>
        /// <param name="cSharpFile">The filepath to the C# source file</param>
        /// <param name="securityMode">The code validation used to verify the code</param>
        /// <returns>An awaitable async operation object containing state information about the compile request</returns>
        public AsyncCompileOperation CompileAndLoadFileAsync(string cSharpFile, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings, IMetadataReferenceProvider[] additionalReferenceAssemblies = null)
        {
            // Make sure the compiler is initialized and the domain is valid
            CheckDisposed();
            CheckCompiler();

            return new AsyncCompileOperation(this, false, securityMode, new string[] { cSharpFile }, additionalReferenceAssemblies);
        }

        /// <summary>
        /// Compile and load the specified C# source strings asynchronously.
        /// Use <see cref="CompileResult"/> of <see cref="AsyncCompileOperation.CompileDomain"/> to get the output from the compile request.
        /// Use <see cref="SecurityResult"/> of <see cref="AsyncCompileOperation.CompileDomain"/> to get the output from the code validation request.
        /// </summary>
        /// <param name="cSharpSources">An array of strings containgin C# source code</param>
        /// <param name="securityMode">The code validation used to verify the code</param>
        /// <returns>An awaitable async operation object containing state infomration about the compile request</returns>
        public AsyncCompileOperation CompileAndLoadSourcesAsync(string[] cSharpSources, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings, IMetadataReferenceProvider[] additionalReferenceAssemblies = null)
        {
            // Make sure the compiler is initialized and the domain is valid
            CheckDisposed();
            CheckCompiler();

            return new AsyncCompileOperation(this, true, securityMode, cSharpSources, additionalReferenceAssemblies);
        }

        /// <summary>
        /// Compile and load the specified C# source files asynchronously.
        /// Use <see cref="CompileResult"/> of <see cref="AsyncCompileOperation.CompileDomain"/> to get the output from the compile request.
        /// Use <see cref="SecurityResult"/> of <see cref="AsyncCompileOperation.CompileDomain"/> to get the output from the code validation request.
        /// </summary>
        /// <param name="cSharpFiles">An array of filepaths to C# source files</param>
        /// <param name="securityMode">The code validation used to verify the code</param>
        /// <returns>An awaitable async operation object containing state information about the compile request</returns>
        public AsyncCompileOperation CompileAndLoadFilesAsync(string[] cSharpFiles, ScriptSecurityMode securityMode = ScriptSecurityMode.UseSettings, IMetadataReferenceProvider[] additionalReferenceAssemblies = null)
        {
            // Make sure the compiler is initialized and the domain is valid
            CheckDisposed();
            CheckCompiler();

            return new AsyncCompileOperation(this, false, securityMode, cSharpFiles, additionalReferenceAssemblies);
        }
        #endregion

        /// <summary>
        /// Dispose of this domain.
        /// This will cause the target app domain to be unloaded if it is not the default app domain.
        /// The domain will be unusable after disposing.
        /// </summary>
        public void Dispose()
        {
            if (sandbox != null)
            {
                // Unload app domain
                if (sandbox.IsDefaultAppDomain() == false)
                    AppDomain.Unload(sandbox);

                // Remove from active list
                activeDomains.Remove(this);

                lock (this)
                {
                    loadedAssemblies.Clear();
                }

                sandbox = null;
                sharedCompiler = null;
                securityResult = null;
                compileResult = null;
            }
        }

        /// <summary>
        /// Initializes the Roslyn compiler service if it has not yet been initialized.
        /// </summary>
        public void InitializeCompilerService()
        {
            // Check if the compiler is initialized
            if (sharedCompiler == null)
            {
                // Create the compiler
                sharedCompiler = new RoslynCSharpCompiler(true, true, Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary, Microsoft.CodeAnalysis.CSharp.LanguageVersion.Default, sandbox);

                // Setup compiler
                ApplyCompilerServiceSettings();
            }
        }

        /// <summary>
        /// Causes the Roslyn C# settings to be loaded and applied to the Roslyn compiler service.
        /// This requires the compiler service to be initialized otherwise it will do nothing.
        /// </summary>
        public void ApplyCompilerServiceSettings()
        {
            // Check for no compiler
            if (sharedCompiler == null)
                return;

            // Load the settings
            RoslynCSharp settings = RoslynCSharp.Settings;

            // Setup compiler values
            sharedCompiler.AllowUnsafe = settings.AllowUnsafeCode;
            sharedCompiler.AllowOptimize = settings.AllowOptimizeCode;
            sharedCompiler.AllowConcurrentCompile = settings.AllowConcurrentCompile;
            sharedCompiler.GenerateInMemory = settings.GenerateInMemory;
            sharedCompiler.GenerateSymbols = settings.GenerateSymbols; // NOT SUPPORTED ON MONO
            sharedCompiler.WarningLevel = settings.WarningLevel;
            sharedCompiler.LanguageVersion = settings.LanguageVersion;
            sharedCompiler.TargetPlatform = settings.TargetPlatform;

            // Setup reference paths
            sharedCompiler.ReferenceAssemblies.Clear();
            foreach (string reference in settings.References)
                sharedCompiler.ReferenceAssemblies.Add(AssemblyReference.FromNameOrFile(reference));

            // Setup define symbols
            sharedCompiler.DefineSymbols.Clear();
            foreach (string define in settings.DefineSymbols)
                sharedCompiler.DefineSymbols.Add(define);
        }

        /// <summary>
        /// Log the last output of the Roslyn compiler to the Unity console.
        /// </summary>
        public void LogCompilerOutputToConsole()
        {
            // Check for no report
            if (compileResult == null)
                return;
            
            bool loggedHeader = false;

            // Simple function to only output the header when one or more errors, warnings or infos will be logged
            Action logHeader = () =>
            {
                if (loggedHeader == false)
                {
                    RoslynCSharp.Log("__Roslyn Compile Output__");
                    loggedHeader = true;
                }
            };

            // Process report
            foreach (CompilationError error in compileResult.Errors)
            {
                if(error.IsError == true)
                {
                    // Log as error
                    logHeader();
                    RoslynCSharp.LogError(error.ToString());
                }
                else if(error.IsWarning == true)
                {
                    // Log as warning
                    logHeader();
                    RoslynCSharp.LogWarning(error.ToString());
                }
                else if(error.IsInfo == true)
                {
                    logHeader();
                    RoslynCSharp.Log(error.ToString());
                }
            }
        }

        private void CheckDisposed()
        {
            // Check for our sandbox domain
            if(sandbox == null)
                throw new ObjectDisposedException("The 'ScriptDomain' has already been disposed");
        }

        private void CheckCompiler()
        {
            // Check for our compiler service
            if (sharedCompiler == null)
                throw new Exception("The compiler service has not been initialized");
        }

        private ScriptAssembly RegisterAssembly(Assembly assembly, string assemblyPath, byte[] assemblyImage, ScriptSecurityMode securityMode, bool isRuntimeCompiled, CompilationResult compileResult = null)
        {
            // Check for error
            if (assembly == null)
                return null;

            // Reset report
            securityResult = null;

            // Create script assembly
            ScriptAssembly scriptAssembly = new ScriptAssembly(this, assembly, compileResult);

            // Set meta data
            scriptAssembly.AssemblyPath = assemblyPath;
            scriptAssembly.AssemblyImage = assemblyImage;

            // Check for ensure security mode
            bool performSecurityCheck = (securityMode == ScriptSecurityMode.EnsureSecurity);

            // Get value from settings
            if (securityMode == ScriptSecurityMode.UseSettings)
                performSecurityCheck = RoslynCSharp.Settings.SecurityCheckCode;

            // Check for security checks
            if (performSecurityCheck == true)
            {
                CodeSecurityRestrictions restrictions = RoslynCSharp.Settings.SecurityRestrictions;

                // Use pinvoke option
                restrictions.AllowPInvoke = RoslynCSharp.Settings.AllowPInvoke;

                // Perform code validation
                if (scriptAssembly.SecurityCheckAssembly(restrictions, out securityResult) == false)
                {
                    // Log the error
                    RoslynCSharp.LogError(securityResult.GetSummaryText());
                    RoslynCSharp.LogError(securityResult.GetAllText(true));
                    // Dont load the assembly
                    return null;
                }
                else
                    RoslynCSharp.Log(securityResult.GetSummaryText());
            }

            // Mark as runtime compiled
            if (isRuntimeCompiled == true)
                scriptAssembly.MarkAsRuntimeCompiled();

            lock (this)
            {
                // Register with domain
                this.loadedAssemblies.Add(scriptAssembly);
            }

            // Return result
            return scriptAssembly;
        }

        /// <summary>
        /// Creates a new <see cref="ScriptDomain"/> into which assemblies and scripts may be loaded.
        /// </summary>
        /// <returns>A new instance of <see cref="ScriptDomain"/></returns>
        public static ScriptDomain CreateDomain(string domainName, bool initCompiler = true, bool makeActiveDomain = true, AppDomain sandboxDomain = null)
        {
            // Create a new named domain
            ScriptDomain domain = new ScriptDomain(domainName, sandboxDomain);

            // Load the roslyn settings - do this now because the next load request could be from a worker thread
            RoslynCSharp.LoadResources();

            // Check for compiler
            if (initCompiler == true)
                domain.InitializeCompilerService();

            // Make domain active
            if (makeActiveDomain == true)
                MakeDomainActive(domain);

            return domain;
        }

        /// <summary>
        /// Attempt to find a domain with the specified name.
        /// </summary>
        /// <param name="domainName">The domain name to search for</param>
        /// <returns>A domain with the specified name or null if no matching domain was found</returns>
        public static ScriptDomain FindDomain(string domainName)
        {
            foreach(ScriptDomain domain in activeDomains)
            {
                if (domain.name == domainName)
                    return domain;
            }

            // Domain not found
            return null;
        }

        /// <summary>
        /// Set the specified domain as the active domain.
        /// The active domain is used when resolving script types from an unspecified source.
        /// </summary>
        /// <param name="domain">The domain to make active</param>
        public static void MakeDomainActive(ScriptDomain domain)
        {
            // Check for null domain
            if (domain == null)
                throw new ArgumentNullException(nameof(domain));

            // Make active
            active = domain;
        }

        /// <summary>
        /// Set the domain with the specified name as the active domain.
        /// The active domain is used when resolving script types from an unspecified source.
        /// </summary>
        /// <param name="domainName">The name of the domain to make active</param>
        public static void MakeDomainActive(string domainName)
        {
            // Find domain with name
            ScriptDomain domain = FindDomain(domainName);

            // Make active
            if (domain != null)
                MakeDomainActive(domain);
        }
    }
}
