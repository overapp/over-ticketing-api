using Application.Abstractions.Messaging;
using Domain.Wiki;

namespace Application.Wiki.UpdateWikiPageStatus;

public sealed record UpdateWikiPageStatusCommand(
    Guid WikiPageId,
    WikiPageStatus Status) : ICommand;
