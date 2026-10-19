namespace PhoneStore.Services;
public class ImageService(IWebHostEnvironment env)
{
    public async Task ValidateAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (file.Length is <= 0 or > 5 * 1024 * 1024 || !new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension))
            throw new InvalidOperationException("Ảnh phải là JPG, PNG hoặc WebP và không quá 5 MB.");
        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var read = await stream.ReadAtLeastAsync(header, 12, false);
        var valid = read >= 12 && (extension switch {
            ".jpg" or ".jpeg" => header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff,
            ".png" => header.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".webp" => System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP",
            _ => false });
        if (!valid) throw new InvalidOperationException("Nội dung tệp không phải định dạng ảnh hợp lệ.");
    }
    public async Task<string> SaveAsync(IFormFile file)
    {
        await ValidateAsync(file);
        var name = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
        var directory = Path.Combine(env.WebRootPath, "images", "products");
        Directory.CreateDirectory(directory);
        await using var output = new FileStream(Path.Combine(directory, name), FileMode.CreateNew);
        await file.CopyToAsync(output);
        return "/images/products/" + name;
    }
    public void Delete(string path)
    {
        if (!path.StartsWith("/images/products/") || !Guid.TryParseExact(Path.GetFileNameWithoutExtension(path), "N", out _)) return;
        var file = Path.Combine(env.WebRootPath, "images", "products", Path.GetFileName(path));
        if (File.Exists(file)) File.Delete(file);
    }
}
