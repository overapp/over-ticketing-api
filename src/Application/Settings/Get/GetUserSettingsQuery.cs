using Application.Abstractions.Messaging;

namespace Application.Settings.Get;

public sealed record GetUserSettingsQuery : IQuery<UserSettingsResponse>;
