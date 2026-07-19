using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Application.Rpis;

/// <summary>Resultado de uma publicação extraída do texto da RPI, antes de virar entidade persistida.</summary>
public sealed record ExtractedPublication(string Numero, PublicationPayload Payload);
