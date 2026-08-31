using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Frota360.Infrastructure.Data
{
    /// <summary>
    /// Normaliza o <see cref="DateTimeKind"/> de toda data antes de ela chegar ao banco.
    ///
    /// O Npgsql é estrito nos dois sentidos e lança exceção em vez de converter:
    /// <c>timestamptz</c> só aceita <c>Kind=Utc</c>, e <c>timestamp without time zone</c>
    /// só aceita <c>Unspecified</c> ou <c>Local</c>. O sistema, porém, grava dois Kinds
    /// diferentes na mesma coluna:
    ///
    /// <list type="bullet">
    ///   <item><c>Kind=Local</c> — todo <c>DateTime.Now</c> (DataInclusao, DataHora, ExpiraEm...).</item>
    ///   <item><c>Kind=Unspecified</c> — a data que o front manda como <c>"aaaa-MM-dd"</c> e o
    ///   System.Text.Json desserializa sem fuso (DataAbastecimento, DataInicio, DataPrevista...).</item>
    /// </list>
    ///
    /// <c>EncerrarRotaHandler</c> mistura os dois na mesma propriedade
    /// (<c>request.DataFim ?? DateTime.Now</c>). Hoje os dois Kinds são aceitos pela coluna,
    /// mas basta alguém voltar a usar <c>DateTime.UtcNow</c> num campo persistido para o
    /// insert começar a lançar. Descartar o Kind aqui torna a política imune a isso.
    ///
    /// O valor gravado é o relógio de parede de Brasília — ver a nota de fuso no
    /// <c>Dockerfile</c> e o log de inicialização em <c>Program.cs</c>, que registra o fuso
    /// efetivo do processo. A leitura devolve <c>Unspecified</c>, então a API serializa as
    /// datas sem sufixo <c>Z</c> e o front as exibe verbatim, sem conversão.
    ///
    /// A única data em UTC no sistema é o <c>expires</c> do JWT (<c>TokenService</c>), porque
    /// o claim <c>exp</c> é epoch UTC por definição do protocolo.
    /// </summary>
    public sealed class DataSemFusoConverter() : ValueConverter<DateTime, DateTime>(
        aoGravar => DateTime.SpecifyKind(aoGravar, DateTimeKind.Unspecified),
        aoLer => DateTime.SpecifyKind(aoLer, DateTimeKind.Unspecified));
}
