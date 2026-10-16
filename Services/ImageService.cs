using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace HotelReservationSystem.Services;

/*
 * @modified: Chong Jun Xiang
 * @description:
 * This service class is used to handle all the image related operations.
 */
public interface IImageService
{
    string ValidatePhoto(IFormFile f);
    string SavePhoto(IFormFile f, string folder);
    void DeletePhoto(string file, string folder);
}

public class ImageService : IImageService
{
    private const string ImageType = @"^image\/(jpeg|png)$";
    private const string ImageName = @"^.+\.(jpeg|jpg|png)$";
    private const int ImageSize = 5 * 1024 * 1024;

    private static readonly Regex ImageTypeRegex = new(ImageType, RegexOptions.IgnoreCase);
    private static readonly Regex ImageNameRegex = new(ImageName, RegexOptions.IgnoreCase);

    private readonly IWebHostEnvironment _environment;

    public ImageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string ValidatePhoto(IFormFile f)
    {
        if (!ImageTypeRegex.IsMatch(f.ContentType) || !ImageNameRegex.IsMatch(f.FileName))
            return "Only JPG and PNG photo is allowed.";

        return f.Length > ImageSize ? "Photo size cannot more than 5MB." : "";
    }

    public string SavePhoto(IFormFile f, string folder)
    {
        string file = Guid.NewGuid().ToString("n") + ".jpg";
        string path = Path.Combine(_environment.WebRootPath, folder, file);

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        using Stream stream = f.OpenReadStream();
        using Image img = Image.Load(stream);

        // Keep enough detail for the room cards and full room page. Previously every
        // upload was cropped to 200x200 and then enlarged by the browser, causing blur.
        if (img.Width > 1600 || img.Height > 1200)
        {
            img.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(1600, 1200),
                Mode = ResizeMode.Max
            }));
        }

        img.Save(path);

        return file;
    }

    public void DeletePhoto(string file, string folder)
    {
        file = Path.GetFileName(file);
        string path = Path.Combine(_environment.WebRootPath, folder, file);
        File.Delete(path);
    }
}
