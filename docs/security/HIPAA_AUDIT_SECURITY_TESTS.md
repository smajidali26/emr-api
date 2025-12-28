# HIPAA Audit Logging - Security Test Plan

## Overview

This document outlines security testing procedures for the EMR HIPAA Audit Logging system with TimescaleDB. All tests must pass before production deployment.

**System Components Under Test:**
- Audit API endpoints (`/api/audit/*`)
- TimescaleDB hypertable and continuous aggregates
- AuditStatisticsService and AuditService
- Admin dashboard UI components

**Compliance Requirements:**
- HIPAA Security Rule (45 CFR Part 164)
- HIPAA Privacy Rule (45 CFR Part 160)
- HITECH Act requirements

---

## 1. Authentication & Authorization Tests

### 1.1 Role-Based Access Control (RBAC)

| Test ID | Test Case | Expected Result | Status |
|---------|-----------|-----------------|--------|
| AUTH-001 | Access `/api/audit` without authentication | 401 Unauthorized | |
| AUTH-002 | Access `/api/audit` with Patient role | 403 Forbidden | |
| AUTH-003 | Access `/api/audit` with Doctor role | 403 Forbidden | |
| AUTH-004 | Access `/api/audit` with Admin role | 200 OK with data | |
| AUTH-005 | Access `/api/audit/compliance/metrics` without Admin | 403 Forbidden | |
| AUTH-006 | Access `/api/audit/storage/stats` without Admin | 403 Forbidden | |
| AUTH-007 | Access `/api/audit/export/stream` without Admin | 403 Forbidden | |
| AUTH-008 | Access user's own activity `/api/audit/users/{id}/activity` | Verify user can only see own data | |

### 1.2 JWT Token Validation

| Test ID | Test Case | Expected Result | Status |
|---------|-----------|-----------------|--------|
| JWT-001 | Request with expired JWT token | 401 Unauthorized | |
| JWT-002 | Request with malformed JWT token | 401 Unauthorized | |
| JWT-003 | Request with JWT signed by wrong key | 401 Unauthorized | |
| JWT-004 | Request with modified JWT claims | 401 Unauthorized | |
| JWT-005 | Request with JWT missing required claims | 401 Unauthorized | |
| JWT-006 | Token refresh during active session | New valid token issued | |

### 1.3 Session Management

| Test ID | Test Case | Expected Result | Status |
|---------|-----------|-----------------|--------|
| SESS-001 | Concurrent sessions from different IPs | Both sessions valid (or policy enforced) | |
| SESS-002 | Session timeout after inactivity | Session invalidated after configured timeout | |
| SESS-003 | Logout invalidates session | Subsequent requests fail with 401 | |

---

## 2. Input Validation & Injection Tests

### 2.1 SQL Injection Prevention

| Test ID | Test Case | Payload | Expected Result | Status |
|---------|-----------|---------|-----------------|--------|
| SQLI-001 | Audit log query with SQL injection in userId | `'; DROP TABLE AuditLogs;--` | Request rejected or sanitized | |
| SQLI-002 | Date range with SQL injection | `2024-01-01' OR '1'='1` | Request rejected or sanitized | |
| SQLI-003 | Event type filter with UNION injection | `Login' UNION SELECT * FROM users--` | Request rejected or sanitized | |
| SQLI-004 | Resource ID with SQL injection | `123; DELETE FROM AuditLogs;` | Request rejected or sanitized | |
| SQLI-005 | Pagination parameters with injection | `pageSize=10; DROP TABLE--` | Request rejected or sanitized | |
| SQLI-006 | TimescaleDB-specific functions injection | `now()' OR extract(epoch from now())>0--` | Request rejected or sanitized | |

### 2.2 Cross-Site Scripting (XSS) Prevention

| Test ID | Test Case | Payload | Expected Result | Status |
|---------|-----------|---------|-----------------|--------|
| XSS-001 | Audit description with script tag | `<script>alert('xss')</script>` | HTML encoded in response | |
| XSS-002 | User name with event handler | `<img onerror="alert(1)" src=x>` | HTML encoded in response | |
| XSS-003 | Resource name with JavaScript URI | `javascript:alert(document.cookie)` | Sanitized or rejected | |
| XSS-004 | Export filename with XSS payload | `report<script>alert(1)</script>.csv` | Sanitized filename | |

### 2.3 Path Traversal Prevention

| Test ID | Test Case | Payload | Expected Result | Status |
|---------|-----------|---------|-----------------|--------|
| PATH-001 | Resource ID with path traversal | `../../../etc/passwd` | Request rejected | |
| PATH-002 | Export path manipulation | `../../sensitive/data` | Request rejected | |
| PATH-003 | User ID with encoded traversal | `%2e%2e%2f%2e%2e%2f` | Request rejected | |

### 2.4 Parameter Tampering

| Test ID | Test Case | Expected Result | Status |
|---------|-----------|-----------------|--------|
| PARAM-001 | Negative page number | Rejected or defaults to 1 | |
| PARAM-002 | Extremely large page size (10000+) | Capped to maximum allowed | |
| PARAM-003 | Future date in date range | Rejected or current date used | |
| PARAM-004 | Date range > 7 years | Handled gracefully | |
| PARAM-005 | Invalid UUID format for IDs | 400 Bad Request | |

---

## 3. Audit Log Integrity Tests

### 3.1 Immutability

| Test ID | Test Case | Expected Result | Status |
|---------|-----------|-----------------|--------|
| INTEG-001 | Attempt to UPDATE audit log record | Operation denied | |
| INTEG-002 | Attempt to DELETE audit log record | Operation denied | |
| INTEG-003 | Attempt to TRUNCATE audit logs table | Operation denied | |
| INTEG-004 | Direct database modification attempt | Triggers alert, operation logged | |

### 3.2 Completeness

| Test ID | Test Case | Expected Result | Status |
|---------|-----------|-----------------|--------|
| COMPL-001 | PHI access generates audit log | Log entry created with all required fields | |
| COMPL-002 | Failed login attempt logged | AccessDenied event recorded | |
| COMPL-003 | Export operation logged | Export event with metadata recorded | |
| COMPL-004 | Bulk operations logged individually | Each record access logged | |
| COMPL-005 | API errors logged | Error details captured (without sensitive data) | |

### 3.3 Accuracy

| Test ID | Test Case | Expected Result | Status |
|---------|-----------|-----------------|--------|
| ACCUR-001 | Timestamp accuracy | Within 1 second of actual time (NTP synced) | |
| ACCUR-002 | User ID matches authenticated user | Correct user recorded | |
| ACCUR-003 | IP address captured correctly | Client IP (considering proxies) | |
| ACCUR-004 | Resource details accurate | Correct resource type and ID | |

---

## 4. Data Protection Tests

### 4.1 Encryption at Rest

| Test ID | Test Case | Expected Result | Status |
|---------|-----------|-----------------|--------|
| ENCR-001 | Database files encrypted | TDE or equivalent enabled | |
| ENCR-002 | Backup files encrypted | Encrypted backup storage | |
| ENCR-003 | Temporary files encrypted | No plaintext temp files | |
| ENCR-004 | Log files don't contain PHI | Sensitive data masked | |

### 4.2 Encryption in Transit

| Test ID | Test Case | Expected Result | Status |
|---------|-----------|-----------------|--------|
| TLS-001 | API requires HTTPS | HTTP redirects to HTTPS or rejected | |
| TLS-002 | TLS 1.2+ enforced | TLS 1.0/1.1 rejected | |
| TLS-003 | Strong cipher suites only | Weak ciphers disabled | |
| TLS-004 | Certificate validation | Invalid certs rejected | |
| TLS-005 | Database connection encrypted | SSL/TLS to PostgreSQL | |

### 4.3 Data Masking

| Test ID | Test Case | Expected Result | Status |
|---------|-----------|-----------------|--------|
| MASK-001 | SSN masked in audit logs | Only last 4 digits visible | |
| MASK-002 | Credit card masked | Only last 4 digits visible | |
| MASK-003 | Password never logged | No password in any log | |
| MASK-004 | API keys not logged | Tokens/keys redacted | |

---

## 5. Access Control Tests

### 5.1 Principle of Least Privilege

| Test ID | Test Case | Expected Result | Status |
|---------|-----------|-----------------|--------|
| PRIV-001 | Database user has minimal permissions | Only SELECT, INSERT on AuditLogs | |
| PRIV-002 | Application service account limited | No DELETE, TRUNCATE permissions | |
| PRIV-003 | Admin users audited | Admin actions logged | |
| PRIV-004 | Superuser access logged | Database superuser actions tracked | |

### 5.2 Segregation of Duties

| Test ID | Test Case | Expected Result | Status |
|---------|-----------|-----------------|--------|
| SEG-001 | Audit log viewers cannot modify logs | Read-only access enforced | |
| SEG-002 | Developers cannot access production logs | Environment separation | |
| SEG-003 | DBAs cannot delete without approval | Requires dual authorization | |

---

## 6. API Security Tests

### 6.1 Rate Limiting

| Test ID | Test Case | Expected Result | Status |
|---------|-----------|-----------------|--------|
| RATE-001 | Exceed 100 requests/minute | 429 Too Many Requests | |
| RATE-002 | Rate limit per user, not just IP | User-specific limiting works | |
| RATE-003 | Export endpoint has stricter limits | Limited concurrent exports | |
| RATE-004 | Rate limit headers present | X-RateLimit-* headers returned | |

### 6.2 CORS Configuration

| Test ID | Test Case | Expected Result | Status |
|---------|-----------|-----------------|--------|
| CORS-001 | Request from unauthorized origin | CORS error, request blocked | |
| CORS-002 | Credentials included only for allowed origins | Access-Control-Allow-Credentials correct | |
| CORS-003 | Preflight OPTIONS requests handled | Correct CORS headers returned | |

### 6.3 Content Security

| Test ID | Test Case | Expected Result | Status |
|---------|-----------|-----------------|--------|
| CONT-001 | Content-Type validation | Only application/json accepted | |
| CONT-002 | Response Content-Type header | Always application/json | |
| CONT-003 | X-Content-Type-Options: nosniff | Header present | |
| CONT-004 | X-Frame-Options header | DENY or SAMEORIGIN | |
| CONT-005 | Strict-Transport-Security header | HSTS enabled | |

---

## 7. TimescaleDB-Specific Security Tests

### 7.1 Hypertable Security

| Test ID | Test Case | Expected Result | Status |
|---------|-----------|-----------------|--------|
| TSDB-001 | Chunk access permissions | Users cannot access chunks directly | |
| TSDB-002 | Compression policy tampering | Policies protected from modification | |
| TSDB-003 | Retention policy modification | Requires elevated privileges | |
| TSDB-004 | Continuous aggregate security | Same permissions as base table | |

### 7.2 Data Retention Compliance

| Test ID | Test Case | Expected Result | Status |
|---------|-----------|-----------------|--------|
| RET-001 | Data retained for 7 years | 2,555-day retention policy active | |
| RET-002 | Premature deletion prevented | Cannot delete before retention period | |
| RET-003 | Retention policy audit | Policy changes logged | |

---

## 8. Penetration Testing Procedures

### 8.1 Automated Scanning

```bash
# OWASP ZAP scan
zap-cli quick-scan --self-contained https://api.example.com/api/audit

# Nikto web scanner
nikto -h https://api.example.com

# SQLMap for SQL injection
sqlmap -u "https://api.example.com/api/audit?fromDate=2024-01-01" --batch

# Nmap for service enumeration
nmap -sV -sC -p 443,5432 api.example.com
```

### 8.2 Manual Testing Checklist

- [ ] Attempt privilege escalation from Patient to Admin
- [ ] Test for IDOR (Insecure Direct Object Reference) vulnerabilities
- [ ] Check for information disclosure in error messages
- [ ] Verify secure cookie attributes (HttpOnly, Secure, SameSite)
- [ ] Test for business logic flaws in date range queries
- [ ] Attempt to bypass rate limiting using multiple IPs
- [ ] Check for sensitive data in browser cache/history
- [ ] Verify secure password/token handling

### 8.3 API Fuzzing

```bash
# Using ffuf for API fuzzing
ffuf -u https://api.example.com/api/audit/FUZZ -w wordlist.txt -H "Authorization: Bearer $TOKEN"

# Parameter fuzzing
ffuf -u "https://api.example.com/api/audit?FUZZ=test" -w params.txt -H "Authorization: Bearer $TOKEN"
```

---

## 9. Security Test Execution

### 9.1 Pre-Test Requirements

- [ ] Test environment isolated from production
- [ ] Test data contains no real PHI
- [ ] Security testing tools installed and configured
- [ ] Penetration testing authorization obtained
- [ ] Incident response team notified

### 9.2 Test Execution Order

1. **Phase 1**: Authentication & Authorization (AUTH-*, JWT-*, SESS-*)
2. **Phase 2**: Input Validation (SQLI-*, XSS-*, PATH-*, PARAM-*)
3. **Phase 3**: Audit Integrity (INTEG-*, COMPL-*, ACCUR-*)
4. **Phase 4**: Data Protection (ENCR-*, TLS-*, MASK-*)
5. **Phase 5**: Access Control (PRIV-*, SEG-*)
6. **Phase 6**: API Security (RATE-*, CORS-*, CONT-*)
7. **Phase 7**: TimescaleDB Security (TSDB-*, RET-*)
8. **Phase 8**: Penetration Testing

### 9.3 Reporting

All findings must be documented with:
- Severity rating (Critical, High, Medium, Low, Informational)
- CVSS score where applicable
- Steps to reproduce
- Evidence (screenshots, logs)
- Recommended remediation
- Remediation verification

---

## 10. Compliance Verification

### 10.1 HIPAA Security Rule Mapping

| HIPAA Requirement | Test Coverage | Status |
|-------------------|---------------|--------|
| §164.312(a)(1) - Access Control | AUTH-*, PRIV-*, SEG-* | |
| §164.312(a)(2)(i) - Unique User ID | AUTH-004, ACCUR-002 | |
| §164.312(a)(2)(iii) - Automatic Logoff | SESS-002 | |
| §164.312(a)(2)(iv) - Encryption | ENCR-*, TLS-* | |
| §164.312(b) - Audit Controls | INTEG-*, COMPL-*, ACCUR-* | |
| §164.312(c)(1) - Integrity | INTEG-001 to INTEG-004 | |
| §164.312(d) - Authentication | JWT-*, AUTH-* | |
| §164.312(e)(1) - Transmission Security | TLS-* | |

### 10.2 Sign-Off Requirements

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Security Lead | | | |
| Compliance Officer | | | |
| HIPAA Privacy Officer | | | |
| IT Director | | | |

---

## Appendix A: Test Data

### Safe Test Payloads

```
# SQL Injection test strings
' OR '1'='1
1; DROP TABLE test--
UNION SELECT NULL,NULL,NULL--
' AND SLEEP(5)--

# XSS test strings
<script>alert('XSS')</script>
<img src=x onerror=alert('XSS')>
javascript:alert('XSS')
<svg onload=alert('XSS')>

# Path traversal test strings
../../../etc/passwd
..%2f..%2f..%2fetc%2fpasswd
....//....//....//etc/passwd
```

### Test User Accounts

| Role | Username | Purpose |
|------|----------|---------|
| Admin | test-admin@example.com | Full access testing |
| Doctor | test-doctor@example.com | Provider access testing |
| Nurse | test-nurse@example.com | Clinical staff testing |
| Patient | test-patient@example.com | Limited access testing |

---

## Appendix B: Tool Configuration

### OWASP ZAP Configuration

```yaml
# zap-config.yaml
env:
  contexts:
    - name: "EMR Audit API"
      urls:
        - "https://api.example.com/api/audit"
      authentication:
        method: "header"
        parameters:
          Authorization: "Bearer ${TOKEN}"
```

### SQLMap Configuration

```bash
sqlmap --url="https://api.example.com/api/audit" \
  --headers="Authorization: Bearer $TOKEN" \
  --level=5 \
  --risk=3 \
  --dbms=postgresql \
  --batch \
  --output-dir=./sqlmap-results
```

---

**Document Version**: 1.0
**Last Updated**: 2024-12-28
**Next Review**: Quarterly
