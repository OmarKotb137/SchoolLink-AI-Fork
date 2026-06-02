namespace Project.BLL.DTOs.Library;

public class LibraryStatsDto
{
    public int TotalItems { get; set; }
    public long TotalSizeBytes { get; set; }
    public int BooksCount { get; set; }
    public int FilesCount { get; set; }
    public int VideosCount { get; set; }
    public int LinksCount { get; set; }
    public int NotesCount { get; set; }
}
