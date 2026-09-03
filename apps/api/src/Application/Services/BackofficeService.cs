using Frota360.Application.DTOs.Backoffice.Request;
using Frota360.Application.DTOs.Backoffice.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.Services
{
    public class BackofficeService(IEmpresaRepository empresaRepository,
                                   IConviteService conviteService,
                                   ITipoManutencaoRepository tipoManutencaoRepository,
                                   ITipoDespesaRepository tipoDespesaRepository,
                                   ILogger<BackofficeService> logger) : IBackofficeService
    {
        public async Task<EmpresaProvisionadaResponse> ProvisionarEmpresaAsync(ProvisionarEmpresaRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.CNPJ) && await empresaRepository.ExisteCnpjAsync(request.CNPJ))
                throw new InvalidOperationException("CNPJ já cadastrado.");

            var empresa = await empresaRepository.AddAsync(new Empresa
            {
                Nome = request.NomeEmpresa,
                CNPJ = string.IsNullOrWhiteSpace(request.CNPJ) ? null : request.CNPJ,
                Ativo = true,
                DataInclusao = DateTime.Now
            });

            await SemearTiposManutencaoAsync(empresa.Id);
            await SemearTiposDespesaAsync(empresa.Id);

            var convite = await conviteService.CriarParaEmpresaAsync(
                empresa.Id, criadoPorUsuarioId: null, request.EmailAdmin, Roles.Admin);

            logger.LogInformation("Empresa {Id} ({Nome}) provisionada; convite de admin enviado para {Email}",
                empresa.Id, empresa.Nome, request.EmailAdmin);

            return new EmpresaProvisionadaResponse
            {
                EmpresaId = empresa.Id,
                NomeEmpresa = empresa.Nome,
                EmailAdmin = request.EmailAdmin,
                LinkConvite = convite.LinkConvite
            };
        }

        /// <summary>
        /// Semeia o catálogo padrão de manutenção para que a empresa nova já tenha o que
        /// selecionar na tela; a partir daqui o catálogo é editável por ela.
        /// </summary>
        private async Task SemearTiposManutencaoAsync(int empresaId)
        {
            var agora = DateTime.Now;

            await tipoManutencaoRepository.AddRangeAsync(
                TiposManutencaoPadrao.Itens.Select(item => new TipoManutencao
                {
                    EmpresaId = empresaId,
                    Nome = item.Nome,
                    IntervaloKm = item.IntervaloKm,
                    Ativo = true,
                    DataInclusao = agora
                }));

            logger.LogInformation("Catálogo padrão de manutenção semeado para a empresa {EmpresaId} ({Quantidade} tipos)",
                empresaId, TiposManutencaoPadrao.Itens.Count);
        }

        /// <summary>
        /// Mesmo motivo do catálogo de manutenção: sem tipo cadastrado, a tela de despesas
        /// abre com o seletor vazio e não aceita lançamento nenhum.
        /// </summary>
        private async Task SemearTiposDespesaAsync(int empresaId)
        {
            var agora = DateTime.Now;

            await tipoDespesaRepository.AddRangeAsync(
                TiposDespesaPadrao.Itens.Select(nome => new TipoDespesa
                {
                    EmpresaId = empresaId,
                    Nome = nome,
                    Ativo = true,
                    DataInclusao = agora
                }));

            logger.LogInformation("Catálogo padrão de despesa semeado para a empresa {EmpresaId} ({Quantidade} tipos)",
                empresaId, TiposDespesaPadrao.Itens.Count);
        }
    }
}
