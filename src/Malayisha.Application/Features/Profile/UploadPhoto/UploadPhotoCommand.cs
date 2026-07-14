using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Profile.UploadPhoto;

public sealed record UploadPhotoCommand(
    Guid UserId,
    string FileName,
    string ContentType) : IRequest<Result<UploadProfilePhotoResponse>>;
