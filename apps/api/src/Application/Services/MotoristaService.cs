using Frota360.Application.DTOs.Motorista.Request;
using Frota360.Application.DTOs.Motorista.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.Services
{
    public class MotoristaService(IMotoristaRepository repository, ILogger<MotoristaService> logger) : IMotoristaService
    {
        public async Task<IEnumerable<MotoristaResponse>> GetAllAsync()
        {
            logger.LogInformation("Buscando todos os motoristas");

            var motoristas = await repository.GetAllAsync();

            logger.LogInformation("Foram encontrados {QuantidadeMotoristas} motoristas", motoristas.Count());

            return motoristas.Select(ToResponse);
        }

        public async Task<MotoristaResponse> AddAsync(CreateMotoristaRequest request)
        {
            try
            {
                logger.LogInformation("Iniciando cadastro do motorista {Nome} | CPF {CPF}", request.Nome, request.CPF);

                var motorista = new Motorista
                {
                    Nome = request.Nome,
                    Email = request.Email,
                    CPF = request.CPF,
                    DataNascimento = request.DataNascimento,
                    DataInclusao = DateTime.UtcNow,
                };

                var criado = await repository.AddAsync(motorista);

                logger.LogInformation("Motorista cadastrado com sucesso. Id {Id} | Nome {Nome}", criado.Id, criado.Nome);

                return ToResponse(criado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao cadastrar motorista {Nome} | CPF {CPF}", request.Nome, request.CPF);
                throw;
            }
        }

        public async Task<MotoristaResponse?> UpdateAsync(int id, UpdateMotoristaRequest request)
        {
            try
            {
                logger.LogInformation("Iniciando atualização do motorista Id {Id}", id);

                var motorista = await repository.GetByIdAsync(id);

                if (motorista is null)
                {
                    logger.LogWarning("Tentativa de atualizar motorista inexistente. Id {Id}", id);
                    return null;
                }

                motorista.Nome = request.Nome;
                motorista.Email = request.Email;
                motorista.CPF = request.CPF;
                motorista.DataNascimento = request.DataNascimento;

                var atualizado = await repository.UpdateAsync(motorista);

                logger.LogInformation("Motorista atualizado com sucesso. Id {Id}", atualizado.Id);

                return ToResponse(atualizado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao atualizar motorista Id {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                logger.LogInformation("Iniciando remoção do motorista Id {Id}", id);

                var motorista = await repository.GetByIdAsync(id);

                if (motorista is null)
                {
                    logger.LogWarning("Tentativa de remover motorista inexistente. Id {Id}", id);
                    return false;
                }

                await repository.DeleteAsync(motorista);

                logger.LogInformation("Motorista removido com sucesso. Id {Id}", id);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao remover motorista Id {Id}", id);
                throw;
            }
        }

        // mapeamento centralizado
        private static MotoristaResponse ToResponse(Motorista v) => new()
        {
            Id = v.Id,
            Nome = v.Nome,
            Email = v.Email,
            CPF = v.CPF,
            DataNascimento = v.DataNascimento,
            DataInclusao = v.DataInclusao
        };
    }
}
