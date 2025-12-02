
using Baubit.DI;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SampleConsoleApp;


var task1 = ModulesLoadedFromAppsettings.RunAsync();
var task2 = ModulesLoadedFromExplicitlyGivenComponent.RunAsync();

await Task.WhenAll(task1, task2);

class MyHostedService : IHostedService
{
    private MyService myService;
    public MyHostedService(MyService myService)
    {
        this.myService = myService;
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"Started with {myService.SomeStrProperty}");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

class MyService
{
    public string SomeStrProperty { get; init; }
    public MyService(string someStrProperty)
    {
        SomeStrProperty = someStrProperty;
    }
}

class MyModuleConfiguration : AConfiguration
{
    public string MyStringProperty { get; set; }
}

class MyModule : AModule<MyModuleConfiguration>
{
    public MyModule(IConfiguration configuration) : base(configuration)
    {
    }

    public MyModule(MyModuleConfiguration configuration, List<IModule> nestedModules = null) : base(configuration, nestedModules)
    {
    }

    public override void Load(IServiceCollection services)
    {
        services.AddHostedService<MyHostedService>();
        services.AddSingleton(sp => new MyService(Configuration.MyStringProperty));
        base.Load(services);
    }
}

class MyComponent : AComponent
{
    protected override Result<ComponentBuilder> Build(ComponentBuilder componentBuilder)
    {
        return componentBuilder.WithModule<MyModule, MyModuleConfiguration>(ConfigureModule);
    }

    private void ConfigureModule(MyModuleConfiguration configuration)
    {
        configuration.MyStringProperty = "Some string value";
    }
}


