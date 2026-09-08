namespace Frota360.Application.DTOs.Rota.Response
{
    /// <summary>
    /// Agregados das rotas encerradas num período, para o KPI "Km da frota" do dashboard.
    /// Com a listagem paginada, somar `kmPercorrido` no cliente deixou de ser possível.
    /// </summary>
    public class ResumoRotasResponse
    {
        public int Quantidade { get; set; }
        public int KmTotal { get; set; }
    }
}
