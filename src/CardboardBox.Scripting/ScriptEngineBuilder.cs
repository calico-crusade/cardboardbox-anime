namespace CardboardBox.Scripting;

public interface IScriptEngineBuilder
{
    /// <summary>
    /// Configure an action that runs against the script engine at run-time
    /// </summary>
    /// <param name="action">The action to run</param>
    /// <returns>The current instance of the config for chaining</returns>
    IScriptEngineBuilder Configure(Action<Engine> action);

    /// <summary>
    /// Registers the given service provider to resolve objects
    /// </summary>
    /// <param name="provider">The provider to use</param>
    /// <returns>The current instance of the config for chaining</returns>
    IScriptEngineBuilder WithProvider(IServiceProvider provider);

    /// <summary>
    /// Creates a service provider from the given service collection
    /// </summary>
    /// <param name="services">The configurator for the service collection</param>
    /// <returns>The current instance of the config for chaining</returns>
    IScriptEngineBuilder WithServices(Action<IServiceCollection> services);

    /// <summary>
    /// Creates a service provider from the given service collection
    /// </summary>
    /// <param name="services">The service collection to use</param>
    /// <returns>The current instance of the config for chaining</returns>
    IScriptEngineBuilder WithServices(IServiceCollection services);

    /// <summary>
    /// Adds a method handler to the script engine
    /// </summary>
    /// <param name="name">The name of the method to use from within JavaScript</param>
    /// <param name="method">The method delegate</param>
    /// <returns>The current instance of the config for chaining</returns>
    IScriptEngineBuilder WithMethod(string name, Delegate method);

    /// <summary>
    /// Adds method handlers to the script engine
    /// </summary>
    /// <param name="methods">The methods to add to the engine</param>
    /// <returns>The current instance of the config for chaining</returns>
    IScriptEngineBuilder WithMethods(params (string name, Delegate method)[] methods);

    /// <summary>
    /// Adds a variable to the script engine
    /// </summary>
    /// <param name="name">The name of the variable</param>
    /// <param name="value">The value of the variable</param>
    /// <returns>The current instance of the config for chaining</returns>
    IScriptEngineBuilder WithVariable(string name, string value);

    /// <summary>
    /// Adds a variable to the script engine
    /// </summary>
    /// <param name="name">The name of the variable</param>
    /// <param name="value">The value of the variable</param>
    /// <returns>The current instance of the config for chaining</returns>
    IScriptEngineBuilder WithVariable(string name, int value);

    /// <summary>
    /// Adds a variable to the script engine
    /// </summary>
    /// <param name="name">The name of the variable</param>
    /// <param name="value">The value of the variable</param>
    /// <returns>The current instance of the config for chaining</returns>
    IScriptEngineBuilder WithVariable(string name, double value);

    /// <summary>
    /// Adds a variable to the script engine
    /// </summary>
    /// <param name="name">The name of the variable</param>
    /// <param name="value">The value of the variable</param>
    /// <returns>The current instance of the config for chaining</returns>
    IScriptEngineBuilder WithVariable(string name, bool value);

    /// <summary>
    /// Adds a variable to the script engine
    /// </summary>
    /// <param name="name">The name of the variable</param>
    /// <param name="value">The value of the variable</param>
    /// <returns>The current instance of the config for chaining</returns>
    IScriptEngineBuilder WithVariable(string name, object? value);

    /// <summary>
    /// Registers all of the public methods on the given type
    /// </summary>
    /// <typeparam name="T">The type of object to register</typeparam>
    /// <returns>The current instance of the config for chaining</returns>
    IScriptEngineBuilder WithGlobalMethods<T>();

    /// <summary>
    /// Registers all of the public methods on the given type
    /// </summary>
    /// <param name="type">The type of object to register</param>
    /// <returns>The current instance of the config for chaining</returns>
    IScriptEngineBuilder WithGlobalMethods(Type type);

    /// <summary>
    /// Registers all of the public methods on the given type
    /// </summary>
    /// <param name="type">The type of target</param>
    /// <param name="instance">The instance of the target type</param>
    /// <returns>The current instance of the config for chaining</returns>
    IScriptEngineBuilder WithGlobalMethods(Type type, object? instance);

    /// <summary>
    /// Registers all of the public methods for the given instance
    /// </summary>
    /// <typeparam name="T">The type of target</typeparam>
    /// <param name="instance">The instance of the target</param>
    /// <returns>The current instance of the config for chaining</returns>
    IScriptEngineBuilder WithGlobalMethods<T>(T instance);

    /// <summary>
    /// Registers all of the public methods for the given instance
    /// </summary>
    /// <param name="instance">The instance of the target</param>
    /// <returns>The current instance of the config for chaining</returns>
    IScriptEngineBuilder WithGlobalMethods(object instance);

    /// <summary>
    /// Creates an instance of the <see cref="ScriptEngine"/>
    /// </summary>
    /// <returns>The scirpt engine</returns>
    IScriptEngine Build();
}

public interface IScriptEngineConfig
{
    /// <summary>
    /// The maximum amount of time the script can run for. Set to null for no limit.
    /// </summary>
    int? TimeoutSec { get; }

    /// <summary>
    /// The maximum amount of recursion the script can do. Set to null for no limit.
    /// </summary>
    int? RecursionLimit { get; }

    /// <summary>
    /// Allows for configuring the underlying script engine
    /// </summary>
    public Action<Options>? ConfigureOptions { get; }

    /// <summary>
    /// The service provider for object resolution
    /// </summary>
    IServiceProvider? Provider { get; }

    /// <summary>
    /// Allows for configuring the underlying script engine
    /// </summary>
    List<Action<Engine>> ConfigureEngine { get; }
}

public class ScriptEngineBuilder : IScriptEngineBuilder, IScriptEngineConfig
{
    #region Config Options

    /// <summary>
    /// The maximum amount of time the script can run for. Set to null for no limit.
    /// </summary>
    public int? TimeoutSec { get; set; } = 10;

    /// <summary>
    /// The maximum amount of recursion the script can do. Set to null for no limit.
    /// </summary>
    public int? RecursionLimit { get; set; } = 900;

    /// <summary>
    /// Allows for configuring the underlying script engine
    /// </summary>
    public Action<Options>? ConfigureOptions { get; set; }

    /// <summary>
    /// The service provider for object resolution
    /// </summary>
    public IServiceProvider? Provider { get; set; }

    /// <summary>
    /// Allows for configuring the underlying script engine
    /// </summary>
    public List<Action<Engine>> ConfigureEngine { get; } = new();

    #endregion

    #region Config Builder Methods

    /// <summary>
    /// Configure an action that runs against the script engine at run-time
    /// </summary>
    /// <param name="action">The action to run</param>
    /// <returns>The current instance of the config for chaining</returns>
    public IScriptEngineBuilder Configure(Action<Engine> action)
    {
        ConfigureEngine.Add(action);
        return this;
    }

    /// <summary>
    /// Registers the given service provider to resolve objects
    /// </summary>
    /// <param name="provider">The provider to use</param>
    /// <returns>The current instance of the config for chaining</returns>
    public IScriptEngineBuilder WithProvider(IServiceProvider provider)
    {
        Provider = provider;
        return this;
    }

    /// <summary>
    /// Creates a service provider from the given service collection
    /// </summary>
    /// <param name="services">The service collection to use</param>
    /// <returns>The current instance of the config for chaining</returns>
    public IScriptEngineBuilder WithServices(IServiceCollection services)
    {
        Provider = services.BuildServiceProvider();
        return this;
    }

    /// <summary>
    /// Creates a service provider from the given service collection
    /// </summary>
    /// <param name="services">The configurator for the service collection</param>
    /// <returns>The current instance of the config for chaining</returns>
    public IScriptEngineBuilder WithServices(Action<IServiceCollection> services)
    {
        var collection = new ServiceCollection();
        services(collection);
        return WithServices(collection);
    }

    /// <summary>
    /// Adds a method handler to the script engine
    /// </summary>
    /// <param name="name">The name of the method to use from within JavaScript</param>
    /// <param name="method">The method delegate</param>
    /// <returns>The current instance of the config for chaining</returns>
    public IScriptEngineBuilder WithMethod(string name, Delegate method)
    {
        return Configure(engine => engine.SetValue(name, method));
    }

    /// <summary>
    /// Adds method handlers to the script engine
    /// </summary>
    /// <param name="methods">The methods to add to the engine</param>
    /// <returns>The current instance of the config for chaining</returns>
    public IScriptEngineBuilder WithMethods(params (string name, Delegate method)[] methods)
    {
        foreach (var (name, method) in methods)
            WithMethod(name, method);
        return this;
    }

    /// <summary>
    /// Adds a variable to the script engine
    /// </summary>
    /// <param name="name">The name of the variable</param>
    /// <param name="value">The value of the variable</param>
    /// <returns>The current instance of the config for chaining</returns>
    public IScriptEngineBuilder WithVariable(string name, string value)
    {
        return Configure(engine => engine.SetValue(name, value));
    }

    /// <summary>
    /// Adds a variable to the script engine
    /// </summary>
    /// <param name="name">The name of the variable</param>
    /// <param name="value">The value of the variable</param>
    /// <returns>The current instance of the config for chaining</returns>
    public IScriptEngineBuilder WithVariable(string name, int value)
    {
        return Configure(engine => engine.SetValue(name, value));
    }

    /// <summary>
    /// Adds a variable to the script engine
    /// </summary>
    /// <param name="name">The name of the variable</param>
    /// <param name="value">The value of the variable</param>
    /// <returns>The current instance of the config for chaining</returns>
    public IScriptEngineBuilder WithVariable(string name, double value)
    {
        return Configure(engine => engine.SetValue(name, value));
    }

    /// <summary>
    /// Adds a variable to the script engine
    /// </summary>
    /// <param name="name">The name of the variable</param>
    /// <param name="value">The value of the variable</param>
    /// <returns>The current instance of the config for chaining</returns>
    public IScriptEngineBuilder WithVariable(string name, bool value)
    {
        return Configure(engine => engine.SetValue(name, value));
    }

    /// <summary>
    /// Adds a variable to the script engine
    /// </summary>
    /// <param name="name">The name of the variable</param>
    /// <param name="value">The value of the variable</param>
    /// <returns>The current instance of the config for chaining</returns>
    public IScriptEngineBuilder WithVariable(string name, object? value)
    {
        return Configure(engine => engine.SetValue(name, value));
    }

    /// <summary>
    /// Registers all of the public methods on the given type
    /// </summary>
    /// <typeparam name="T">The type of object to register</typeparam>
    /// <returns>The current instance of the config for chaining</returns>
    public IScriptEngineBuilder WithGlobalMethods<T>() => WithGlobalMethods(typeof(T));

    /// <summary>
    /// Registers all of the public methods on the given type
    /// </summary>
    /// <param name="type">The type of object to register</param>
    /// <returns>The current instance of the config for chaining</returns>
    public IScriptEngineBuilder WithGlobalMethods(Type type)
    {
        return Configure(engine =>
        {
            var instance = CreateInstance(type);
            AddMethods(engine, type, instance);
        });
    }

    /// <summary>
    /// Registers all of the public methods on the given type
    /// </summary>
    /// <param name="type">The type of target</param>
    /// <param name="instance">The instance of the target type</param>
    /// <returns>The current instance of the config for chaining</returns>
    public IScriptEngineBuilder WithGlobalMethods(Type type, object? instance)
    {
        return Configure(engine => AddMethods(engine, type, instance));
    }

    /// <summary>
    /// Registers all of the public methods for the given instance
    /// </summary>
    /// <typeparam name="T">The type of target</typeparam>
    /// <param name="instance">The instance of the target</param>
    /// <returns>The current instance of the config for chaining</returns>
    public IScriptEngineBuilder WithGlobalMethods<T>(T instance) => WithGlobalMethods(typeof(T), instance);

    /// <summary>
    /// Registers all of the public methods for the given instance
    /// </summary>
    /// <param name="instance">The instance of the target</param>
    /// <returns>The current instance of the config for chaining</returns>
    public IScriptEngineBuilder WithGlobalMethods(object instance) => WithGlobalMethods(instance.GetType(), instance);

    /// <summary>
    /// Creates an instance of the <see cref="ScriptEngine"/>
    /// </summary>
    /// <returns>The scirpt engine</returns>
    public IScriptEngine Build() => new ScriptEngine(this);

    #endregion

    #region HelperMethods 

    public object CreateInstance(Type type)
    {
        if (Provider is not null)
        {
            var instance = Provider.GetService(type);
            if (instance != null) return instance;
        }

        return Activator.CreateInstance(type) 
            ?? throw new InvalidOperationException("Could not determine constructor for type: " + type.Name);
    }

    public void AddMethods(Engine engine, Type type, object? instance)
    {
        foreach (var method in type.GetMethods())
        {
            var name = method.Name;
            var delType = Expression.GetDelegateType(
                method.GetParameters()
                    .Select(p => p.ParameterType)
                    .Concat(new[] { method.ReturnType })
                    .ToArray()
                );
            var del = method.CreateDelegate(delType, instance);
            engine.SetValue(name, del);
        }
    }

    #endregion
}
