namespace Application.Tickets;

public sealed record FileUploadModel(
    string FileName,
    string ContentType,
    long Size,
    Stream ContentStream);
