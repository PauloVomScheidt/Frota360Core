using Frota360.Application.DTOs.Rota.Request;
using Frota360.Application.DTOs.Rota.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.Services
{
    public class RotaService(IRotaRepository repository, ILogger<RotaService> logger) : IRotaService
    {
        public async Task<IEnumerable<RotaResponse>> GetAllAsync()
        {
            logger.LogInformation("Buscando todas as rotas");

            var rotas = await repository.GetAllAsync();

            logger.LogInformation("Foram encontradas {QuantidadeRotas} rotas", rotas.Count());

            return rotas.Select(ToResponse);
        }

        public async Task<RotaResponse> AddAsync(CreateRotaRequest request)
        {
            try
            {
                logger.LogInformation("Iniciando cadastro da rota {Origem} -> {Destino}", request.Origem, request.Destino);

                var rota = new Rota
                {
                    Origem = request.Origem,
                    Destino = request.Destino,
                    CodigoMotorista = request.CodigoMotorista,
                    CodigoVeiculo = request.CodigoVeiculo,
                    Ativo = request.Ativo,
                    DataInicio = request.DataInicio,
                    DataFim = request.DataFim,
                    DataInclusao = DateTime.UtcNow,
                };

                var criado = await repository.AddAsync(rota);

                logger.LogInformation("Rota cadastrada com sucesso. Id {Id} | Origem {Origem} | Destino {Destino}", criado.Id, criado.Origem, criado.Destino);

                return ToResponse(criado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,"Erro ao cadastrar rota {Origem} -> {Destino}", request.Origem,request.Destino);
                throw;
            }
        }

        public async Task<RotaResponse?> UpdateAsync(int id, UpdateRotaRequest request)
        {
            try
            {
                logger.LogInformation("Iniciando atualização da rota Id {Id}", id);

                var rota = await repository.GetByIdAsync(id);

                if (rota is null)
                {
                    logger.LogWarning("Tentativa de atualizar rota inexistente. Id {Id}",id);
                    return null;
                }

                rota.Origem = request.Origem;
                rota.Destino = request.Destino;
                rota.CodigoMotorista = request.CodigoMotorista;
                rota.CodigoVeiculo = request.CodigoVeiculo;
                rota.Ativo = request.Ativo;
                rota.DataInicio = request.DataInicio;
                rota.DataFim = request.DataFim;

                var atualizado = await repository.UpdateAsync(rota);

                logger.LogInformation("Rota atualizada com sucesso. Id {Id}", atualizado.Id);

                return ToResponse(atualizado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao atualizar rota Id {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                logger.LogInformation("Iniciando remoção da rota Id {Id}", id);

                var rota = await repository.GetByIdAsync(id);

                if (rota is null)
                {
                    logger.LogWarning("Tentativa de remover rota inexistente. Id {Id}", id);
                    return false;
                }

                await repository.DeleteAsync(rota);

                logger.LogInformation("Rota removida com sucesso. Id {Id}", id);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao remover rota Id {Id}", id);
                throw;
            }
        }

        // mapeamento centralizado
        private static RotaResponse ToResponse(Rota v) => new()
        {
            Id = v.Id,
            Ativo = v.Ativo,
            CodigoMotorista = v.CodigoMotorista,
            CodigoVeiculo = v.CodigoVeiculo,
            DataFim = v.DataFim,
            DataInicio = v.DataInicio,
            Destino = v.Destino,
            Origem = v.Origem,
            DataInclusao = v.DataInclusao
        };
    }
}
