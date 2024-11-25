namespace kate.FileShare.Data.Models;

public class S3FileInformationModel
{
    public const string TableName = "S3FileInformation";
    public string Id { get; set; }
    public FileModel File { get; set; }
    public long FileSize { get; set; }
    public int ChunkSize { get; set; }
    public virtual int TotalNumberOfChunks
    {
        get
        {
            return (int)Math.Ceiling(FileSize / (ChunkSize * 1F));
        }
    }

    public List<S3FileChunkModel> Chunks { get; set; }
}