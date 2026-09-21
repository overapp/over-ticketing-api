namespace Application.Tickets.GetAttachment;

public sealed record AttachmentDownloadResponse(
    Stream Stream,
    string FileName,
    string ContentType);
