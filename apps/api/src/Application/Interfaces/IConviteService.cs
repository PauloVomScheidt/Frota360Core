using Frota360.Application.DTOs.Convite.Request;
using Frota360.Application.DTOs.Convite.Response;
using Frota360.Application.DTOs.Usuario.Response;

namespace Frota360.Application.Interfaces
{
    public interface IConviteService
    {
        /// <summary>Cria um convite na empresa do usuário logado e envia o link por e-mail.</summary>
        Task<ConviteCriadoResponse> CriarAsync(CriarConviteRequest request);

        /// <summary>Núcleo da criação, usado também pelo backoffice (sem usuário logado).</summary>
        Task<ConviteCriadoResponse> CriarParaEmpresaAsync(int empresaId, int? criadoPorUsuarioId, string email, string role);

        /// <summary>Aceita um convite: cria o usuário na empresa/role do convite e já autentica. Null se inválido/expirado.</summary>
        Task<AuthResponse?> AceitarAsync(AceitarConviteRequest request);

        Task<IEnumerable<ConviteResponse>> ListarAsync();

        /// <summary>Cancela um convite pendente. False se não encontrado na empresa.</summary>
        Task<bool> CancelarAsync(int id);
    }
}
