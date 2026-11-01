using RastreadorHabitos.Modulo.Metas.Entities;

namespace RastreadorHabitos.Tests.Metas;

// La máquina de estados de la Meta es independiente de la de Gestión de permisos [RF-NEG-09].
public class IndependenciaMaquinaMetaTests
{
    // El módulo de Metas no referencia a Gestión de permisos: cambiar sus estados no lo obliga
    // a cambiar.
    [Fact]
    public void ModuloMetas_NoReferenciaAGestionDePermisos()
    {
        // Preparar
        var moduloMetas = typeof(EstadoMeta).Assembly;

        // Actuar
        var referencias = moduloMetas.GetReferencedAssemblies().Select(a => a.Name).ToList();

        // Verificar
        Assert.DoesNotContain("RastreadorHabitos.Core.GestionPermisos", referencias);
    }

    // Sus estados son exactamente los cuatro de EstadoMeta, entre 3 y 5 [RF-NEG-03].
    [Fact]
    public void EstadoMeta_DeclaraCuatroEstados()
    {
        // Preparar: nada.

        // Actuar
        var estados = Enum.GetNames<EstadoMeta>();

        // Verificar
        Assert.Equal(["Pendiente", "EnProgreso", "Completada", "Fallida"], estados);
    }
}
