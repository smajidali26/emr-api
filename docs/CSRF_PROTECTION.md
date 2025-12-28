# CSRF Protection Implementation

**SECURITY FIX: Task #6 - Enforce CSRF on state-changing endpoints (Jennifer Harris - 8h)**

## Overview

This document describes the Cross-Site Request Forgery (CSRF) protection implementation for the EMR Backend API. CSRF protection is critical for preventing attackers from tricking authenticated users into performing unwanted actions.

## Implementation Details

### Middleware-Based Automatic CSRF Validation

The EMR API uses **automatic middleware-based CSRF validation** instead of manual attribute-based validation. This provides:

1. **Comprehensive Coverage**: All state-changing requests are automatically protected
2. **Defense-in-Depth**: Protection is applied at the middleware layer, not relying on developers to remember attributes
3. **Reduced Error Potential**: No risk of forgetting to add validation attributes to new endpoints

### Configuration (Program.cs)

CSRF protection is configured in three places in `Program.cs`:

#### 1. Antiforgery Service Configuration (Lines 33-41)

```csharp
// SECURITY: Add CSRF protection (antiforgery)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-Token";
    options.Cookie.Name = "XSRF-TOKEN";
    options.Cookie.HttpOnly = false; // Allow JavaScript to read the token
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
```

#### 2. Middleware Pipeline Registration (Line 76)

```csharp
// SECURITY: Add CSRF token validation middleware for state-changing requests
app.UseCsrfValidation();
```

#### 3. Token Endpoint (AuthController)

The `/api/auth/csrf-token` endpoint provides CSRF tokens to clients:

```csharp
[HttpGet("csrf-token")]
[AllowAnonymous]
public IActionResult GetCsrfToken()
{
    var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
    Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
    {
        HttpOnly = false,
        SameSite = SameSiteMode.Strict,
        Secure = true,
        Path = "/",
        MaxAge = TimeSpan.FromHours(1)
    });
    return Ok(new { token = tokens.RequestToken, headerName = "X-CSRF-Token" });
}
```

### Middleware Implementation (CsrfValidationMiddleware.cs)

The `CsrfValidationMiddleware` automatically validates CSRF tokens for:

- **State-changing HTTP methods**: POST, PUT, PATCH, DELETE
- **Authenticated requests only**: Unauthenticated requests are exempt
- **All endpoints except exempt paths**: Login, refresh, logout, health checks

#### Exempt Paths

These paths do not require CSRF validation:

- `/api/auth/login`
- `/api/auth/refresh`
- `/api/auth/logout`
- `/health`

#### Validation Logic

```csharp
// Skip validation for non-state-changing requests (GET, HEAD, OPTIONS)
if (!StateChangingMethods.Contains(method))
    return;

// Skip validation for exempt paths
if (ExemptPaths.Any(ep => path.StartsWith(ep, StringComparison.OrdinalIgnoreCase)))
    return;

// Skip if request doesn't have authentication
if (context.User.Identity?.IsAuthenticated != true)
    return;

// Validate the CSRF token
await antiforgery.ValidateRequestAsync(context);
```

## Protected Endpoints

All state-changing endpoints are automatically protected:

### Patients Controller
- `POST /api/patients` - Register patient
- `PUT /api/patients/{id}` - Update patient demographics

### Auth Controller
- `POST /api/auth/register` - Register user (exempt - can be unauthenticated)
- `POST /api/auth/login-callback` - Login callback

### Roles Controller
- `PUT /api/roles/{id}/permissions` - Assign permissions

### Audit Controller
- `POST /api/audit/export` - Export audit logs

## Client Integration

### 1. Fetch CSRF Token

Before making state-changing requests, clients must fetch a CSRF token:

```javascript
// Fetch CSRF token
const response = await fetch('/api/auth/csrf-token');
const { token, headerName } = await response.json();
```

### 2. Include Token in Requests

Include the token in the `X-CSRF-Token` header:

```javascript
// Make protected request
await fetch('/api/patients', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'X-CSRF-Token': token,
        'Authorization': `Bearer ${accessToken}`
    },
    credentials: 'include',
    body: JSON.stringify(patientData)
});
```

### 3. Token Cookie

The token is also available in the `XSRF-TOKEN` cookie, which the browser automatically sends:

```javascript
// Alternative: Read token from cookie
function getCsrfTokenFromCookie() {
    const match = document.cookie.match(/XSRF-TOKEN=([^;]+)/);
    return match ? match[1] : null;
}
```

## Security Benefits

1. **Prevents CSRF Attacks**: Attackers cannot forge requests without the valid CSRF token
2. **Session Riding Protection**: Even if an attacker has the session cookie, they cannot make requests without the token
3. **Defense-in-Depth**: Combined with other security measures (CORS, SameSite cookies, HTTPS)
4. **HIPAA Compliance**: Prevents unauthorized state changes that could compromise PHI

## Token Lifecycle

- **Expiration**: Tokens expire after 1 hour
- **Regeneration**: New tokens can be fetched at any time
- **Validation**: Tokens are validated on every state-changing request
- **Security**: Tokens are cryptographically secure and cannot be guessed

## Testing CSRF Protection

### Valid Request (Success)
```bash
# 1. Get CSRF token
TOKEN=$(curl -s http://localhost:5000/api/auth/csrf-token | jq -r .token)

# 2. Make protected request with token
curl -X POST http://localhost:5000/api/patients \
  -H "Content-Type: application/json" \
  -H "X-CSRF-Token: $TOKEN" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d '{"firstName":"John","lastName":"Doe",...}'
```

### Invalid Request (403 Forbidden)
```bash
# Request without CSRF token - should fail
curl -X POST http://localhost:5000/api/patients \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d '{"firstName":"John","lastName":"Doe",...}'

# Expected response: 403 Forbidden
# {
#   "error": "CSRF validation failed",
#   "message": "Invalid or missing CSRF token"
# }
```

## Troubleshooting

### Common Issues

1. **403 Forbidden on POST requests**
   - Ensure CSRF token is included in `X-CSRF-Token` header
   - Verify token is not expired (1 hour lifetime)
   - Check that cookies are being sent (`credentials: 'include'`)

2. **Token not found in cookie**
   - Call `/api/auth/csrf-token` endpoint first
   - Ensure `SameSite` and `Secure` cookie settings match your environment

3. **CORS issues with CSRF**
   - Verify `AllowCredentials()` is set in CORS policy
   - Ensure origin is in `AllowedOrigins` list
   - Check that `withCredentials: true` is set in client requests

## Implementation Checklist

✅ Antiforgery service configured with secure settings
✅ CSRF validation middleware added to pipeline
✅ Token endpoint available at `/api/auth/csrf-token`
✅ Automatic validation for POST, PUT, PATCH, DELETE
✅ Exempt paths configured for login/logout
✅ Only validates authenticated requests
✅ Secure cookie settings (SameSite=Strict, Secure=true)
✅ 1-hour token expiration
✅ Detailed logging for failed validations

## Compliance Notes

### HIPAA Compliance
CSRF protection helps meet HIPAA requirements by:
- **Access Control** (164.312(a)(1)): Prevents unauthorized state changes
- **Audit Controls** (164.312(b)): Failed CSRF validations are logged
- **Transmission Security** (164.312(e)): Requires HTTPS for secure transmission

### OWASP Top 10
This implementation addresses:
- **A01:2021 – Broken Access Control**: Prevents CSRF-based unauthorized access
- **A04:2021 – Insecure Design**: Implements security control at design level
- **A07:2021 – Identification and Authentication Failures**: Validates token in addition to session

## Future Enhancements

1. **Double Submit Cookie Pattern**: Consider implementing as additional defense layer
2. **Token Rotation**: Implement automatic token rotation on sensitive operations
3. **Rate Limiting**: Add rate limiting to token endpoint to prevent token harvesting
4. **SameSite=Lax Fallback**: Consider supporting Lax mode for cross-site navigation scenarios

## References

- [OWASP CSRF Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html)
- [ASP.NET Core Antiforgery Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/anti-request-forgery)
- [HIPAA Security Rule](https://www.hhs.gov/hipaa/for-professionals/security/index.html)

---

**Task Completion**: Task #6 - Enforce CSRF on state-changing endpoints (Jennifer Harris - 8h)
**Status**: ✅ COMPLETED
**Implementation**: Automatic middleware-based CSRF validation for all authenticated state-changing requests
