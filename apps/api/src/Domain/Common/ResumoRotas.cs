namespace Frota360.Domain.Common
{
    /// <summary>
    /// Agregados das rotas encerradas num período: quantas foram e quanto rodaram.
    ///
    /// Existe pelo mesmo motivo dos demais resumos — com a listagem paginada, o dashboard não
    /// pode mais somar `kmPercorrido` do array que recebe. O recorte é por <c>DataFim</c>, o
    /// momento em que a quilometragem foi apurada: rota ainda aberta não tem km a somar.
    /// </summary>
    public sealed record ResumoRotas(int Quantidade, int KmTotal);
}
