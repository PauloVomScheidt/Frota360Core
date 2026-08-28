using Frota360.Application.Common;
using Frota360.Application.DTOs.Convite.Request;
using Frota360.Application.DTOs.Convite.Response;
using Frota360.Application.DTOs.Usuario.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.Services
{
    public class ConviteService(IConviteRepository conviteRepository,
                                IUsuarioRepository usuarioRepository,
                                ITokenService tokenService,
                                IEmailService emailService,
                                ICurrentUserService currentUser,
                                IAuditoriaService auditoria,
                                FrontendSettings frontendSettings,
                                ILogger<ConviteService> logger) : IConviteService
    {
        private static readonly TimeSpan ValidadeConvite = TimeSpan.FromDays(7);
        private static readonly TimeSpan RefreshTokenValidade = TimeSpan.FromDays(7);

        public Task<ConviteCriadoResponse> CriarAsync(CriarConviteRequest request)
            => CriarParaEmpresaAsync(currentUser.EmpresaId, currentUser.UsuarioId, request.Email, request.Role);

        public async Task<ConviteCriadoResponse> CriarParaEmpresaAsync(int empresaId, int? criadoPorUsuarioId, string email, string role)
        {
            if (await usuarioRepository.ExisteEmailAsync(email))
                throw new InvalidOperationException("Já existe um usuário com este e-mail.");

            // Reenvio: convites pendentes anteriores para o mesmo e-mail são invalidados
            foreach (var pendente in await conviteRepository.GetPendentesByEmailAsync(email, empresaId))
                await conviteRepository.DeleteAsync(pendente);

            var token = tokenService.GerarRefreshToken();

            var convite = await conviteRepository.AddAsync(new Convite
            {
                EmpresaId = empresaId,
                Email = email,
                Role = role,
                TokenHash = TokenHelper.Hash(token),
                ExpiraEm = DateTime.UtcNow.Add(ValidadeConvite),
                CriadoPorUsuarioId = criadoPorUsuarioId,
                DataInclusao = DateTime.UtcNow
            });

            var link = MontarLink(token);

            await emailService.EnviarAsync(email, "Convite para o Frota360", CorpoEmail(role, link));

            logger.LogInformation("Convite criado para {Email} como {Role} na empresa {EmpresaId}", email, role, empresaId);

            // O backoffice também passa por aqui, sem sessão (criadoPorUsuarioId nulo): ali
            // não há ator para registrar, e o provisionamento fica só no Serilog.
            if (criadoPorUsuarioId is not null)
                await auditoria.RegistrarAsync(EntidadesAuditadas.Convite, AcoesAuditoria.Criou, convite.Id,
                    $"Convidou {email} como {role}");

            return new ConviteCriadoResponse
            {
                Id = convite.Id,
                Email = convite.Email,
                Role = convite.Role,
                ExpiraEm = convite.ExpiraEm,
                DataInclusao = convite.DataInclusao,
                LinkConvite = link
            };
        }


        public async Task<AuthResponse?> AceitarAsync(AceitarConviteRequest request)
        {
            var convite = await conviteRepository.GetByTokenHashAsync(TokenHelper.Hash(request.Token));

            if (convite is null || convite.UtilizadoEm is not null || convite.ExpiraEm < DateTime.UtcNow)
            {
                logger.LogWarning("Tentativa de aceite de convite inválido, utilizado ou expirado");
                return null;
            }

            if (await usuarioRepository.ExisteEmailAsync(convite.Email))
                throw new InvalidOperationException("Já existe um usuário com este e-mail.");

            var refreshToken = tokenService.GerarRefreshToken();

            var usuario = await usuarioRepository.AddAsync(new Usuario
            {
                EmpresaId = convite.EmpresaId,
                Nome = request.Nome,
                Email = convite.Email,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha),
                Role = convite.Role,
                // Dados pessoais são opcionais: quem não informou fica com nulo.
                CPF = string.IsNullOrWhiteSpace(request.CPF) ? null : request.CPF,
                DataNascimento = request.DataNascimento,
                Ativo = true,
                RefreshTokenHash = TokenHelper.Hash(refreshToken),
                RefreshTokenExpiraEm = DateTime.UtcNow.Add(RefreshTokenValidade),
                DataInclusao = DateTime.UtcNow
            });

            convite.UtilizadoEm = DateTime.UtcNow;
            await conviteRepository.UpdateAsync(convite);

            logger.LogInformation("Convite aceito. Usuário {Id} criado na empresa {EmpresaId} como {Role}",
                usuario.Id, usuario.EmpresaId, usuario.Role);

            // Único evento da trilha em que o ator é também o objeto — e o único sem sessão:
            // o aceite é anônimo, e o usuário que age acabou de nascer nesta operação.
            await auditoria.RegistrarComoAsync(usuario.EmpresaId, usuario,
                EntidadesAuditadas.Convite, AcoesAuditoria.Aceitou, convite.Id,
                $"Aceitou o convite e criou a conta {usuario.Email} como {usuario.Role}");

            return new AuthResponse
            {
                Token = tokenService.GerarToken(usuario),
                RefreshToken = refreshToken,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Role = usuario.Role
            };
        }

        public async Task<IEnumerable<ConviteResponse>> ListarAsync()
        {
            var convites = await conviteRepository.GetAllAsync(currentUser.EmpresaId);

            return convites.Select(c => new ConviteResponse
            {
                Id = c.Id,
                Email = c.Email,
                Role = c.Role,
                ExpiraEm = c.ExpiraEm,
                UtilizadoEm = c.UtilizadoEm,
                DataInclusao = c.DataInclusao
            });
        }

        public async Task<bool> CancelarAsync(int id)
        {
            var convite = await conviteRepository.GetByIdAsync(id, currentUser.EmpresaId);

            if (convite is null)
                return false;

            if (convite.UtilizadoEm is not null)
                throw new InvalidOperationException("Convite já utilizado não pode ser cancelado.");

            await conviteRepository.DeleteAsync(convite);

            logger.LogInformation("Convite {Id} cancelado", id);

            await auditoria.RegistrarAsync(EntidadesAuditadas.Convite, AcoesAuditoria.Cancelou, id,
                $"Cancelou o convite de {convite.Email} ({convite.Role})");

            return true;
        }

        private string MontarLink(string token)
            => $"{frontendSettings.BaseUrl.TrimEnd('/')}/convite?token={Uri.EscapeDataString(token)}";

        private static string CorpoEmail(string role, string link) => $"""
            <p>Você foi convidado(a) para acessar o <strong>Frota360</strong> com o perfil <strong>{role}</strong>.</p>
            <p><a href="{link}">Clique aqui para criar sua conta</a>. O link é válido por 7 dias.</p>
            <p>Se você não esperava este convite, ignore este e-mail.</p>
            """;
    }
}
