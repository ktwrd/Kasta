using ImageMagick;
using Kasta.Web.Data;
using Kasta.Web.Data.Models;

namespace Kasta.Web.Services;

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
        if (PreviewImageSupported(file))
            return true;
        else
            return false;
    }

    public bool PreviewImageSupported(FileModel file)
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

    public bool PreviewVideoSupported(FileModel file)
    {
        if (file.MimeType?.StartsWith("video/mp4") ?? false)
        {
            return true;
        }

        return false;
    }
    public async Task<FilePreviewModel?> Create(ApplicationDbContext db, FileModel file, Stream inputStream)
    {
        if (PreviewImageSupported(file))
        {
            return await RegisterPreviewAction(db, file, inputStream, CreateImagePreview);
        }
        /*else if (PreviewVideoSupported(file))
        {
            return await RegisterPreviewAction(db, file, inputStream, CreateVideoPreview);
        }*/

        return null;
    }

    public async Task<FilePreviewModel?> RegisterPreviewAction(
        ApplicationDbContext db,
        FileModel file,
        Stream inputStream,
        Func<FileModel, Stream, Task<FilePreviewModel?>> action)
    {
        FilePreviewModel? preview = null;
        try
        {
            preview = await action(file, inputStream);
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
        if (!PreviewImageSupported(file))
        {
            return null;
        }

        using var stream = _fileService.GetStream(file, out var r);
        try
        {
            var result = await CreateImagePreview(file, stream);
            r.Dispose();
            return result;
        }
        catch
        {
            r.Dispose();
            throw;
        }
    }
    public async Task<FilePreviewModel?> CreateVideoPreview(FileModel file)
    {
        if (!PreviewVideoSupported(file))
        {
            return null;
        }

        using var stream = _fileService.GetStream(file, out var r);
        try
        {
            var result = await CreateVideoPreview(file, stream);
            r.Dispose();
            return result;
        }
        catch
        {
            r.Dispose();
            throw;
        }
    }
    public async Task<FilePreviewModel?> CreateImagePreview(FileModel file, Stream inputStream)
    {
        if (!PreviewImageSupported(file))
        {
            return null;
        }

        try
        {
            inputStream.Seek(0, SeekOrigin.Begin);
            var img = new MagickImage(inputStream);
            var resultStream = new MemoryStream();

            await GeneratePreview(img, resultStream);
            var model = await UploadFile(file, resultStream);

            await resultStream.DisposeAsync();
            return model;
        }
        catch (Exception ex)
        {
            inputStream.Seek(0, SeekOrigin.Begin);
            throw new ApplicationException($"Failed to generate preview for file {file.Filename} (id: {file.Id})", ex);
        }

    }

    /// <summary>
    /// Generate preview (<see cref="MagickFormat.Png24"/>) of the image provided, and downscale to a maximum size of 600x600 (respects aspect ratio)
    /// </summary>
    /// <param name="image">Image to generate a preview for.</param>
    /// <param name="resultStream">Stream to write the PNG to.</param>
    public async Task GeneratePreview(IMagickImage image, Stream resultStream)
    {
        if (image.Width > 600 || image.Height > 600)
        {
            var size = new MagickGeometry(600, 600)
            {
                IgnoreAspectRatio = false
            };
            image.Resize(size);
        }
        await image.WriteAsync(resultStream, MagickFormat.Png24);
    }

    /// <summary>
    /// Generate instance of <see cref="FilePreviewModel"/>, then upload via <see cref="S3Service"/>
    /// </summary>
    /// <param name="parentFile">File that the preview was generated for</param>
    /// <param name="imageStream">Stream containing preview that was written to in <see cref="GeneratePreview"/></param>
    public async Task<FilePreviewModel> UploadFile(FileModel parentFile, Stream imageStream)
    {
        var model = new FilePreviewModel()
        {
            Id = parentFile.Id,
            RelativeLocation = $"{parentFile.Id}-preview/",
            Filename = "preview.png",
            Size = imageStream.Length,
            MimeType = "image/png"
        };
                
        var originalFilename = Path.GetFileName(parentFile.Filename);
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
                
        using var uploadResult = await _s3.UploadObject(imageStream, model.RelativeLocation);
        return model;
    }
    
    public async Task<FilePreviewModel?> CreateVideoPreview(FileModel file, Stream inputStream)
    {
        if (!PreviewVideoSupported(file))
            return null;

        try
        {
            using var resultStream = new MemoryStream();
            using (var videoFrames = new MagickImageCollection(inputStream, MagickFormat.Mp4))
            {
                if (videoFrames.Count < 1)
                {
                    throw new InvalidDataException($"Cannot generate preview since there are no frames!");
                }
                
                // get frame that is 10% into the video
                var target = Convert.ToInt32(Math.Round(Math.Max(videoFrames.Count * (decimal)0.10f, 0)));
                if (target > videoFrames.Count)
                    target = 0;

                var targetImage = videoFrames[target];
                await GeneratePreview(targetImage, resultStream);
            }

            var model = await UploadFile(file, resultStream);
            return model;
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Failed to generate preview for file {file.Filename} (id: {file.Id})", ex);
        }
    }
}