using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

namespace ContosoUniversity.Web.Services;

/// <summary>
/// rw-005 (closes SEC-HIGH-002): teaching-material image upload validation contract.
///
/// The validator implements four overlapping defenses for the file-upload vector
/// flagged by SEC-HIGH-002:
///   1. Extension allowlist check (.jpg/.jpeg/.png/.gif/.bmp).
///   2. Magic-byte check via SixLabors.ImageSharp so a renamed
///      .aspx-disguised-as-.jpg is rejected even though its extension passes (1).
///   3. Path-traversal check on the supplied filename. Any "..", absolute path,
///      or directory separator yields <see cref="UploadValidationOutcome.PathTraversalRejected"/>.
///   4. Sanitized server-generated filename: callers MUST persist the file using
///      <see cref="GenerateSanitizedFileName"/> rather than the user-controlled name.
///
/// The framework-level 5 MB request-size cap (vector 4) is enforced declaratively
/// by <c>[RequestSizeLimit(5_242_880)]</c> on the action method and is therefore
/// outside this validator's scope.
/// </summary>
public interface IUploadValidator
{
    /// <summary>Returns the set of file extensions accepted as teaching-material images (lowercase, dot-prefixed).</summary>
    IReadOnlyCollection<string> AllowedExtensions { get; }

    /// <summary>
    /// Runs the path-traversal + extension-allowlist + magic-byte defenses against the supplied
    /// uploaded file content + client-supplied filename. The stream MUST be positioned at zero
    /// and seekable (the validator rewinds it before reading).
    /// </summary>
    Task<UploadValidationOutcome> ValidateAsync(
        Stream content, string clientFileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a sanitized server-side filename of the form
    /// <c>course_{courseId}_{guid8}{ext}</c> where <c>ext</c> is the validated lowercase
    /// extension (with the leading dot). The caller-controlled filename is intentionally NOT
    /// reused so a malicious filename cannot path-traverse on disk regardless of host OS.
    /// </summary>
    string GenerateSanitizedFileName(int courseId, string validatedExtension);
}

/// <summary>rw-005: explicit outcome enum so callers can map directly to HTTP status codes.</summary>
public enum UploadValidationOutcome
{
    Valid,
    EmptyFile,
    PathTraversalRejected,
    ExtensionRejected,
    MagicByteRejected
}

/// <summary>
/// rw-005 (closes SEC-HIGH-002): default <see cref="IUploadValidator"/>.
///
/// This implementation is intentionally registered as a singleton (no per-request state)
/// and is unit-testable in isolation — see <c>UploadValidatorTests</c> in
/// <c>src/ContosoUniversity.Web.UnitTests/</c>.
/// </summary>
public sealed class DefaultUploadValidator : IUploadValidator
{
    private static readonly HashSet<string> _allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp"
    };

    public IReadOnlyCollection<string> AllowedExtensions => _allowed;

    public async Task<UploadValidationOutcome> ValidateAsync(
        Stream content, string clientFileName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.CanSeek && content.Length == 0)
        {
            return UploadValidationOutcome.EmptyFile;
        }

        // (1) Path-traversal check — reject before we look at the extension. We MUST check the
        // raw client-supplied filename before any extension parsing because Path.GetExtension
        // on "../../etc/passwd.jpg" returns ".jpg" which would otherwise pass the allowlist.
        if (string.IsNullOrWhiteSpace(clientFileName))
        {
            return UploadValidationOutcome.PathTraversalRejected;
        }
        if (clientFileName.Contains("..", StringComparison.Ordinal)
            || clientFileName.Contains('/')
            || clientFileName.Contains('\\')
            || Path.IsPathRooted(clientFileName)
            || clientFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return UploadValidationOutcome.PathTraversalRejected;
        }

        // (2) Extension allowlist check.
        var ext = Path.GetExtension(clientFileName);
        if (string.IsNullOrEmpty(ext) || !_allowed.Contains(ext))
        {
            return UploadValidationOutcome.ExtensionRejected;
        }

        // (3) Magic-byte check via SixLabors.ImageSharp.Image.Identify. Identify reads only the
        // header bytes — it does not decode the entire pixel buffer — so it is cheap enough to
        // run inside the request handler. If the file is not actually a recognized image format,
        // Identify returns null and we reject as MagicByteRejected.
        if (content.CanSeek)
        {
            content.Position = 0;
        }
        try
        {
            var info = await Image.IdentifyAsync(content, cancellationToken).ConfigureAwait(false);
            if (info == null)
            {
                return UploadValidationOutcome.MagicByteRejected;
            }
        }
        catch (UnknownImageFormatException)
        {
            return UploadValidationOutcome.MagicByteRejected;
        }
        catch (InvalidImageContentException)
        {
            return UploadValidationOutcome.MagicByteRejected;
        }
        finally
        {
            if (content.CanSeek)
            {
                content.Position = 0;
            }
        }

        return UploadValidationOutcome.Valid;
    }

    public string GenerateSanitizedFileName(int courseId, string validatedExtension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(validatedExtension);
        if (!validatedExtension.StartsWith('.'))
        {
            validatedExtension = "." + validatedExtension;
        }
        if (!_allowed.Contains(validatedExtension))
        {
            throw new ArgumentOutOfRangeException(nameof(validatedExtension),
                $"Extension '{validatedExtension}' is not in the allowlist; call ValidateAsync first.");
        }
        var token = Guid.NewGuid().ToString("N").Substring(0, 8);
        return $"course_{courseId}_{token}{validatedExtension.ToLowerInvariant()}";
    }
}
