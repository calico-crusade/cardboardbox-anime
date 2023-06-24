namespace CardboardBox.Scripting;

public interface IScriptEngine : IDisposable
{
    JsValue Execute(string script);
}

public class ScriptEngine : IScriptEngine
{
    private readonly Engine _engine;
    private readonly IScriptEngineConfig _config;

    public IServiceProvider? Provider => _config.Provider;

    public ScriptEngine(IScriptEngineConfig config)
    {
        _config = config;
        _engine = BuildEngineFromConfig(config);
    }

    public JsValue Execute(string script)
    {
        return _engine.Evaluate(script);
    }

    public void Dispose()
    {
        _engine.Dispose();
        GC.SuppressFinalize(this);
    }

    public static Engine BuildEngineFromConfig(IScriptEngineConfig config)
    {
        var engine = new Engine(c =>
        {
            if (config.TimeoutSec.HasValue && config.TimeoutSec.Value > 0)
                c.TimeoutInterval(TimeSpan.FromSeconds(config.TimeoutSec.Value));

            if (config.RecursionLimit.HasValue && config.RecursionLimit.Value > 0)
                c.LimitRecursion(config.RecursionLimit.Value);

            config.ConfigureOptions?.Invoke(c);
        });

        foreach (var method in config.ConfigureEngine)
            method(engine);

        return engine;
    }
}
