using Application.Abstractions.Messaging;

namespace Application.Tickets.GetAttachment;

public sealed record GetAttachmentQuery(Guid AttachmentId) : IQuery<AttachmentDownloadResponse>;
