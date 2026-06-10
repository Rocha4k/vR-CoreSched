using Warehouse.Backend.Contracts;

namespace Warehouse.Backend.Infrastructure;

internal static class WarehouseSeedData
{
    public static IReadOnlyList<MachineStateDto> Machines { get; } =
    [
        new MachineStateDto("press-01", "Prensa Hidráulica", "zona-producao", true, DateTimeOffset.UtcNow, 72, 2.3m, 1200, 9.1m, "Info"),
        new MachineStateDto("line-01", "Linha de Montagem", "linha-montagem", true, DateTimeOffset.UtcNow, 66, 1.8m, 820, 6.4m, "Info"),
        new MachineStateDto("belt-01", "Tapete Rolante", "corredor-a", true, DateTimeOffset.UtcNow, 58, 1.1m, 400, 3.2m, "Info")
    ];

    public static IReadOnlyList<LightingDeviceDto> Lighting { get; } =
    [
        new LightingDeviceDto("light-carga", "zona-carga", "Luz da Zona de Carga", true, DateTimeOffset.UtcNow, "seed"),
        new LightingDeviceDto("light-corridor-a", "corredor-a", "Luz do Corredor A", true, DateTimeOffset.UtcNow, "seed"),
        new LightingDeviceDto("light-corridor-b", "corredor-b", "Luz do Corredor B", true, DateTimeOffset.UtcNow, "seed"),
        new LightingDeviceDto("light-office", "escritorios", "Luz dos Escritórios", true, DateTimeOffset.UtcNow, "seed")
    ];
}
