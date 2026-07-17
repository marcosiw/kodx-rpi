namespace Kodx.Rpi.Application.Rpis;

public sealed class RpiDownloadException(string message, Exception? innerException = null)
    : Exception(message, innerException);
