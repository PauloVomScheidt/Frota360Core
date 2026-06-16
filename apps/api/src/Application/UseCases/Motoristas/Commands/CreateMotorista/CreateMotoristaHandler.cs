using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Motorista.Response;
using Frota360.Application.UseCases.Motoristas;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Motoristas.Commands.CreateMotorista
{
    public sealed class CreateMotoristaHandler(IMotoristaRepository repository, ILogger<CreateMotoristaHandler> logger)
        : ICommandHandler<CreateMotoristaCommand, MotoristaResponse>
    {
        public async Task<MotoristaResponse> HandleAsync(CreateMotoristaCommand command, CancellationToken cancellationToken = default)
        {
            var request = command.Data;

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

                return criado.ToResponse();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao cadastrar motorista {Nome} | CPF {CPF}", request.Nome, request.CPF);
                throw;
            }
        }
    }
}
