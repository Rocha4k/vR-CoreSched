using Warehouse.Simulator.Services;

Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<SimulatorOptions>(context.Configuration.GetSection(SimulatorOptions.SectionName));
        services.AddHostedService<MachineSimulationWorker>();
    })
    .Build()
    .Run();
