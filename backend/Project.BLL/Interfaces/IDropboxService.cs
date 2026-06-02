using Common.Results;

namespace Project.BLL.Interfaces;

public interface IDropboxService
{
    Task<OperationResult<string>> UploadFileAsync(Stream fileStream, string fileName, string? folder = null);
    Task<OperationResult<string>> GetSharedLinkAsync(string path);
    Task<OperationResult> DeleteFileAsync(string path);
    Task<OperationResult<IEnumerable<string>>> ListFilesAsync(string? folder = null);
    Task<OperationResult<Stream>> DownloadFileAsync(string path);
}
