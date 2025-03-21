using Kasta.Data.Models;

namespace Kasta.Web.Helpers;

public static class FileHelper
{
    private static string GetFilenameExtension(string filename)
    {
        var split = filename.Split('.');
        if (split.Length < 1) return "";
        return split[split.Length - 1];
    }
    public static string GetBootstrapIcon(FileModel file)
    {
        var prefix = "bi bi-";

        var filenameExtension = GetFilenameExtension(file.Filename);
        if (string.IsNullOrEmpty(filenameExtension) && string.IsNullOrEmpty(file.MimeType))
        {
            return prefix + "question-circle";
        }

        string mime = file.MimeType ?? "";

        var text = new Dictionary<string, string>()
        {
            {"", "file-earmark"},
            {"csv", "filetype-csv"},
            {"html", "filetype-html"},
            {"javascript", "filetype-js"},
            {"markdown", "filetype-md"},
            {"plain", "file-earmark-text"},
            {"richtext", "file-earmark-text"},
            {"troff", "file-earmark-text"},
            {"x-asm", "file-earmark-code"},
            {"x-java-source", "filetype-java"},
            {"x-python", "filetype-py"}
        };

        var audio = new Dictionary<string, string>()
        {
            {"", "file-music"}
        };

        var application = new Dictionary<string, string>()
        {
            {"msword", "file-earmark-word"},
            {"vnd.wordperfect", "file-earmark-word"},
            {"vnd.kde.kword", "file-earmark-word"},
            {"vnd.lotus-wordpro", "file-earmark-word"},
            {"vnd.ms-word.document.macroenabled.12", "file-earmark-word"},
            {"vnd.ms-word.template.macroenabled.12", "file-earmark-word"},
            {"vnd.openxmlformats-officedocument.wordprocessingml.document", "file-earmark-word"},
            {"vnd.openxmlformats-officedocument.wordprocessingml.template", "file-earmark-word"},
            {"vnd.oasis.opendocument.text", "file-earmark-word"},
            {"vnd.oasis.opendocument.text-master", "file-earmark-word"},
            {"vnd.oasis.opendocument.text-template", "file-earmark-word"},
            {"vnd.oasis.opendocument.text-web", "file-earmark-word"},
            {"vnd.stardivision.writer", "file-earmark-word"},
            {"vnd.stardivision.writer-global", "file-earmark-word"},
            {"vnd.sun.xml.writer", "file-earmark-word"},

            {"vnd.kde.kspread", "file-earmark-spreadsheet"},
            {"vnd.lotus-1-2-3", "file-earmark-spreadsheet"},
            {"vnd.ms-excel", "file-earmark-spreadsheet"},
            {"vnd.ms-excel.addin.macroenabled.12", "file-earmark-spreadsheet"},
            {"vnd.ms-excel.sheet.binary.macroenabled.12", "file-earmark-spreadsheet"},
            {"vnd.ms-excel.sheet.macroenabled.12", "file-earmark-spreadsheet"},
            {"vnd.ms-excel.template.macroenabled.12", "file-earmark-spreadsheet"},
            {"vnd.openxmlformats-officedocument.spreadsheetml.sheet", "file-earmark-spreadsheet"},
            {"vnd.openxmlformats-officedocument.spreadsheetml.template", "file-earmark-spreadsheet"},
            {"vnd.oasis.opendocument.spreadsheet", "file-earmark-spreadsheet"},
            {"vnd.oasis.opendocument.spreadsheet-template", "file-earmark-spreadsheet"},
            {"vnd.stardivision.calc", "file-earmark-spreadsheet"},
            {"vnd.sun.xml.calc", "file-earmark-spreadsheet"},

            {"vnd.sqlite3", "database"},
            {"x-msaccess", "database"},
            {"vnd.oasis.opendocument.database", "database"},
            {"vnd.oasis.opendocument.graphics", "file-earmark-easel"},
            {"vnd.oasis.opendocument.graphics-template", "file-earmark-easel"},
            {"vnd.oasis.opendocument.image", "file-earmark-image"},
            {"vnd.oasis.opendocument.image-template", "file-earmark-image"},

            
            {"vnd.ms-powerpoint", "file-earmark-presentation"},
            {"vnd.ms-powerpoint.addin.macroenabled.12", "file-earmark-presentation"},
            {"vnd.ms-powerpoint.presentation.macroenabled.12", "file-earmark-presentation"},
            {"vnd.ms-powerpoint.slide.macroenabled.12", "file-earmark-presentation"},
            {"vnd.ms-powerpoint.slideshow.macroenabled.12", "file-earmark-presentation"},
            {"vnd.ms-powerpoint.template.macroenabled.12", "file-earmark-presentation"},
            {"vnd.openxmlformats-officedocument.presentationml.slideshow", "file-earmark-presentation"},
            {"vnd.openxmlformats-officedocument.presentationml.template", "file-earmark-presentation"},
            {"vnd.oasis.opendocument.presentation", "file-earmark-presentation"},
            {"vnd.oasis.opendocument.presentation-template", "file-earmark-presentation"},
            {"vnd.stardivision.impress", "file-earmark-presentation"},
            {"vnd.sun.xml.impress", "file-earmark-presentation"},



            {"pdf", "file-earmark-pdf"},
            {"x-latex", "file-earmark-text"},
            {"rtf", "file-earmark-text"},

            {"wasm", "file-earmark-code"},
            {"x-7z-compressed", "file-earmark-zip"},
            {"x-bzip", "file-earmark-zip"},
            {"x-bzip2", "file-earmark-zip"},
            {"x-gzip", "file-earmark-zip"},
            {"x-rar-compressed", "file-earmark-zip"},
            {"x-zip-compressed", "file-earmark-zip"},
            {"gzip", "file-earmark-zip"},
            {"zip", "file-earmark-zip"},
            {"zip-compressed", "file-earmark-zip"},

            {"x-msdownload", "filetype-exe"},



            {"font-tdpfr", "file-earmark-font"},
            {"vnd.ms-fontobject", "file-earmark-font"},
            {"x-font-bdf", "file-earmark-font"},
            {"x-font-ghostscript", "file-earmark-font"},
            {"x-font-linux-psf", "file-earmark-font"},
            {"x-font-otf", "file-earmark-font"},
            {"x-font-pcf", "file-earmark-font"},
            {"x-font-snf", "file-earmark-font"},
            {"x-font-ttf", "file-earmark-font"},
            {"x-font-type1", "file-earmark-font"},

            {"x-iso9660-image", "disc"},

            {"vnd.google-earth.kml+xml", "globe-asia-australia"},
            {"vnd.google-earth.kmz", "globe-asia-australia"},

            {"vnd.amazon.ebook", "book"},
            {"x-dtbook+xml", "book"},
            {"x-mobipocket-ebook", "book"},
            {"epub+zip", "book"},

            {"json", "filetype-json"}
        };


        var index = mime.IndexOf('/');
        var key = mime;
        string? value = null;
        if (index != -1)
        {
            key = mime.Substring(0, index);
            value = mime.Substring(index + 1);
        }

        switch (key)
        {
            case "text":
                if (text.TryGetValue(value ?? "", out var textValue))
                {
                    return textValue;
                }
                break;
            case "model":
                return prefix + "box-fill";
            case "video":
                return prefix + "camera-reels";
            case "audio":
                if (audio.TryGetValue(value ?? "", out var audioValue))
                {
                    return audioValue;
                }
                return prefix + "file-earmark-music";
            case "application":
                if (application.TryGetValue(value ?? "", out var appValue))
                {
                    return appValue;
                }
                break;
            case "font":
                return prefix + "file-earmark-font";
            case "image":
                return prefix + "image";
            case "message":
                if (value == "rfc822")
                {
                    return prefix + "envelope";
                }
                break;
        }
        return prefix + "file";
    }
}