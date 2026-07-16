namespace Kodx.Rpi.Domain.Rpis;

public enum ProcessingStage
{
    Download = 1,
    ConvertToTxt = 2,
    ExtractPublications = 3,
    UploadBlob = 4
}
