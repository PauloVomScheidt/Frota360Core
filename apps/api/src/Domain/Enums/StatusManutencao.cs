namespace Frota360.Domain.Enums
{
    /// <summary>
    /// Estados que o usuário provoca em uma manutenção. "Atrasada" não entra aqui:
    /// é derivada da quilometragem atual do veículo no momento da leitura
    /// (ver <c>ManutencaoMappings</c>), evitando um job para envelhecer registros.
    /// </summary>
    public enum StatusManutencao
    {
        Pendente = 0,
        Realizada = 1,
        Cancelada = 2
    }
}
