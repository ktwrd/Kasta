using ImageMagick;
using kate.FileShare.Data;
using kate.FileShare.Data.Models;

namespace kate.FileShare.Services;

public class PreviewService
{
    private readonly FileService _fileService;
    private readonly S3Service _s3;

    public PreviewService(IServiceProvider services)
    {
        _fileService = services.GetRequiredService<FileService>();
        _s3 = services.GetRequiredService<S3Service>();
    }

    public bool PreviewSupported(FileModel file)
    {
        if (file.MimeType?.StartsWith("image/") ?? false)
        {
            if (file.MimeType == "image/svg+xml" || file.MimeType.StartsWith("image/svg"))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    public async Task<FilePreviewModel?> Create(ApplicationDbContext db, FileModel file)
    {
        if (PreviewSupported(file))
        {
            if (file.MimeType?.StartsWith("image/") ?? false)
            {
                return await CreateImagePreview(db, file);
            }
        }

        return null;
    }

    public async Task<FilePreviewModel?> CreateImagePreview(ApplicationDbContext db, FileModel file)
    {
        FilePreviewModel? preview = null;
        try
        {
            preview = await CreateImagePreview(file);
        }
        catch (ArgumentException)
        {
            return null;
        }

        if (preview == null)
        {
            return null;
        }
        
        await db.FilePreviews.AddAsync(preview);
        await db.SaveChangesAsync();
        return preview;
    }
    public async Task<FilePreviewModel?> CreateImagePreview(FileModel file)
    {
        if (!(file.MimeType?.StartsWith("image/") ?? false))
        {
            throw new ArgumentException(
                $"Cannot process file since it is not an image. MIME type reports {file.MimeType}. (id: {file.Id}, filename: {file.Filename})", nameof(file));
        }

        // SVG thumbnails are not supported
        if (file.MimeType.Contains("svg"))
        {
            return null;
        }
        

        using var imageStream = _fileService.GetMemoryStream(file, out var originalS3Object);
        try
        {
            imageStream.Seek(0, SeekOrigin.Begin);
            var info = new MagickImageInfo(imageStream);
            if (info.Width < 600 && info.Height < 600)
            {
                return new FilePreviewModel()
                {
                    Id = file.Id,
                    RelativeLocation = file.RelativeLocation,
                    Size = file.Size,
                    MimeType = file.MimeType,
                };
            }

            var img = new MagickImage(imageStream);
            var size = new MagickGeometry(600, 600)
            {
                IgnoreAspectRatio = false
            };
            img.Resize(size);
            var resultStream = new MemoryStream();
            await img.WriteAsync(resultStream, MagickFormat.Png24);
            resultStream.Seek(0, SeekOrigin.Begin);

            var model = new FilePreviewModel()
            {
                Id = file.Id,
                RelativeLocation = $"{file.Id}-preview/",
                Filename = "preview.png",
                Size = resultStream.Length,
                MimeType = "image/png"
            };
            var originalFilename = Path.GetFileName(file.Filename);
            if (!string.IsNullOrEmpty(originalFilename))
            {
                var fn = Path.GetFileNameWithoutExtension(originalFilename);
                if (string.IsNullOrEmpty(fn))
                {
                    model.Filename = "preview.png";
                }
                else
                {
                    model.Filename = $"{fn}-preview.png";
                }
            }
            model.RelativeLocation += model.Filename;
            
            var uploadResult = await _s3.UploadObject(resultStream, model.RelativeLocation);
            uploadResult.Dispose();
            await resultStream.DisposeAsync();
            originalS3Object.Dispose();
            return model;
        }
        catch (Exception ex)
        {
            originalS3Object.Dispose();
            throw new ApplicationException($"Failed to generate preview for file {file.Filename} (id: {file.Id})", ex);
        }

    }
}