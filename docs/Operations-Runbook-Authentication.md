# EMR User Authentication System - Operations Runbook

**Document Version:** 1.0
**Last Updated:** 2025-12-28
**Owner:** Platform Engineering Team
**Audience:** On-call Engineers, SREs, DevOps

---

## Table of Contents
1. [System Overview](#1-system-overview)
2. [Daily Operations](#2-daily-operations)
3. [Common Issues & Resolutions](#3-common-issues--resolutions)
4. [Troubleshooting Procedures](#4-troubleshooting-procedures)
5. [Log Locations & Analysis](#5-log-locations--analysis)
6. [Security Incident Response](#6-security-incident-response)
7. [Maintenance Procedures](#7-maintenance-procedures)
8. [Performance Tuning](#8-performance-tuning)
9. [Contact & Escalation](#9-contact--escalation)

---

## 1. System Overview

### 1.1 Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         AUTHENTICATION FLOW                          │
└─────────────────────────────────────────────────────────────────────┘

┌──────────────────┐         ┌──────────────────┐
│   Web Client     │         │  Mobile Client   │
│  (React/Next.js) │         │ (React Native)   │
│                  │         │                  │
│  MSAL Browser    │         │  expo-auth-      │
│  sessionStorage  │         │  session         │
│                  │         │  expo-secure-    │
│                  │         │  store           │
└────────┬─────────┘         └────────┬─────────┘
         │                            │
         │ HTTPS (JWT)                │ HTTPS (JWT + Biometric)
         │                            │
         └────────────┬───────────────┘
                      │
                      ▼
         ┌────────────────────────┐
         │   API Gateway/LB       │
         │  (Rate Limiting)       │
         │  - 100 req/min global  │
         │  - 10 req/5min auth    │
         └────────────┬───────────┘
                      │
                      ▼
         ┌────────────────────────┐
         │   Authentication       │
         │   Middleware           │
         │  ┌──────────────────┐  │
         │  │ CSRF Protection  │  │
         │  │ JWT Validation   │  │
         │  │ Rate Limiter     │  │
         │  │ Audit Logger     │  │
         │  └──────────────────┘  │
         └────────────┬───────────┘
                      │
                      ▼
         ┌────────────────────────┐
         │   .NET Core API        │
         │                        │
         │  ┌──────────────────┐  │
         │  │ JWT Bearer Auth  │  │
         │  │ RBAC + ABAC      │  │
         │  │ Authorization    │  │
         │  └──────────────────┘  │
         └────────────┬───────────┘
                      │
         ┌────────────┴───────────┐
         │                        │
         ▼                        ▼
┌─────────────────┐      ┌─────────────────┐
│  Azure AD B2C   │      │ Azure Key Vault │
│                 │      │                 │
│  - SSO          │      │  - Signing Keys │
│  - MFA Policies │      │  - Certificates │
│  - User Flows   │      │  - Secrets      │
└─────────────────┘      └─────────────────┘
         │
         ▼
┌─────────────────┐
│  User Database  │
│  (SQL Server)   │
└─────────────────┘
```

### 1.2 Key Components

| Component | Technology | Purpose | Health Endpoint |
|-----------|-----------|---------|-----------------|
| Web App | React/Next.js + MSAL | User interface | `/health` |
| Mobile App | React Native + Expo | Mobile interface | N/A (client-side) |
| Backend API | .NET Core 8.0 | Business logic | `/api/health` |
| Identity Provider | Azure AD B2C | Authentication | Azure portal metrics |
| Token Store (Web) | sessionStorage | Token caching | N/A (browser) |
| Token Store (Mobile) | expo-secure-store | Secure token storage | N/A (device) |
| Key Management | Azure Key Vault | Secrets, keys, certs | Azure portal metrics |
| Audit Logging | Application Insights | Security events | Azure portal |

### 1.3 Authentication Flow Summary

1. **User Login**: User credentials sent to Azure AD B2C
2. **MFA Challenge**: B2C enforces MFA policy (SMS/Email/Authenticator/Biometric)
3. **Token Issuance**: B2C issues JWT (access + refresh tokens)
4. **Token Storage**:
   - Web: sessionStorage (cleared on tab close)
   - Mobile: expo-secure-store (encrypted, biometric-protected)
5. **API Authorization**: JWT validated, RBAC/ABAC checks enforced
6. **Token Refresh**: Automatic refresh before expiry (Web: 5min before, Mobile: 10min before)
7. **Audit Logging**: All auth events logged to Application Insights

### 1.4 Critical SLAs

| Metric | Target | Alert Threshold |
|--------|--------|----------------|
| Authentication Success Rate | >99.9% | <99.5% |
| Token Refresh Success Rate | >99.5% | <99.0% |
| API Response Time (p95) | <200ms | >500ms |
| B2C Availability | 99.99% | <99.9% (Azure SLA) |
| Audit Log Ingestion Delay | <30s | >2min |

---

## 2. Daily Operations

### 2.1 Morning Health Checks (9 AM Daily)

Run these checks every morning before business hours peak:

#### Step 1: Check Azure AD B2C Health
```bash
# Azure Portal > Azure AD B2C > Overview
# Check for:
# - Service health alerts
# - Unusual sign-in activity
# - MFA failure spikes
```

**Expected State:**
- No service degradation alerts
- Sign-in success rate >99.5%
- MFA success rate >95%

#### Step 2: Check API Health Endpoint
```bash
curl -X GET https://api.emr.example.com/api/health \
  -H "Accept: application/json"

# Expected Response:
# {
#   "status": "Healthy",
#   "components": {
#     "database": "Healthy",
#     "azureAD": "Healthy",
#     "keyVault": "Healthy",
#     "cache": "Healthy"
#   }
# }
```

**Action if Unhealthy:**
- Check Application Insights for errors
- Verify database connectivity
- Check Key Vault access permissions

#### Step 3: Verify Token Refresh Mechanism
```bash
# Check Application Insights logs for token refresh errors
# Query: traces | where message contains "TokenRefreshFailed"
# Time range: Last 24 hours
# Expected: <10 failures per day
```

#### Step 4: Review Rate Limiting Metrics
```bash
# Application Insights > Metrics
# Query: customMetrics | where name == "RateLimitExceeded"
# Expected: <0.1% of total requests
```

#### Step 5: Check Certificate Expiry
```bash
# Azure Key Vault > Certificates
# Alert if any certificate expires within 30 days
```

### 2.2 Monitoring Dashboards

#### Dashboard 1: Authentication Metrics (Primary)
**Location:** Application Insights > Dashboards > "Auth Metrics"

**Key Metrics:**
- Total login attempts (last 24h)
- Success rate (target: >99.5%)
- MFA success rate (target: >95%)
- Average login time (target: <2s)
- Failed login attempts by reason
- Rate limit hits by endpoint

#### Dashboard 2: Security Events
**Location:** Application Insights > Dashboards > "Security Events"

**Key Metrics:**
- Brute force attempts
- Invalid token attempts
- CSRF validation failures
- Suspicious IP activity
- Concurrent session violations

#### Dashboard 3: Performance Metrics
**Location:** Application Insights > Dashboards > "API Performance"

**Key Metrics:**
- Token validation time (p50, p95, p99)
- API response times by endpoint
- Database query performance
- Key Vault access latency

### 2.3 Automated Alerts Configuration

| Alert | Condition | Severity | Action |
|-------|-----------|----------|--------|
| Auth Success Rate Drop | <99% over 5min | Critical | Page on-call immediately |
| MFA Failure Spike | >20% over 10min | High | Investigate B2C policy |
| Token Refresh Failures | >50 in 5min | High | Check Key Vault connectivity |
| Rate Limit Exceeded | >1000 hits in 1min | Medium | Check for DDoS |
| CSRF Failures | >100 in 5min | High | Potential attack, investigate |
| Certificate Expiry | <30 days | High | Schedule rotation |
| Key Vault Access Denied | >10 in 5min | Critical | Check service principal |

---

## 3. Common Issues & Resolutions

### 3.1 Token Refresh Failures

**Symptoms:**
- Users logged out unexpectedly
- "Session expired" errors
- Application Insights shows `TokenRefreshFailed` errors

**Common Causes:**

#### Cause 1: Refresh Token Expired
```
Error: "AADB2C90080: The provided grant has expired"
```

**Resolution:**
1. Check B2C token lifetime settings:
   ```bash
   # Azure Portal > B2C > User flows > [Your flow] > Token configuration
   # Verify: Refresh token lifetime = 14 days (default)
   ```
2. If users report frequent logouts:
   - Increase refresh token lifetime (max 90 days for web, 1 year for mobile)
   - Verify client app correctly stores refresh tokens
3. For mobile: Check if `expo-secure-store` is accessible (device might require re-auth after OS update)

#### Cause 2: Key Vault Signing Key Rotation
```
Error: "IDX10501: Signature validation failed. Unable to match key"
```

**Resolution:**
1. Check Key Vault access:
   ```bash
   # Azure Portal > Key Vault > Access policies
   # Verify service principal has "Get" permission for keys
   ```
2. Clear JWT validation cache:
   ```bash
   # Restart API instances to reload JWKS
   kubectl rollout restart deployment/emr-api
   ```
3. Verify JWKS endpoint is reachable:
   ```bash
   curl https://[tenant].b2clogin.com/[tenant].onmicrosoft.com/[policy]/discovery/v2.0/keys
   ```

#### Cause 3: Client-Side Token Storage Issues

**Web (sessionStorage):**
```
Error: "Token not found in storage"
```

**Resolution:**
- Verify browser allows sessionStorage (not in incognito mode)
- Check for browser extensions blocking storage
- Ask user to clear browser cache and retry

**Mobile (expo-secure-store):**
```
Error: "SecureStore: Error reading from keychain"
```

**Resolution:**
- Device biometric settings changed (user disabled Face ID/fingerprint)
- Ask user to:
  1. Log out completely
  2. Re-enable biometric authentication in device settings
  3. Log in again (will re-initialize secure store)

### 3.2 B2C Policy Errors

**Symptoms:**
- Login redirects fail
- MFA not triggered
- Custom claims missing from token

#### Issue 1: Policy Configuration Mismatch
```
Error: "AADB2C90091: The user has cancelled entering self-asserted information"
```

**Resolution:**
1. Check B2C user flow configuration:
   ```bash
   # Azure Portal > B2C > User flows > [SignUpSignIn]
   # Verify: MFA enforcement = "Always" or "Conditional"
   ```
2. Verify application redirect URIs:
   ```bash
   # B2C > App registrations > [Your app] > Authentication
   # Web: https://app.emr.example.com/auth/callback
   # Mobile: exp://[your-app-slug]/--/auth/callback
   ```

#### Issue 2: MFA Policy Not Enforced
```
Symptom: Users logging in without MFA challenge
```

**Resolution:**
1. Check Conditional Access policies:
   ```bash
   # Azure AD B2C > Security > Conditional Access
   # Verify policy is enabled and applies to your app
   ```
2. Check user flow MFA settings:
   ```bash
   # B2C > User flows > [Your flow] > Multi-factor authentication
   # Set to: "Required" for all users
   ```
3. Verify MFA claim in token:
   ```bash
   # Decode JWT at jwt.io
   # Check for claim: "amr": ["mfa"]
   ```

### 3.3 Rate Limit Exceeded

**Symptoms:**
- HTTP 429 responses
- "Too Many Requests" errors
- Users unable to authenticate during peak hours

#### Global Rate Limit (100 req/min)
```
Error: "Rate limit exceeded: 100 requests per minute"
```

**Immediate Action:**
1. Check if it's a legitimate spike or attack:
   ```bash
   # Application Insights query:
   requests
   | where timestamp > ago(10m)
   | where resultCode == 429
   | summarize count() by client_IP
   | order by count_ desc
   ```
2. If single IP causing spike (potential attack):
   ```bash
   # Block IP at API Gateway/Firewall level
   az network application-gateway waf-policy custom-rule create \
     --policy-name emr-waf-policy \
     --resource-group emr-rg \
     --name BlockMaliciousIP \
     --priority 10 \
     --rule-type MatchRule \
     --action Block \
     --match-conditions "[{\"matchVariables\":[{\"variableName\":\"RemoteAddr\"}],\"operator\":\"IPMatch\",\"matchValues\":[\"<MALICIOUS_IP>\"]}]"
   ```
3. If legitimate traffic spike:
   - Temporarily increase rate limit (see [Performance Tuning](#8-performance-tuning))
   - Add more API instances to handle load

#### Auth Endpoint Rate Limit (10 req/5min)
```
Error: "Authentication rate limit exceeded: 10 requests per 5 minutes"
```

**Immediate Action:**
1. Check if user is stuck in login loop:
   ```bash
   # Application Insights query:
   traces
   | where message contains "AuthenticationAttempt"
   | where timestamp > ago(10m)
   | where customDimensions.userId == "<USER_ID>"
   | order by timestamp desc
   ```
2. Common causes:
   - **Incorrect credentials**: User repeatedly entering wrong password
     - Resolution: Reset password via B2C self-service
   - **Mobile app bug**: App retrying auth in loop
     - Resolution: Ask user to force-close and restart app
   - **CSRF token mismatch**: Web app retrying due to CSRF failures
     - Resolution: Clear browser cache, restart browser

### 3.4 CSRF Validation Failures

**Symptoms:**
- "Invalid CSRF token" errors
- Authentication fails after successful B2C login
- Works in some browsers but not others

#### Issue 1: Cookie Not Set
```
Error: "CSRF token cookie not found"
```

**Resolution:**
1. Verify cookie settings in API:
   ```csharp
   // Check appsettings.json:
   "CsrfProtection": {
     "CookieName": "X-CSRF-TOKEN",
     "CookieSecure": true,  // Must be true in production
     "CookieSameSite": "Lax", // Or "Strict"
     "CookieHttpOnly": true
   }
   ```
2. Check browser cookie policy:
   - Safari: May block third-party cookies
   - Chrome: Check `chrome://settings/cookies`
3. Verify domain matches:
   - API: `api.emr.example.com`
   - Web: `app.emr.example.com`
   - Cookie domain should be `.emr.example.com` (note the leading dot)

#### Issue 2: Token Mismatch
```
Error: "CSRF token validation failed"
```

**Resolution:**
1. Check if user has multiple tabs open:
   - Each tab might have different CSRF tokens
   - Resolution: Close all tabs, open single tab
2. Check token rotation logic:
   ```bash
   # Application Insights query:
   traces
   | where message contains "CsrfTokenRotated"
   | where timestamp > ago(1h)
   | summarize count() by sessionId
   ```
3. Verify frontend sends token correctly:
   ```javascript
   // Should be in request header:
   headers: {
     'X-CSRF-TOKEN': csrfToken,
     'Content-Type': 'application/json'
   }
   ```

### 3.5 Biometric Auth Issues (Mobile)

**Symptoms:**
- Biometric prompt not showing
- "Authentication failed" after Face ID/Touch ID
- App falls back to password every time

#### Issue 1: Device Not Enrolled
```
Error: "BiometricNotAvailable"
```

**Resolution:**
1. Check device capabilities:
   ```javascript
   // Mobile app should check:
   import * as LocalAuthentication from 'expo-local-authentication';
   const hasHardware = await LocalAuthentication.hasHardwareAsync();
   const isEnrolled = await LocalAuthentication.isEnrolledAsync();
   ```
2. Guide user to enroll:
   - iOS: Settings > Face ID & Passcode
   - Android: Settings > Security > Fingerprint

#### Issue 2: Biometric Changed
```
Error: "BiometricChanged - re-authentication required"
```

**Resolution:**
- Occurs when user adds/removes fingerprint or changes Face ID
- Security measure: Forces re-login with password
- User action required:
  1. Log in with password
  2. Re-enable biometric auth in app settings
  3. Biometric will work for future logins

#### Issue 3: Token Not Accessible After Biometric Auth
```
Error: "SecureStore: Could not decrypt value"
```

**Resolution:**
1. Token encrypted with old biometric key
2. Resolution (data loss - user must re-login):
   ```javascript
   // Clear secure store:
   await SecureStore.deleteItemAsync('access_token');
   await SecureStore.deleteItemAsync('refresh_token');
   // Redirect to login
   ```

### 3.6 Session Timeout Issues

**Symptoms:**
- Users logged out during active use
- "Session expired" mid-transaction
- Inconsistent timeout behavior between web/mobile

#### Issue 1: Premature Timeout
```
Error: "Session expired" (user claims they were active)
```

**Resolution:**
1. Check token lifetimes:
   ```bash
   # B2C > User flows > Token configuration
   # Access token: 60 minutes (default)
   # Refresh token: 14 days (web), 1 year (mobile)
   # Session timeout: 24 hours (configurable)
   ```
2. Verify token refresh logic:
   - **Web**: Should auto-refresh 5 minutes before expiry
   - **Mobile**: Should auto-refresh 10 minutes before expiry
3. Check Application Insights for refresh attempts:
   ```bash
   traces
   | where message contains "TokenRefreshAttempt"
   | where customDimensions.userId == "<USER_ID>"
   | where timestamp > ago(2h)
   ```

#### Issue 2: Session Persists After Logout
```
Symptom: User logs out but can still access API
```

**Resolution:**
1. Check logout implementation:
   - Must clear client-side tokens (sessionStorage/SecureStore)
   - Must revoke refresh token at B2C
   - Must clear server-side session cache
2. Verify B2C logout:
   ```javascript
   // Web (MSAL):
   await msalInstance.logoutRedirect({
     postLogoutRedirectUri: "https://app.emr.example.com"
   });

   // Mobile (expo-auth-session):
   await AuthSession.revokeAsync({
     token: refreshToken
   }, {
     revocationEndpoint: 'https://[tenant].b2clogin.com/[tenant].onmicrosoft.com/[policy]/oauth2/v2.0/logout'
   });
   ```
3. Server-side: Implement token revocation list (if needed for high-security scenarios)

---

## 4. Troubleshooting Procedures

### 4.1 Decision Tree: Authentication Failures

```
User cannot login
       │
       ▼
   [Check B2C Status]
       │
       ├─► B2C Down? ──► Check Azure Status Page
       │                 ├─► Service Issue: Wait for Azure resolution
       │                 └─► No Issue: Proceed below
       │
       ▼
   [Check Error Code]
       │
       ├─► AADB2C90080 (Grant Expired)
       │   └─► Refresh token expired
       │       └─► User must re-login (expected behavior)
       │
       ├─► AADB2C90091 (User Cancelled)
       │   └─► User closed MFA prompt
       │       └─► Ask user to retry and complete MFA
       │
       ├─► AADB2C90118 (Password Reset Required)
       │   └─► Direct user to password reset flow
       │
       ├─► IDX10501 (Signature Validation Failed)
       │   └─► JWT signing key mismatch
       │       ├─► Check Key Vault access
       │       ├─► Restart API to reload JWKS
       │       └─► Verify B2C JWKS endpoint
       │
       ├─► HTTP 429 (Rate Limit)
       │   └─► See [Rate Limit Exceeded](#33-rate-limit-exceeded)
       │
       ├─► HTTP 403 (Forbidden)
       │   └─► Authorization issue (not authentication)
       │       ├─► Check user roles in token
       │       ├─► Verify RBAC policies
       │       └─► Check ABAC resource permissions
       │
       └─► Other Error
           └─► Check Application Insights logs
               └─► Search for userId/correlationId
```

### 4.2 Decision Tree: Token Refresh Failures

```
Token refresh fails
       │
       ▼
   [Check Refresh Token Validity]
       │
       ├─► Token Expired (>14 days old for web, >1 year for mobile)
       │   └─► Expected: User must re-login
       │
       ├─► Token Revoked
       │   └─► Check audit logs for revocation event
       │       ├─► User-initiated logout: Expected
       │       ├─► Admin revocation: Contact security team
       │       └─► Password change: User must re-login
       │
       ├─► Key Vault Access Denied
       │   └─► Check service principal permissions
       │       ├─► Verify Key Vault access policy
       │       ├─► Check if key was rotated
       │       └─► Restart API pods to refresh credentials
       │
       ├─► B2C Endpoint Unreachable
       │   └─► Network connectivity issue
       │       ├─► Check Azure AD B2C status
       │       ├─► Verify DNS resolution
       │       └─► Check firewall rules
       │
       └─► Unknown Error
           └─► Capture logs and escalate
               └─► Include: correlationId, timestamp, userId
```

### 4.3 Step-by-Step: Investigating Failed Logins

**Use this for: "User reports unable to login"**

#### Step 1: Gather Information
Ask user:
- Username/email
- Timestamp of failure (in user's timezone)
- Device type (web browser/mobile app)
- Error message (screenshot if possible)
- Recent password changes?

#### Step 2: Check B2C Sign-In Logs
```bash
# Azure Portal > Azure AD B2C > Audit logs
# Filter:
#   - Activity: Sign-in activity
#   - User: [username from Step 1]
#   - Date range: [timestamp ± 1 hour]
```

Look for:
- `Success` = Login worked, issue is elsewhere (token refresh? authorization?)
- `Failure` = Check failure reason:
  - `InvalidPassword` → Password reset needed
  - `MfaFailed` → MFA issue (SMS not received? Authenticator app problem?)
  - `UserNotFound` → Account doesn't exist or wrong username
  - `AccountLocked` → Too many failed attempts (auto-unlocks after 1 hour)

#### Step 3: Check Application Insights
```bash
# Query 1: Find authentication attempts
traces
| where timestamp between (datetime("<START>") .. datetime("<END>"))
| where message contains "AuthenticationAttempt"
| where customDimensions.username == "<USERNAME>"
| project timestamp, message, customDimensions
| order by timestamp desc

# Query 2: Find related errors
exceptions
| where timestamp between (datetime("<START>") .. datetime("<END>"))
| where customDimensions contains "<USERNAME>"
| project timestamp, type, message, customDimensions
```

#### Step 4: Test Authentication Flow
```bash
# Use Postman or curl to test B2C endpoint directly:

# 1. Get authorization code
# Open in browser:
https://[tenant].b2clogin.com/[tenant].onmicrosoft.com/[policy]/oauth2/v2.0/authorize?
  client_id=[client_id]&
  response_type=code&
  redirect_uri=[redirect_uri]&
  scope=openid offline_access&
  state=12345

# 2. Exchange code for token
curl -X POST https://[tenant].b2clogin.com/[tenant].onmicrosoft.com/[policy]/oauth2/v2.0/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=authorization_code" \
  -d "client_id=[client_id]" \
  -d "client_secret=[client_secret]" \
  -d "code=[code_from_step1]" \
  -d "redirect_uri=[redirect_uri]"

# Expected: 200 OK with access_token and refresh_token
```

#### Step 5: Verify Token Claims
```bash
# Decode access token at jwt.io
# Verify:
#   - "aud": Correct audience (your API client ID)
#   - "iss": Correct issuer (B2C tenant)
#   - "exp": Not expired
#   - "roles": User has expected roles
#   - "amr": Contains "mfa" if MFA is required
```

#### Step 6: Test API Access
```bash
# Use token from Step 4 to call API:
curl -X GET https://api.emr.example.com/api/health \
  -H "Authorization: Bearer <ACCESS_TOKEN>" \
  -H "X-CSRF-TOKEN: <CSRF_TOKEN>"

# Expected: 200 OK
# If 401: JWT validation failed (check signing key)
# If 403: Authorization failed (check roles/permissions)
```

### 4.4 Step-by-Step: Investigating Rate Limit Issues

#### Step 1: Identify Scope of Impact
```bash
# Application Insights query:
requests
| where timestamp > ago(15m)
| where resultCode == 429
| summarize affectedUsers = dcount(user_Id), totalHits = count() by client_IP
| order by totalHits desc
```

Interpretation:
- **Single IP, many users**: Likely shared network (office/hospital) hitting limit
- **Single IP, single user**: User issue (stuck in loop or intentional spam)
- **Many IPs, many users**: System-wide issue (limit too low or DDoS)

#### Step 2: Check Rate Limit Type
```bash
# Query rate limit details:
customMetrics
| where name == "RateLimitExceeded"
| where timestamp > ago(15m)
| summarize count() by tostring(customDimensions.endpoint), tostring(customDimensions.limitType)
```

Limit types:
- `global` = 100 req/min across all endpoints
- `auth` = 10 req/5min on authentication endpoints
- `api` = Custom endpoint-specific limits

#### Step 3: Analyze Request Pattern
```bash
# Check request frequency for affected IP:
requests
| where client_IP == "<IP_FROM_STEP1>"
| where timestamp > ago(1h)
| summarize count() by bin(timestamp, 1m), url
| render timechart
```

Look for:
- **Steady high rate**: Legitimate load or bot
- **Sudden spike**: DDoS or application bug
- **Repeating pattern**: Retry loop in client code

#### Step 4: Take Action Based on Pattern

**Scenario A: Legitimate Traffic Spike**
```bash
# Temporarily increase rate limit (see Section 8.2)
# Or scale out API instances:
kubectl scale deployment/emr-api --replicas=10
```

**Scenario B: Single User Stuck in Loop**
```bash
# 1. Contact user to stop/restart application
# 2. If unresponsive, temporarily block IP:
# (See Section 3.3 for IP blocking command)
# 3. Investigate client-side bug causing loop
```

**Scenario C: DDoS Attack**
```bash
# 1. Enable Azure DDoS Protection (if not already enabled)
# 2. Block malicious IPs at WAF level
# 3. Escalate to security team
# 4. Monitor Application Insights for attack patterns
```

---

## 5. Log Locations & Analysis

### 5.1 Log Sources

| Log Type | Location | Retention | Purpose |
|----------|----------|-----------|---------|
| Application Logs | Application Insights | 90 days | API errors, traces, metrics |
| Audit Logs | Application Insights (custom table) | 7 years (HIPAA) | Security events, access logs |
| B2C Sign-In Logs | Azure AD B2C > Audit Logs | 30 days | Authentication events |
| B2C Audit Logs | Azure AD B2C > Audit Logs | 30 days | Policy changes, admin actions |
| Key Vault Logs | Key Vault > Diagnostic Settings | 90 days | Secret access, key operations |
| API Gateway Logs | Azure Application Gateway > Logs | 30 days | Rate limiting, WAF events |
| Container Logs | Azure Monitor > Container Insights | 30 days | Pod crashes, restarts |

### 5.2 Critical Log Queries

#### Query 1: Recent Authentication Failures
```kusto
// Application Insights > Logs
traces
| where timestamp > ago(1h)
| where severityLevel >= 2  // Warning or higher
| where message contains "Authentication" or message contains "Login"
| where message contains "Failed" or message contains "Error"
| project timestamp, severityLevel, message,
          userId = tostring(customDimensions.userId),
          correlationId = tostring(customDimensions.correlationId),
          errorCode = tostring(customDimensions.errorCode)
| order by timestamp desc
| take 100
```

#### Query 2: Token Refresh Patterns
```kusto
traces
| where timestamp > ago(24h)
| where message contains "TokenRefresh"
| summarize
    attempts = countif(message contains "Attempt"),
    successes = countif(message contains "Success"),
    failures = countif(message contains "Failed")
    by bin(timestamp, 1h)
| extend successRate = (successes * 100.0) / attempts
| render timechart
```

#### Query 3: CSRF Validation Failures
```kusto
traces
| where timestamp > ago(1h)
| where message contains "CsrfValidationFailed"
| summarize count() by
    client_IP = tostring(customDimensions.clientIP),
    userAgent = tostring(customDimensions.userAgent)
| order by count_ desc
```

#### Query 4: Rate Limiting by Endpoint
```kusto
customMetrics
| where timestamp > ago(1h)
| where name == "RateLimitExceeded"
| extend endpoint = tostring(customDimensions.endpoint)
| summarize count() by endpoint, bin(timestamp, 5m)
| render timechart
```

#### Query 5: MFA Success/Failure Rates
```kusto
// B2C Audit Logs (must export to Log Analytics workspace)
AuditLogs
| where TimeGenerated > ago(1h)
| where OperationName == "Sign-in activity"
| extend mfaResult = tostring(parse_json(tostring(AdditionalDetails[0])).value)
| where mfaResult in ("success", "failure")
| summarize
    total = count(),
    failures = countif(mfaResult == "failure")
    by bin(TimeGenerated, 10m)
| extend failureRate = (failures * 100.0) / total
| render timechart
```

#### Query 6: Concurrent Session Violations
```kusto
traces
| where timestamp > ago(1h)
| where message contains "ConcurrentSessionViolation"
| extend userId = tostring(customDimensions.userId),
         sessionId1 = tostring(customDimensions.existingSession),
         sessionId2 = tostring(customDimensions.newSession),
         clientIP = tostring(customDimensions.clientIP)
| project timestamp, userId, sessionId1, sessionId2, clientIP
```

#### Query 7: Suspicious Activity Detection
```kusto
traces
| where timestamp > ago(1h)
| where severityLevel == 3  // Error level
| where message contains any ("InvalidToken", "MalformedToken", "ExpiredToken")
| summarize
    attempts = count(),
    uniqueIPs = dcount(tostring(customDimensions.clientIP))
    by userId = tostring(customDimensions.userId)
| where attempts > 10  // More than 10 failed token attempts in 1 hour
| order by attempts desc
```

### 5.3 Log Analysis Best Practices

#### When Investigating Incidents:

1. **Always use correlation IDs**: Every request has a unique `correlationId` - use it to trace the full request lifecycle
   ```kusto
   union traces, requests, exceptions
   | where customDimensions.correlationId == "<CORRELATION_ID>"
   | order by timestamp asc
   ```

2. **Look at context, not just errors**: Check successful requests before/after the failure
   ```kusto
   traces
   | where customDimensions.userId == "<USER_ID>"
   | where timestamp between (datetime("<INCIDENT_TIME>") - 30m .. datetime("<INCIDENT_TIME>") + 30m)
   | order by timestamp asc
   ```

3. **Check dependencies**: Token validation failures might be due to Key Vault issues
   ```kusto
   dependencies
   | where timestamp > ago(1h)
   | where name contains "KeyVault"
   | where success == false
   | project timestamp, name, duration, resultCode
   ```

4. **Compare with baseline**: Is this error rate abnormal?
   ```kusto
   // Current hour vs same hour yesterday
   traces
   | where message contains "AuthenticationFailed"
   | where timestamp > ago(25h)
   | summarize count() by bin(timestamp, 1h)
   | render timechart
   ```

### 5.4 Setting Up Log Alerts

**Example: Alert on High Authentication Failure Rate**
```kusto
// Alert query (run every 5 minutes)
traces
| where timestamp > ago(5m)
| where message contains "AuthenticationFailed"
| summarize failureCount = count()
| where failureCount > 50  // More than 50 failures in 5 min

// Alert configuration:
// - Frequency: 5 minutes
// - Time window: 5 minutes
// - Severity: High (Sev 2)
// - Action: Email + Page on-call
```

---

## 6. Security Incident Response

### 6.1 Incident Classification

| Severity | Criteria | Response Time | Escalation |
|----------|----------|---------------|------------|
| **Critical (P0)** | Active breach, mass account compromise, B2C outage | Immediate | CISO, VP Engineering |
| **High (P1)** | Individual account compromise, brute force attack | 15 minutes | Security team lead |
| **Medium (P2)** | Suspicious activity, unusual login patterns | 1 hour | On-call engineer |
| **Low (P3)** | Policy violations, audit anomalies | 4 hours | Next business day |

### 6.2 Compromised Token Incident

**Indicators:**
- Token used from unusual IP address
- Token used after user logout
- Multiple concurrent sessions from different geolocations
- API calls for resources user shouldn't access

#### Step 1: Confirm Compromise (5 minutes)
```kusto
// Check token usage pattern
traces
| where timestamp > ago(24h)
| where customDimensions.userId == "<USER_ID>"
| extend clientIP = tostring(customDimensions.clientIP),
         location = tostring(customDimensions.geoLocation)
| summarize count() by clientIP, location
| order by count_ desc
```

Red flags:
- IPs from different countries within short timespan
- User agent strings that don't match user's device
- API calls at unusual times (e.g., 3 AM for day-shift nurse)

#### Step 2: Immediate Containment (2 minutes)
```bash
# 1. Revoke all refresh tokens for user
# Azure Portal > Azure AD B2C > Users > [User] > Sessions > "Revoke all refresh tokens"

# 2. Force password reset
# Azure AD B2C > Users > [User] > Reset password > Check "Require user to change password at next sign-in"

# 3. Block user temporarily (if active attack)
# Azure AD B2C > Users > [User] > Block sign-in: Yes
```

#### Step 3: Audit Impact (15 minutes)
```kusto
// Find all API calls made with compromised token
traces
| where timestamp > ago(7d)  // Look back 7 days
| where customDimensions.userId == "<USER_ID>"
| where customDimensions.clientIP == "<SUSPICIOUS_IP>"
| project timestamp, operation = message,
          resourceAccessed = tostring(customDimensions.resource),
          action = tostring(customDimensions.action)
| order by timestamp desc
```

Document:
- What data was accessed?
- Was any data modified or deleted?
- Were any sensitive operations performed (e.g., prescription changes)?

#### Step 4: User Communication (30 minutes)
Template email:
```
Subject: Security Alert - Account Activity Detected

Dear [User Name],

We detected unusual activity on your EMR account on [Date] at [Time].
As a precaution, we have:

1. Logged you out of all devices
2. Required a password reset
3. Enabled additional security monitoring

Please:
- Reset your password immediately using the link below
- Review your recent account activity
- Report any unrecognized actions

If you did not initiate this activity, please contact the security team
immediately at security@emr.example.com or call [PHONE].

[Password Reset Link]

Thank you,
EMR Security Team
```

#### Step 5: Post-Incident Review (24 hours)
- How was token compromised? (phishing? malware? shoulder surfing?)
- Were security controls adequate?
- What preventive measures should be implemented?
- Update incident response playbook

### 6.3 Brute Force Attack

**Indicators:**
- High rate of failed login attempts for single user
- Multiple users targeted from same IP
- Sequential password attempts (password spraying)

#### Detection Query:
```kusto
// Find brute force attempts (>10 failures in 5 min)
traces
| where timestamp > ago(15m)
| where message contains "AuthenticationFailed"
| extend userId = tostring(customDimensions.userId),
         clientIP = tostring(customDimensions.clientIP)
| summarize
    attempts = count(),
    uniqueUsers = dcount(userId)
    by clientIP, bin(timestamp, 5m)
| where attempts > 10
| order by attempts desc
```

#### Response Actions:

**Automated (already configured):**
- Account lockout after 5 failed attempts (1-hour cooldown)
- Rate limiting: 10 auth attempts per 5 minutes per IP

**Manual Response:**
1. **Block attacking IP** (if confirmed malicious):
   ```bash
   # Add to WAF block list
   az network application-gateway waf-policy custom-rule create \
     --policy-name emr-waf-policy \
     --resource-group emr-rg \
     --name BlockBruteForceIP \
     --priority 20 \
     --rule-type MatchRule \
     --action Block \
     --match-conditions "[{\"matchVariables\":[{\"variableName\":\"RemoteAddr\"}],\"operator\":\"IPMatch\",\"matchValues\":[\"<ATTACKER_IP>\"]}]"
   ```

2. **Notify affected users** (if accounts targeted):
   - Email warning about attack attempt
   - Recommend password change if using weak password
   - Enable MFA if not already enabled

3. **Increase monitoring**:
   - Lower alert threshold temporarily
   - Watch for attacker IP changes (distributed attack)

### 6.4 Privilege Escalation Attempt

**Indicators:**
- User attempting to access resources above their role
- Token claims modified (should be impossible if validation works)
- Horizontal privilege escalation (accessing other users' data)

#### Detection Query:
```kusto
traces
| where timestamp > ago(1h)
| where message contains "AuthorizationFailed"
| extend userId = tostring(customDimensions.userId),
         attemptedRole = tostring(customDimensions.requiredRole),
         actualRole = tostring(customDimensions.userRole),
         resource = tostring(customDimensions.resource)
| where attemptedRole != actualRole  // Role mismatch
| summarize attempts = count() by userId, attemptedRole, actualRole
| where attempts > 5  // More than 5 attempts = suspicious
```

#### Response Actions:
1. **Immediate**: Block user account (potential compromise)
2. **Investigate**: Review user's recent activity
3. **Audit**: Check if RBAC/ABAC policies are correctly configured
4. **Escalate**: Contact security team - may indicate vulnerability

### 6.5 Insider Threat Detection

**Indicators:**
- Mass data exports
- Access to many patient records in short time
- Access to records outside normal work area/department
- Login from personal device (if not allowed)

#### Detection Query:
```kusto
traces
| where timestamp > ago(1h)
| where message contains "DataAccess"
| extend userId = tostring(customDimensions.userId),
         patientId = tostring(customDimensions.patientId)
| summarize
    uniquePatients = dcount(patientId),
    totalAccesses = count()
    by userId, bin(timestamp, 10m)
| where uniquePatients > 20  // Accessing >20 patients in 10 min
| order by uniquePatients desc
```

#### Response Actions:
1. **Don't alert the user** (may be malicious insider)
2. **Shadow monitoring**: Increase logging for this user
3. **Escalate to CISO**: Potential data exfiltration
4. **Preserve evidence**: Don't delete logs
5. **Legal/HR involvement**: May require formal investigation

### 6.6 Incident Response Checklist

**Use this during active incident:**

- [ ] **Identify** incident type and severity
- [ ] **Contain** threat (block IP, revoke tokens, disable account)
- [ ] **Notify** appropriate stakeholders (security team, CISO, legal)
- [ ] **Preserve** logs and evidence (export to secure storage)
- [ ] **Investigate** root cause and attack vector
- [ ] **Remediate** vulnerability or configuration issue
- [ ] **Recover** affected systems/accounts
- [ ] **Document** incident in security incident tracker
- [ ] **Communicate** with affected users (if applicable)
- [ ] **Review** and update playbooks based on lessons learned

---

## 7. Maintenance Procedures

### 7.1 Certificate Rotation

**Frequency:** Every 12 months (or when expiry < 30 days)
**Downtime:** Zero-downtime if done correctly
**Risk Level:** High (can break authentication if misconfigured)

#### Pre-Rotation Checklist:
- [ ] Backup current certificate from Key Vault
- [ ] Verify new certificate is valid and matches domain
- [ ] Schedule during low-traffic window (2-4 AM)
- [ ] Notify team in advance (72 hours notice)

#### Step 1: Generate New Certificate (T-0 hours)
```bash
# Option A: Azure Key Vault managed certificate
az keyvault certificate create \
  --vault-name emr-keyvault \
  --name emr-auth-cert-2026 \
  --policy @cert-policy.json

# Option B: Import existing certificate
az keyvault certificate import \
  --vault-name emr-keyvault \
  --name emr-auth-cert-2026 \
  --file /path/to/certificate.pfx \
  --password "<CERT_PASSWORD>"
```

#### Step 2: Update B2C Policy to Use New Certificate
```bash
# Azure Portal > Azure AD B2C > Policy keys > Add
# Name: B2C_1A_AuthCert2026
# Options: Upload
# Upload: [Your certificate]
# Key usage: Signature

# Update custom policy XML:
# <Key Id="B2C_1A_AuthCert2026" StorageReferenceId="B2C_1A_AuthCert2026" />
```

#### Step 3: Deploy Updated API Configuration
```bash
# Update appsettings.json (or environment variable):
{
  "AzureAdB2C": {
    "SigningCertificateName": "emr-auth-cert-2026"
  }
}

# Deploy to staging first:
kubectl set env deployment/emr-api-staging \
  AZURE_SIGNING_CERT_NAME=emr-auth-cert-2026

# Verify staging works (run test suite)
# Then deploy to production:
kubectl set env deployment/emr-api \
  AZURE_SIGNING_CERT_NAME=emr-auth-cert-2026
```

#### Step 4: Monitor for Issues (T+1 hour)
```kusto
// Check for signature validation errors
traces
| where timestamp > ago(1h)
| where message contains "IDX10501" or message contains "SignatureValidation"
| summarize count() by message
```

**If errors found:**
- Rollback immediately to old certificate
- Investigate certificate mismatch
- Re-verify certificate installation

#### Step 5: Decommission Old Certificate (T+7 days)
```bash
# Wait 7 days to ensure all clients refreshed tokens
# Then disable old certificate:
az keyvault certificate set-attributes \
  --vault-name emr-keyvault \
  --name emr-auth-cert-2025 \
  --enabled false

# Delete after 30 days (if no issues reported)
az keyvault certificate delete \
  --vault-name emr-keyvault \
  --name emr-auth-cert-2025
```

### 7.2 Key Rotation (Key Vault Signing Keys)

**Frequency:** Every 90 days (compliance requirement)
**Downtime:** None (automatic key rollover)
**Risk Level:** Medium

#### Automatic Rotation (Recommended):
```bash
# Enable automatic rotation in Key Vault:
az keyvault key rotation-policy update \
  --vault-name emr-keyvault \
  --name jwt-signing-key \
  --value '{
    "lifetimeActions": [
      {
        "trigger": {
          "timeBeforeExpiry": "P30D"
        },
        "action": {
          "type": "Rotate"
        }
      }
    ],
    "attributes": {
      "expiryTime": "P90D"
    }
  }'
```

#### Manual Rotation:
```bash
# 1. Create new key version:
az keyvault key create \
  --vault-name emr-keyvault \
  --name jwt-signing-key \
  --kty RSA \
  --size 2048 \
  --ops sign verify

# 2. No API restart needed - JWKS endpoint automatically includes all active key versions
# 3. Verify new key is in JWKS:
curl https://[tenant].b2clogin.com/[tenant].onmicrosoft.com/[policy]/discovery/v2.0/keys

# 4. After 7 days, disable old key version:
az keyvault key set-attributes \
  --vault-name emr-keyvault \
  --name jwt-signing-key \
  --version <OLD_VERSION> \
  --enabled false
```

### 7.3 B2C Policy Updates

**Frequency:** As needed (feature updates, security patches)
**Downtime:** None if using versioned policies
**Risk Level:** High (can break entire authentication flow)

#### Best Practices:
1. **Always use versioned policies**: `B2C_1A_SignUpSignIn_v2` not `B2C_1A_SignUpSignIn`
2. **Test in separate tenant**: Use B2C dev tenant for testing
3. **Deploy during maintenance window**: Even though no downtime, deploy at low-traffic time
4. **Have rollback plan**: Keep previous policy version active

#### Step 1: Update Policy XML
```xml
<!-- Example: Adding new claim to token -->
<OutputClaims>
  <OutputClaim ClaimTypeReferenceId="objectId" PartnerClaimType="sub"/>
  <OutputClaim ClaimTypeReferenceId="email" />
  <OutputClaim ClaimTypeReferenceId="displayName" />
  <!-- NEW: Add department claim -->
  <OutputClaim ClaimTypeReferenceId="department" />
</OutputClaims>
```

#### Step 2: Upload to B2C
```bash
# Using Azure AD B2C Custom Policy Upload Tool
# Or manual upload via portal:
# Azure AD B2C > Identity Experience Framework > Upload custom policy
```

#### Step 3: Update Application Configuration
```javascript
// Web app (MSAL) - update scopes if needed:
const loginRequest = {
  scopes: ["openid", "profile", "email", "offline_access"],
  extraQueryParameters: {
    p: "B2C_1A_SignUpSignIn_v2"  // New policy version
  }
};
```

#### Step 4: Gradual Rollout (Canary Deployment)
```bash
# Route 10% of traffic to new policy:
# - Update 10% of API instances to expect new claims
# - Monitor for errors
# - Increase to 50%, then 100% over 24 hours
```

#### Step 5: Monitor Impact
```kusto
traces
| where timestamp > ago(1h)
| where message contains "PolicyVersion"
| extend policyVersion = tostring(customDimensions.policyVersion)
| summarize requests = count(), errors = countif(severityLevel >= 3) by policyVersion
```

### 7.4 Cache Clearing Procedures

**When to clear cache:**
- After key rotation (if cache not auto-invalidating)
- After B2C policy update
- When debugging token validation issues

#### Clear JWKS Cache (API side):
```bash
# Option 1: Restart API pods (forces JWKS refresh)
kubectl rollout restart deployment/emr-api

# Option 2: Call cache invalidation endpoint (if implemented)
curl -X POST https://api.emr.example.com/admin/cache/clear \
  -H "Authorization: Bearer <ADMIN_TOKEN>" \
  -d '{"cacheType": "jwks"}'

# Verify cache cleared:
kubectl logs -l app=emr-api --tail=100 | grep "JwksCache"
# Should see: "JwksCache cleared" or "Reloading JWKS from endpoint"
```

#### Clear Client-Side Token Cache:

**Web (sessionStorage):**
```javascript
// Users must clear manually:
// - Log out and log back in (clears sessionStorage)
// - Or hard refresh browser (Ctrl+Shift+R)
```

**Mobile (expo-secure-store):**
```javascript
// Users must:
// - Log out from app settings
// - Or force-close and reopen app (triggers token validation)
```

#### Clear Rate Limit Cache:
```bash
# If using Redis for rate limiting:
redis-cli FLUSHDB

# If using in-memory cache:
kubectl rollout restart deployment/emr-api
```

### 7.5 Emergency Maintenance Procedures

**Scenario: Critical security vulnerability in auth system**

#### Immediate Actions (T+0 min):
```bash
# 1. Enable maintenance mode (reject all new logins):
kubectl set env deployment/emr-api MAINTENANCE_MODE=true

# 2. Notify users via status page:
# "Authentication system undergoing emergency maintenance.
#  Existing sessions will continue. New logins temporarily disabled."

# 3. Preserve existing sessions (don't revoke tokens)
```

#### Apply Fix (T+15 min):
```bash
# 1. Deploy hotfix:
kubectl set image deployment/emr-api \
  emr-api=emrregistry.azurecr.io/emr-api:hotfix-auth-v1.2.3

# 2. Monitor rollout:
kubectl rollout status deployment/emr-api

# 3. Verify fix:
# Run smoke tests on authentication endpoints
```

#### Resume Normal Operations (T+30 min):
```bash
# 1. Disable maintenance mode:
kubectl set env deployment/emr-api MAINTENANCE_MODE=false

# 2. Monitor error rates:
# Should return to baseline within 5 minutes

# 3. Update status page:
# "Authentication system restored. All services operational."
```

---

## 8. Performance Tuning

### 8.1 Token Cache TTL Optimization

**Goal:** Balance security vs performance

#### Current Configuration:
```json
{
  "TokenCaching": {
    "AccessTokenTTL": "60m",      // 60 minutes
    "RefreshTokenTTL": "14d",     // 14 days (web), 365d (mobile)
    "JwksCacheTTL": "24h",        // 24 hours
    "ValidatedTokenCacheTTL": "5m" // 5 minutes (in-memory cache)
  }
}
```

#### Tuning Recommendations:

**Access Token TTL:**
- **Default:** 60 minutes
- **High security environments:** 15-30 minutes (more frequent rotation)
- **Performance-critical:** 120 minutes (fewer refresh calls, but higher risk)
- **Trade-off:** Shorter = more secure, but more refresh overhead

**Refresh Token TTL:**
- **Web:** 14 days (users log in daily, don't need long-lived tokens)
- **Mobile:** 365 days (users expect to stay logged in)
- **High security:** 7 days (force re-authentication weekly)

**JWKS Cache TTL:**
- **Default:** 24 hours
- **During key rotation:** 1 hour (faster propagation)
- **Stable environment:** 48 hours (reduce B2C calls)

#### Monitoring Impact:
```kusto
// Track token refresh rate:
traces
| where timestamp > ago(1h)
| where message contains "TokenRefresh"
| summarize refreshes = count() by bin(timestamp, 5m)
| render timechart

// Expected: ~5-10 refreshes per minute per 1000 active users (with 60min TTL)
// If too high: Increase access token TTL
// If too low: Tokens might be expiring before refresh logic runs
```

### 8.2 Rate Limit Tuning

**Goal:** Prevent abuse while allowing legitimate traffic

#### Current Limits:
```json
{
  "RateLimiting": {
    "Global": {
      "PermitLimit": 100,
      "Window": "1m"
    },
    "Authentication": {
      "PermitLimit": 10,
      "Window": "5m"
    },
    "API": {
      "PermitLimit": 60,
      "Window": "1m"
    }
  }
}
```

#### Tuning Process:

**Step 1: Analyze Current Traffic Patterns**
```kusto
requests
| where timestamp > ago(7d)
| summarize
    p50 = percentile(itemCount, 50),
    p95 = percentile(itemCount, 95),
    p99 = percentile(itemCount, 99),
    max = max(itemCount)
    by bin(timestamp, 1m)
| summarize avg(p95), avg(p99), max(max)

// Result example:
// p95: 85 req/min
// p99: 120 req/min
// max: 200 req/min
```

**Step 2: Set Limits Based on Percentiles**
- **Global limit:** Set at p99 + 20% buffer = 120 * 1.2 = 144 req/min
- **Auth limit:** Analyze separately (auth endpoints have different pattern)

**Step 3: Implement and Monitor**
```bash
# Update configuration:
kubectl set env deployment/emr-api \
  RATE_LIMIT_GLOBAL=144 \
  RATE_LIMIT_WINDOW=1m

# Monitor for 429 errors:
# Should be <0.1% of total requests
```

**Step 4: Gradual Adjustment**
- Week 1: Monitor 429 rate
- Week 2: If 429 rate <0.01%, limits are good
- Week 2: If 429 rate >0.5%, increase limits by 20%
- Repeat until balanced

#### Per-User Rate Limits (Advanced):
```json
{
  "RateLimiting": {
    "PerUser": {
      "PermitLimit": 30,
      "Window": "1m"
    }
  }
}
```

Benefits:
- Prevents single user from consuming all quota
- Limits brute force attacks
- Doesn't penalize other users

### 8.3 Database Query Optimization

**Common bottleneck:** Role/permission lookups during authorization

#### Current Flow:
```
1. JWT validated (fast - in-memory cache)
2. User roles fetched from database (SLOW if not cached)
3. Resource permissions checked (SLOW if not cached)
4. Access granted/denied
```

#### Optimization 1: Cache User Roles in JWT
```json
// Include roles in token claims (set in B2C policy):
{
  "sub": "user123",
  "email": "nurse@hospital.com",
  "roles": ["Nurse", "EmergencyStaff"],  // Cached for 60min
  "department": "ER"
}
```

**Benefits:**
- No database lookup for roles
- Authorization check is pure in-memory operation

**Trade-off:**
- Role changes take up to 60min to propagate (token TTL)
- Larger token size (not an issue unless 50+ roles)

#### Optimization 2: Cache Permission Checks
```csharp
// Cache permission check results:
var cacheKey = $"permission:{userId}:{resource}:{action}";
var allowed = await cache.GetOrSetAsync(cacheKey,
    async () => await CheckPermissionInDatabase(userId, resource, action),
    TimeSpan.FromMinutes(5));  // 5-minute cache
```

**Monitoring:**
```kusto
dependencies
| where timestamp > ago(1h)
| where type == "SQL"
| where name contains "GetUserRoles" or name contains "CheckPermission"
| summarize count(), avg(duration) by name
| order by count_ desc

// Goal: <10ms average duration, <100 calls per minute (rest should be cached)
```

### 8.4 Mobile App Performance Tuning

**Issue:** Mobile apps have slower network, battery constraints

#### Optimization 1: Aggressive Token Caching
```javascript
// Cache tokens in memory + secure-store:
let inMemoryTokenCache = null;

async function getAccessToken() {
  // 1. Check in-memory cache first (fastest)
  if (inMemoryTokenCache && !isExpiringSoon(inMemoryTokenCache)) {
    return inMemoryTokenCache;
  }

  // 2. Check secure-store (slower but persistent)
  const storedToken = await SecureStore.getItemAsync('access_token');
  if (storedToken && !isExpiringSoon(storedToken)) {
    inMemoryTokenCache = storedToken;
    return storedToken;
  }

  // 3. Refresh from B2C (slowest)
  const newToken = await refreshTokenFromB2C();
  await SecureStore.setItemAsync('access_token', newToken);
  inMemoryTokenCache = newToken;
  return newToken;
}
```

#### Optimization 2: Prefetch Token Refresh
```javascript
// Refresh token in background before expiry:
useEffect(() => {
  const interval = setInterval(async () => {
    const token = await getAccessToken();
    const expiresIn = getTokenExpirySeconds(token);

    if (expiresIn < 10 * 60) {  // Less than 10 minutes left
      await refreshTokenInBackground();  // Don't wait for user action
    }
  }, 60 * 1000);  // Check every minute

  return () => clearInterval(interval);
}, []);
```

#### Optimization 3: Reduce Biometric Prompts
```javascript
// Only prompt for biometric on:
// 1. Initial login
// 2. After app backgrounded for >15 minutes
// 3. High-security actions (e.g., viewing patient records)

const requireBiometric = async (action) => {
  const lastAuth = await SecureStore.getItemAsync('last_biometric_auth');
  const timeSinceAuth = Date.now() - parseInt(lastAuth);

  if (timeSinceAuth < 15 * 60 * 1000) {  // Less than 15 min
    return true;  // Skip biometric, use cached auth
  }

  const result = await LocalAuthentication.authenticateAsync({
    promptMessage: `Authenticate to ${action}`,
  });

  if (result.success) {
    await SecureStore.setItemAsync('last_biometric_auth', Date.now().toString());
  }

  return result.success;
};
```

---

## 9. Contact & Escalation

### 9.1 On-Call Rotation

**Primary On-Call Engineer:**
- **Name:** [To be filled]
- **Phone:** [To be filled]
- **Slack:** @oncall-platform
- **Hours:** 24/7 rotation (1 week shifts)

**Backup On-Call Engineer:**
- **Name:** [To be filled]
- **Phone:** [To be filled]
- **Escalate to backup if:** Primary non-responsive for 15 minutes

### 9.2 Escalation Matrix

| Severity | Initial Response | Escalate After | Escalate To |
|----------|------------------|----------------|-------------|
| P0 (Critical) | On-call engineer (immediate) | 15 minutes | Engineering Manager + Security Lead |
| P1 (High) | On-call engineer (15 min) | 1 hour | Engineering Manager |
| P2 (Medium) | On-call engineer (1 hour) | 4 hours | Team Lead |
| P3 (Low) | Next business day | N/A | Email to team |

### 9.3 External Contacts

#### Microsoft Azure Support:
- **B2C Issues:** Open ticket via Azure Portal
- **Severity A (Critical):** 1-hour response SLA
- **Severity B (High):** 4-hour response SLA
- **Phone:** 1-800-642-7676 (US)

#### Security Incidents:
- **CISO:** [To be filled]
- **Security Team Email:** security@emr.example.com
- **Pager:** [To be filled]
- **Slack:** #security-incidents (private channel)

#### Legal/Compliance (for data breaches):
- **Legal Team:** legal@emr.example.com
- **Privacy Officer:** [To be filled]
- **Required for:** PHI exposure, HIPAA violations

### 9.4 Incident Communication Templates

#### Template 1: P0 Incident Alert
```
Subject: [P0] Authentication System Outage

SEVERITY: Critical (P0)
STATUS: Investigating
IMPACT: Users unable to login. Estimated XX,XXX users affected.
STARTED: [Timestamp]

ACTIONS TAKEN:
- [ ] Checked Azure AD B2C status: [Status]
- [ ] Reviewed Application Insights: [Findings]
- [ ] Attempted restart: [Result]

NEXT STEPS:
- [Action 1]
- [Action 2]

ESCALATED TO: [Names]
INCIDENT COMMANDER: [Name]

Updates will be provided every 15 minutes.
```

#### Template 2: Incident Resolution
```
Subject: [RESOLVED] Authentication System Outage

SEVERITY: Critical (P0)
STATUS: Resolved
DURATION: [Start time] to [End time] ([Duration])
IMPACT: [Number] users affected

ROOT CAUSE:
[Brief explanation]

RESOLUTION:
[What fixed it]

PREVENTIVE MEASURES:
- [ ] [Action 1]
- [ ] [Action 2]

POST-INCIDENT REVIEW:
Scheduled for [Date/Time] in [Meeting Link]

Thank you to [Team members] for rapid response.
```

### 9.5 Runbook Maintenance

**This runbook should be reviewed and updated:**
- **After every P0/P1 incident:** Update troubleshooting procedures based on learnings
- **Quarterly:** Review and test all procedures
- **After major changes:** System upgrades, architecture changes
- **Annually:** Full audit and refresh

**Document Owner:** Platform Engineering Team
**Last Reviewed:** 2025-12-28
**Next Review Due:** 2026-03-28

---

## Appendix: Quick Reference Commands

### Health Checks
```bash
# API health
curl https://api.emr.example.com/api/health

# Check B2C JWKS endpoint
curl https://[tenant].b2clogin.com/[tenant].onmicrosoft.com/[policy]/discovery/v2.0/keys

# Check Key Vault access
az keyvault secret show --vault-name emr-keyvault --name test-secret
```

### Emergency Actions
```bash
# Restart API (clears cache, reloads config)
kubectl rollout restart deployment/emr-api

# Enable maintenance mode
kubectl set env deployment/emr-api MAINTENANCE_MODE=true

# Block IP address
az network application-gateway waf-policy custom-rule create \
  --policy-name emr-waf-policy --resource-group emr-rg \
  --name BlockIP --priority 100 --action Block \
  --match-conditions "[{\"matchVariables\":[{\"variableName\":\"RemoteAddr\"}],\"operator\":\"IPMatch\",\"matchValues\":[\"<IP>\"]}]"

# Revoke user tokens (Azure Portal or API call)
# Azure Portal > Azure AD B2C > Users > [User] > Sessions > Revoke
```

### Monitoring Queries
```kusto
// Authentication failures (last hour)
traces | where timestamp > ago(1h) | where message contains "AuthenticationFailed" | summarize count() by tostring(customDimensions.errorCode)

// Token refresh failures
traces | where timestamp > ago(1h) | where message contains "TokenRefreshFailed" | project timestamp, customDimensions

// Rate limit hits
customMetrics | where timestamp > ago(1h) | where name == "RateLimitExceeded" | summarize count() by bin(timestamp, 5m)

// CSRF failures
traces | where timestamp > ago(1h) | where message contains "CsrfValidationFailed" | summarize count() by tostring(customDimensions.clientIP)
```

---

**End of Runbook**

*For questions or issues with this runbook, contact: platform-team@emr.example.com*
