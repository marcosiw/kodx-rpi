namespace Kodx.Rpi.Application.Rpis;

/// <summary>Permite que quem chamou (ex: o controller, ao enfileirar) saiba se deve encadear a próxima etapa e para qual edição.</summary>
public sealed record DownloadRpiEditionResult(bool Success, int Edicao);
