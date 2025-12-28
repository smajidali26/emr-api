# Disaster Recovery Testing - User Authentication System

## Overview

This document provides comprehensive disaster recovery (DR) testing procedures for the EMR User Authentication system powered by Azure AD B2C. These procedures are designed to validate the system's resilience, recovery capabilities, and compliance with HIPAA Business Continuity and Disaster Recovery requirements (45 CFR 164.308(a)(7)).

### Purpose

- Validate authentication system recovery procedures under various failure scenarios
- Ensure Recovery Time Objective (RTO) and Recovery Point Objective (RPO) targets are met
- Verify business continuity for critical authentication services
- Maintain compliance with HIPAA DR requirements
- Document lessons learned and improve recovery procedures

### Scope

This DR testing plan covers:

- **Web Application**: React/Next.js with MSAL Browser (Microsoft Authentication Library)
- **Mobile Application**: React Native/Expo with expo-auth-session
- **Backend API**: .NET with Microsoft Identity Web
- **Identity Provider**: Azure AD B2C (multi-region deployment)
- **Supporting Services**: Azure Key Vault, SQL Database, Application Insights

### Out of Scope

- Application data recovery (covered in separate DR plan)
- Infrastructure-level disaster recovery (covered by Azure platform)
- Third-party service integrations (separate DR plans)

### Compliance Requirements

- **HIPAA 45 CFR 164.308(a)(7)(ii)(B)**: Disaster Recovery Plan
- **HIPAA 45 CFR 164.308(a)(7)(ii)(C)**: Emergency Mode Operation Plan
- **HIPAA 45 CFR 164.308(a)(7)(ii)(E)**: Testing and Revision Procedures

---

## Credential Management for DR Procedures

### Emergency Credential Retrieval

On-call engineers must be able to retrieve credentials at 3 AM. Use these methods in order of preference:

#### Method 1: Azure Key Vault (Primary - Requires Azure RBAC)

```bash
# Authenticate to Azure (use your organizational account)
az login

# Retrieve B2C client secret
CLIENT_SECRET=$(az keyvault secret show \
  --vault-name emr-keyvault-prod \
  --name b2c-client-secret \
  --query value -o tsv)

# Retrieve B2C client ID
CLIENT_ID=$(az keyvault secret show \
  --vault-name emr-keyvault-prod \
  --name b2c-client-id \
  --query value -o tsv)

# Verify retrieval
echo "Client ID: ${CLIENT_ID:0:8}..." # Show only first 8 chars
```

#### Method 2: Service Principal Authentication (For CI/CD and automation)

```bash
# Service principal credentials stored in secure location
# Contact: security-team@emr-healthcare.com for access

az login --service-principal \
  --username $AZURE_CLIENT_ID \
  --password $AZURE_CLIENT_SECRET \
  --tenant $AZURE_TENANT_ID

# Then use Method 1 commands to retrieve secrets
```

#### Method 3: Break-Glass Emergency Access

**ONLY use when Methods 1-2 fail and system is down:**

1. Contact Security On-Call: [See Contact List section]
2. Break-glass credentials location: `https://emr-breakglass.1password.com/`
3. Vault name: `EMR Production Emergency`
4. Two-person authorization required for break-glass access
5. All break-glass access is logged and audited

### Required Environment Variables for DR Procedures

```bash
# Set these before running DR procedures
export CLIENT_ID="<from-keyvault>"
export CLIENT_SECRET="<from-keyvault>"
export AZURE_TENANT_ID="<your-tenant-id>"
export AZURE_SUBSCRIPTION_ID="<your-subscription-id>"

# Verify variables are set
env | grep -E "CLIENT_|AZURE_" | cut -d'=' -f1
```

### Post-DR Credential Rotation

After any DR event using break-glass credentials:
1. Rotate all credentials accessed during DR within 24 hours
2. Log credential access in HIPAA audit system
3. Review break-glass access log with security team

---

## Recovery Objectives

### Recovery Time Objective (RTO)

**Target: 4 hours** - Maximum acceptable downtime for authentication services

| Service Component | RTO Target | Priority |
|-------------------|------------|----------|
| Azure AD B2C Authentication | 1 hour | P0 - Critical |
| Token Validation Service | 2 hours | P0 - Critical |
| User Profile API | 4 hours | P1 - High |
| Session Management | 2 hours | P1 - High |
| MFA Services | 1 hour | P0 - Critical |

### Recovery Point Objective (RPO)

**Target: 1 hour** - Maximum acceptable data loss for user authentication data

| Data Type | RPO Target | Backup Frequency | Replication |
|-----------|------------|------------------|-------------|
| User Identities (Azure AD B2C) | 0 minutes | Continuous | Multi-region active-active |
| User Roles & Permissions | 15 minutes | Real-time sync | Active-passive (geo-replicated) |
| Session State | 5 minutes | Redis persistence | Active-passive |
| Audit Logs | 1 hour | Continuous streaming | Geo-redundant storage |
| Custom User Attributes | 15 minutes | SQL geo-replication | Active-passive |

### Service Level Objectives (SLO)

- **Authentication Availability**: 99.9% (43.8 minutes downtime/month)
- **Token Issuance Success Rate**: 99.95%
- **MFA Challenge Success Rate**: 99.9%
- **API Response Time (p95)**: < 500ms during normal operations
- **API Response Time (p95) During Failover**: < 2000ms

---

## Failure Scenarios

### DR Test Scenarios Matrix

| Scenario ID | Scenario Name | Likelihood | Impact | RTO | Test Frequency |
|-------------|---------------|------------|--------|-----|----------------|
| DR-AUTH-01 | Azure AD B2C Regional Outage | Low | Critical | 1 hour | Quarterly |
| DR-AUTH-02 | Token Service Unavailability | Medium | Critical | 2 hours | Quarterly |
| DR-AUTH-03 | Key Vault Unavailability | Low | Critical | 1 hour | Quarterly |
| DR-AUTH-04 | Database Failover (User Records) | Medium | High | 2 hours | Quarterly |
| DR-AUTH-05 | Network Partitioning | Medium | High | 2 hours | Semi-Annually |
| DR-AUTH-06 | Redis Cache Failure (Sessions) | Medium | Medium | 1 hour | Semi-Annually |
| DR-AUTH-07 | Certificate Expiration/Rotation | Low | Critical | 30 minutes | Annually |
| DR-AUTH-08 | Complete Azure Region Failure | Very Low | Critical | 4 hours | Annually |

---

## Test Procedures

### DR-AUTH-01: Azure AD B2C Regional Outage

#### Scenario Description

Primary Azure AD B2C tenant becomes unavailable due to regional Azure outage affecting authentication services.

#### Prerequisites

- [ ] Azure AD B2C tenant deployed in multiple regions (Primary: East US, Secondary: West US)
- [ ] Traffic Manager or Azure Front Door configured for failover
- [ ] Custom policies replicated to secondary tenant
- [ ] DNS TTL set to 300 seconds (5 minutes)
- [ ] Monitoring alerts configured for B2C availability

#### Test Procedure

**Phase 1: Preparation (T-15 minutes)**

1. **Notify stakeholders** of planned DR test
   ```bash
   # Send notification via Teams/Slack
   curl -X POST https://hooks.slack.com/services/YOUR_WEBHOOK \
     -H 'Content-Type: application/json' \
     -d '{"text":"DR TEST STARTING: Azure AD B2C Regional Failover Test - Expected 15-30 min impact"}'
   ```

2. **Baseline metrics** - Capture current authentication metrics
   ```bash
   # Query Application Insights for baseline
   az monitor app-insights query \
     --app emr-appinsights \
     --analytics-query "requests
       | where timestamp > ago(15m)
       | where name contains 'auth'
       | summarize Count=count(), AvgDuration=avg(duration), P95=percentile(duration, 95) by name" \
     --output table
   ```

3. **Verify secondary tenant** is healthy
   ```bash
   # Test secondary B2C endpoint
   curl -I https://emrsecondary.b2clogin.com/emrsecondary.onmicrosoft.com/oauth2/v2.0/authorize
   # Expected: HTTP/2 200
   ```

**Phase 2: Simulate Failure (T+0 minutes)**

4. **Simulate regional outage** by blocking primary B2C endpoint
   ```bash
   # Method 1: Update Traffic Manager endpoint to disable primary
   az network traffic-manager endpoint update \
     --resource-group emr-prod-rg \
     --profile-name emr-auth-tm \
     --name primary-b2c-endpoint \
     --type azureEndpoints \
     --endpoint-status Disabled

   # Method 2: Update application configuration (for controlled test)
   # Temporarily update appsettings to point to invalid B2C tenant
   az webapp config appsettings set \
     --resource-group emr-prod-rg \
     --name emr-api-prod \
     --settings AzureAdB2C__Instance="https://invalid.b2clogin.com/"
   ```

5. **Verify failure detection** - Monitor alerts
   ```bash
   # Check Application Insights for failures
   az monitor app-insights query \
     --app emr-appinsights \
     --analytics-query "requests
       | where timestamp > ago(5m)
       | where name contains 'auth'
       | where success == false
       | summarize FailureCount=count() by resultCode" \
     --output table

   # Expected: Increase in 503/504 errors
   ```

**Phase 3: Execute Failover (T+5 minutes)**

6. **Initiate failover** to secondary B2C tenant
   ```bash
   # Update application configuration to use secondary tenant
   az webapp config appsettings set \
     --resource-group emr-prod-rg \
     --name emr-api-prod \
     --settings \
       AzureAdB2C__Instance="https://emrsecondary.b2clogin.com/" \
       AzureAdB2C__Domain="emrsecondary.onmicrosoft.com"

   # Restart application to apply changes
   az webapp restart --resource-group emr-prod-rg --name emr-api-prod
   ```

7. **Update web/mobile app configurations**
   ```bash
   # Update Web App configuration (Next.js)
   az webapp config appsettings set \
     --resource-group emr-prod-rg \
     --name emr-web-prod \
     --settings \
       NEXT_PUBLIC_B2C_AUTHORITY="https://emrsecondary.b2clogin.com/emrsecondary.onmicrosoft.com" \
       NEXT_PUBLIC_B2C_KNOWN_AUTHORITIES="emrsecondary.b2clogin.com"

   # Restart web app
   az webapp restart --resource-group emr-prod-rg --name emr-web-prod

   # Note: Mobile apps require app update for tenant change
   # Document this limitation in lessons learned
   ```

**Phase 4: Verification (T+15 minutes)**

8. **Verify authentication flow**
   ```bash
   # IMPORTANT: Azure AD B2C token endpoint requires policy name in path
   # Test token endpoint with client credentials (for service-to-service)
   curl -X POST "https://emrsecondary.b2clogin.com/emrsecondary.onmicrosoft.com/B2C_1_SignUpSignIn/oauth2/v2.0/token" \
     -H "Content-Type: application/x-www-form-urlencoded" \
     -d "grant_type=client_credentials&client_id=$CLIENT_ID&client_secret=$CLIENT_SECRET&scope=https://emrsecondary.onmicrosoft.com/api/.default"

   # Expected: 200 OK with access_token

   # Note: For user authentication testing, use authorization code flow instead
   # Password grant is NOT supported in Azure AD B2C
   ```

9. **Verify token validation**
   ```bash
   # Test API endpoint with newly issued token
   curl -X GET https://emr-api-prod.azurewebsites.net/api/auth/validate \
     -H "Authorization: Bearer $ACCESS_TOKEN"

   # Expected: 200 OK with user claims
   ```

10. **Monitor authentication metrics**
    ```bash
    az monitor app-insights query \
      --app emr-appinsights \
      --analytics-query "requests
        | where timestamp > ago(10m)
        | where name contains 'auth'
        | summarize SuccessRate=100.0*countif(success==true)/count(),
                    AvgDuration=avg(duration),
                    P95=percentile(duration, 95)
        | where timestamp > ago(5m)" \
      --output table

    # Expected: Success rate > 99%, P95 < 2000ms
    ```

**Phase 5: Rollback (T+30 minutes)**

11. **Restore primary tenant** configuration
    ```bash
    # Re-enable primary Traffic Manager endpoint
    az network traffic-manager endpoint update \
      --resource-group emr-prod-rg \
      --profile-name emr-auth-tm \
      --name primary-b2c-endpoint \
      --type azureEndpoints \
      --endpoint-status Enabled

    # Restore application settings
    az webapp config appsettings set \
      --resource-group emr-prod-rg \
      --name emr-api-prod \
      --settings \
        AzureAdB2C__Instance="https://emrprimary.b2clogin.com/" \
        AzureAdB2C__Domain="emrprimary.onmicrosoft.com"

    # Restart services
    az webapp restart --resource-group emr-prod-rg --name emr-api-prod
    az webapp restart --resource-group emr-prod-rg --name emr-web-prod
    ```

12. **Verify normal operations restored**
    ```bash
    # Wait 5 minutes for DNS propagation
    sleep 300

    # Verify primary endpoint
    curl -I https://emrprimary.b2clogin.com/emrprimary.onmicrosoft.com/oauth2/v2.0/authorize

    # Check metrics
    az monitor app-insights query \
      --app emr-appinsights \
      --analytics-query "requests
        | where timestamp > ago(5m)
        | where name contains 'auth'
        | summarize SuccessRate=100.0*countif(success==true)/count()" \
      --output table
    ```

#### Success Criteria

- [ ] Secondary B2C tenant accepts authentication requests within 5 minutes
- [ ] Token validation succeeds with secondary tenant tokens
- [ ] Web application authentication flow completes successfully
- [ ] API authorization checks pass with new tokens
- [ ] Total failover time < 60 minutes (RTO)
- [ ] No user data loss (RPO = 0)
- [ ] Authentication success rate > 99% after failover
- [ ] All existing sessions remain valid (token revalidation succeeds)

#### Expected Results

| Metric | Target | Actual | Status |
|--------|--------|--------|--------|
| Time to Detect Failure | < 3 minutes | ___ | ___ |
| Time to Initiate Failover | < 5 minutes | ___ | ___ |
| Time to Complete Failover | < 60 minutes | ___ | ___ |
| Data Loss (users) | 0 | ___ | ___ |
| Auth Success Rate Post-Failover | > 99% | ___ | ___ |
| P95 Response Time | < 2000ms | ___ | ___ |

---

### DR-AUTH-02: Token Service Unavailability

#### Scenario Description

Backend API token validation service becomes unavailable due to application error, deployment failure, or infrastructure issue.

#### Prerequisites

- [ ] Multi-instance API deployment (minimum 3 instances)
- [ ] Application Gateway with health probes configured
- [ ] Token validation caching enabled (5-minute TTL)
- [ ] Fallback authentication mechanism documented

#### Test Procedure

**Phase 1: Preparation (T-10 minutes)**

1. **Notify stakeholders** of planned DR test
   ```bash
   # Send notification
   curl -X POST https://hooks.slack.com/services/YOUR_WEBHOOK \
     -H 'Content-Type: application/json' \
     -d '{"text":"DR TEST STARTING: Token Service Unavailability - Expected 10-20 min impact"}'
   ```

2. **Baseline current token validation metrics**
   ```bash
   # Query token validation performance
   az monitor app-insights query \
     --app emr-appinsights \
     --analytics-query "requests
       | where timestamp > ago(15m)
       | where name == 'POST /api/auth/validate' or name == 'GET /api/auth/validate'
       | summarize Count=count(), SuccessRate=100.0*countif(success==true)/count(),
                   AvgDuration=avg(duration), P95=percentile(duration, 95)" \
     --output table
   ```

3. **Verify scaling settings**
   ```bash
   # Check autoscale configuration
   az monitor autoscale show \
     --resource-group emr-prod-rg \
     --name emr-api-autoscale \
     --output table
   ```

**Phase 2: Simulate Failure (T+0 minutes)**

4. **Simulate token service failure**
   ```bash
   # Method 1: Disable specific API instances (controlled test)
   az webapp stop \
     --resource-group emr-prod-rg \
     --name emr-api-prod \
     --slot staging

   # Method 2: Introduce configuration error to cause validation failures
   az webapp config appsettings set \
     --resource-group emr-prod-rg \
     --name emr-api-prod \
     --settings AzureAdB2C__SignUpSignInPolicyId="INVALID_POLICY"

   # Restart to apply bad configuration
   az webapp restart --resource-group emr-prod-rg --name emr-api-prod
   ```

5. **Monitor failure propagation**
   ```bash
   # Watch for 401/403 errors
   az monitor app-insights query \
     --app emr-appinsights \
     --analytics-query "requests
       | where timestamp > ago(5m)
       | where resultCode in ('401', '403', '500')
       | summarize Count=count() by resultCode, name
       | order by Count desc" \
     --output table
   ```

**Phase 3: Execute Recovery (T+5 minutes)**

6. **Trigger autoscaling** to add healthy instances
   ```bash
   # Manual scale-out if autoscale doesn't trigger
   az appservice plan update \
     --resource-group emr-prod-rg \
     --name emr-api-plan \
     --number-of-workers 5
   ```

7. **Deploy known-good configuration**
   ```bash
   # Rollback to previous slot (if using deployment slots)
   az webapp deployment slot swap \
     --resource-group emr-prod-rg \
     --name emr-api-prod \
     --slot staging \
     --target-slot production

   # OR restore correct configuration
   az webapp config appsettings set \
     --resource-group emr-prod-rg \
     --name emr-api-prod \
     --settings @appsettings.production.json

   # Restart services
   az webapp restart --resource-group emr-prod-rg --name emr-api-prod
   ```

**Phase 4: Verification (T+10 minutes)**

8. **Verify token validation restored**
   ```bash
   # Obtain fresh token
   TOKEN=$(curl -X POST https://emrprimary.b2clogin.com/emrprimary.onmicrosoft.com/oauth2/v2.0/token \
     -H "Content-Type: application/x-www-form-urlencoded" \
     -d "grant_type=password&username=$TEST_USER&password=$TEST_PASS&client_id=$CLIENT_ID&scope=openid" \
     -s | jq -r '.access_token')

   # Validate token via API
   curl -X GET https://emr-api-prod.azurewebsites.net/api/auth/validate \
     -H "Authorization: Bearer $TOKEN" \
     -w "\nHTTP Status: %{http_code}\n"

   # Expected: 200 OK with user claims
   ```

9. **Check cache effectiveness**
   ```bash
   # Query cache hit ratio
   az monitor app-insights query \
     --app emr-appinsights \
     --analytics-query "customMetrics
       | where timestamp > ago(10m)
       | where name == 'TokenValidationCacheHitRatio'
       | summarize AvgCacheHitRatio=avg(value)" \
     --output table

   # Expected: > 80% cache hit ratio
   ```

10. **Verify end-to-end authentication**
    ```bash
    # Test full authentication flow
    curl -X POST https://emr-api-prod.azurewebsites.net/api/patients \
      -H "Authorization: Bearer $TOKEN" \
      -H "Content-Type: application/json" \
      -d '{"action":"list"}' \
      -w "\nHTTP Status: %{http_code}\n"

    # Expected: 200 OK with patient data
    ```

**Phase 5: Post-Recovery Monitoring (T+20 minutes)**

11. **Monitor for degraded performance**
    ```bash
    # 15-minute rolling window
    az monitor app-insights query \
      --app emr-appinsights \
      --analytics-query "requests
        | where timestamp > ago(15m)
        | where name contains 'auth'
        | summarize SuccessRate=100.0*countif(success==true)/count(),
                    P50=percentile(duration, 50),
                    P95=percentile(duration, 95),
                    P99=percentile(duration, 99)
        | order by timestamp desc" \
      --output table
    ```

12. **Scale back to normal capacity** (if manually scaled)
    ```bash
    # Return to autoscale mode
    az appservice plan update \
      --resource-group emr-prod-rg \
      --name emr-api-plan \
      --number-of-workers 3
    ```

#### Success Criteria

- [ ] Token validation service recovers within 2 hours (RTO)
- [ ] Cached tokens continue to work during outage
- [ ] New token issuance resumes within 10 minutes
- [ ] Authentication success rate > 95% during recovery
- [ ] No user lockouts or session terminations
- [ ] Autoscaling triggers appropriately
- [ ] Health probes correctly identify unhealthy instances

#### Expected Results

| Metric | Target | Actual | Status |
|--------|--------|--------|--------|
| Time to Detect Failure | < 2 minutes | ___ | ___ |
| Time to Begin Recovery | < 5 minutes | ___ | ___ |
| Time to Full Recovery | < 120 minutes | ___ | ___ |
| Cache Hit Ratio During Outage | > 80% | ___ | ___ |
| Auth Success Rate During Recovery | > 95% | ___ | ___ |
| User Sessions Invalidated | 0 | ___ | ___ |

---

### DR-AUTH-03: Key Vault Unavailability

#### Scenario Description

Azure Key Vault becomes unavailable, preventing access to signing keys, certificates, and secrets required for token validation and encryption.

#### Prerequisites

- [ ] Key Vault deployed with geo-replication enabled
- [ ] Secrets cached in application memory (configurable TTL)
- [ ] Managed Identity configured for API access to Key Vault
- [ ] Secondary Key Vault in different region configured
- [ ] Key rotation procedures documented

#### Test Procedure

**Phase 1: Preparation (T-10 minutes)**

1. **Notify stakeholders**
   ```bash
   curl -X POST https://hooks.slack.com/services/YOUR_WEBHOOK \
     -H 'Content-Type: application/json' \
     -d '{"text":"DR TEST STARTING: Key Vault Unavailability - Expected 15-30 min impact"}'
   ```

2. **Verify secret caching is enabled**
   ```bash
   # Check application configuration
   az webapp config appsettings list \
     --resource-group emr-prod-rg \
     --name emr-api-prod \
     --query "[?name=='KeyVault__CacheDurationMinutes'].value" \
     --output table

   # Expected: 60 (minutes)
   ```

3. **Baseline Key Vault access metrics**
   ```bash
   # Query Key Vault diagnostic logs
   az monitor log-analytics query \
     --workspace emr-logs-workspace \
     --analytics-query "AzureDiagnostics
       | where ResourceProvider == 'MICROSOFT.KEYVAULT'
       | where TimeGenerated > ago(15m)
       | summarize Count=count() by OperationName, httpStatusCode_d
       | order by Count desc" \
     --output table
   ```

**Phase 2: Simulate Failure (T+0 minutes)**

4. **Block access to primary Key Vault**
   ```bash
   # Method 1: Update firewall rules to deny application access
   az keyvault network-rule remove \
     --resource-group emr-prod-rg \
     --name emr-keyvault-prod \
     --ip-address $(az webapp show --resource-group emr-prod-rg --name emr-api-prod --query outboundIpAddresses -o tsv | cut -d',' -f1)

   # Method 2: Disable Key Vault temporarily (more disruptive)
   # Note: This is commented out as it affects all services
   # az keyvault update --name emr-keyvault-prod --enabled-for-deployment false --enabled-for-disk-encryption false
   ```

5. **Monitor for failures**
   ```bash
   # Watch application logs for Key Vault errors
   az webapp log tail \
     --resource-group emr-prod-rg \
     --name emr-api-prod \
     --filter "KeyVault|Secret|Certificate" \
     &

   # Monitor Key Vault access denials
   az monitor log-analytics query \
     --workspace emr-logs-workspace \
     --analytics-query "AzureDiagnostics
       | where ResourceProvider == 'MICROSOFT.KEYVAULT'
       | where TimeGenerated > ago(5m)
       | where httpStatusCode_d >= 400
       | summarize Count=count() by OperationName, httpStatusCode_d" \
     --output table
   ```

**Phase 3: Verify Cache Effectiveness (T+5 minutes)**

6. **Verify cached secrets continue to work**
   ```bash
   # Test authentication with cached signing keys
   TOKEN=$(curl -X POST https://emrprimary.b2clogin.com/emrprimary.onmicrosoft.com/oauth2/v2.0/token \
     -H "Content-Type: application/x-www-form-urlencoded" \
     -d "grant_type=client_credentials&client_id=$CLIENT_ID&client_secret=$CLIENT_SECRET&scope=https://emrprimary.onmicrosoft.com/.default" \
     -s | jq -r '.access_token')

   # Validate token (should work with cached keys)
   curl -X GET https://emr-api-prod.azurewebsites.net/api/auth/validate \
     -H "Authorization: Bearer $TOKEN" \
     -w "\nHTTP Status: %{http_code}\n"

   # Expected: 200 OK (using cached signing keys)
   ```

7. **Monitor cache expiration timeline**
   ```bash
   # Check when secrets were last refreshed
   az monitor app-insights query \
     --app emr-appinsights \
     --analytics-query "customEvents
       | where timestamp > ago(2h)
       | where name == 'SecretRefreshed'
       | summarize LastRefresh=max(timestamp) by SecretName
       | extend MinutesAgo=datetime_diff('minute', now(), LastRefresh)" \
     --output table
   ```

**Phase 4: Execute Failover (T+15 minutes)**

8. **Switch to secondary Key Vault**
   ```bash
   # Update application configuration to use secondary Key Vault
   az webapp config appsettings set \
     --resource-group emr-prod-rg \
     --name emr-api-prod \
     --settings KeyVault__VaultUri="https://emr-keyvault-secondary.vault.azure.net/"

   # Grant application access to secondary vault
   APP_IDENTITY=$(az webapp identity show --resource-group emr-prod-rg --name emr-api-prod --query principalId -o tsv)

   az keyvault set-policy \
     --name emr-keyvault-secondary \
     --object-id $APP_IDENTITY \
     --secret-permissions get list \
     --certificate-permissions get list \
     --key-permissions get list decrypt verify

   # Restart application to apply changes
   az webapp restart --resource-group emr-prod-rg --name emr-api-prod
   ```

9. **Verify secret retrieval from secondary vault**
   ```bash
   # Wait for application startup (30 seconds)
   sleep 30

   # Check application logs for successful secret retrieval
   az webapp log tail \
     --resource-group emr-prod-rg \
     --name emr-api-prod \
     --filter "Secret retrieved from KeyVault" \
     | head -n 20
   ```

**Phase 5: Verification (T+20 minutes)**

10. **Verify authentication flows**
    ```bash
    # Test full authentication flow
    curl -X POST https://emr-api-prod.azurewebsites.net/api/auth/login \
      -H "Content-Type: application/json" \
      -d '{"username":"test.user@emr.com","password":"Test123!"}' \
      -w "\nHTTP Status: %{http_code}\n"

    # Expected: 200 OK with access_token
    ```

11. **Verify certificate-based operations**
    ```bash
    # Test API mutual TLS (if configured)
    curl -X GET https://emr-api-prod.azurewebsites.net/api/health/detailed \
      --cert client-cert.pem \
      --key client-key.pem \
      -w "\nHTTP Status: %{http_code}\n"
    ```

**Phase 6: Rollback (T+30 minutes)**

12. **Restore primary Key Vault access**
    ```bash
    # Restore firewall rule
    az keyvault network-rule add \
      --resource-group emr-prod-rg \
      --name emr-keyvault-prod \
      --ip-address $(az webapp show --resource-group emr-prod-rg --name emr-api-prod --query outboundIpAddresses -o tsv | cut -d',' -f1)

    # Restore application configuration
    az webapp config appsettings set \
      --resource-group emr-prod-rg \
      --name emr-api-prod \
      --settings KeyVault__VaultUri="https://emr-keyvault-prod.vault.azure.net/"

    # Restart application
    az webapp restart --resource-group emr-prod-rg --name emr-api-prod
    ```

13. **Verify normal operations**
    ```bash
    # Wait for startup
    sleep 30

    # Verify primary Key Vault access
    az monitor log-analytics query \
      --workspace emr-logs-workspace \
      --analytics-query "AzureDiagnostics
        | where ResourceProvider == 'MICROSOFT.KEYVAULT'
        | where ResourceId contains 'emr-keyvault-prod'
        | where TimeGenerated > ago(5m)
        | where httpStatusCode_d == 200
        | summarize Count=count() by OperationName" \
      --output table
    ```

#### Success Criteria

- [ ] Cached secrets continue to work during Key Vault outage
- [ ] Secret cache TTL is at least 60 minutes
- [ ] Failover to secondary Key Vault completes within 1 hour
- [ ] No authentication failures occur during cache validity period
- [ ] All secrets successfully retrieved from secondary vault
- [ ] Certificate-based operations continue functioning
- [ ] Zero user-facing authentication errors

#### Expected Results

| Metric | Target | Actual | Status |
|--------|--------|--------|--------|
| Time Before Cache Expiration | > 60 minutes | ___ | ___ |
| Auth Success Rate (Cached Period) | 100% | ___ | ___ |
| Time to Failover to Secondary Vault | < 60 minutes | ___ | ___ |
| Secret Retrieval Success Rate | 100% | ___ | ___ |
| User-Facing Auth Errors | 0 | ___ | ___ |

---

### DR-AUTH-04: Database Failover (User Records)

#### Scenario Description

SQL Database containing user roles, permissions, and custom attributes becomes unavailable, requiring failover to geo-replicated secondary database.

#### Prerequisites

- [ ] SQL Database configured with active geo-replication
- [ ] Secondary database in different Azure region (paired region)
- [ ] Automatic failover group configured
- [ ] Connection string failover logic implemented in application
- [ ] Read-only routing enabled for reporting queries

#### Test Procedure

**Phase 1: Preparation (T-10 minutes)**

1. **Notify stakeholders**
   ```bash
   curl -X POST https://hooks.slack.com/services/YOUR_WEBHOOK \
     -H 'Content-Type: application/json' \
     -d '{"text":"DR TEST STARTING: SQL Database Failover - Expected 10-15 min impact"}'
   ```

2. **Baseline database metrics**
   ```bash
   # Check current primary/secondary status
   az sql failover-group show \
     --resource-group emr-prod-rg \
     --server emr-sql-primary \
     --name emr-sql-fog \
     --query "{Primary:replicationRole,State:replicationState}" \
     --output table

   # Query database performance metrics
   az monitor metrics list \
     --resource $(az sql db show --resource-group emr-prod-rg --server emr-sql-primary --name emr-users-db --query id -o tsv) \
     --metric "cpu_percent" "dtu_consumption_percent" "connection_successful" \
     --output table
   ```

3. **Verify replication lag**
   ```bash
   # Check geo-replication status
   az sql db replica list \
     --resource-group emr-prod-rg \
     --server emr-sql-primary \
     --name emr-users-db \
     --output table

   # Query replication lag (should be < 5 seconds)
   ```

4. **Capture current data state**
   ```bash
   # Count records in key tables (for verification post-failover)
   sqlcmd -S emr-sql-primary.database.windows.net -d emr-users-db -U $ADMIN_USER -P $ADMIN_PASS -Q "
     SELECT 'Users' as TableName, COUNT(*) as RecordCount FROM dbo.Users
     UNION ALL
     SELECT 'UserRoleAssignments', COUNT(*) FROM dbo.UserRoleAssignments
     UNION ALL
     SELECT 'RolePermissions', COUNT(*) FROM dbo.RolePermissions
     UNION ALL
     SELECT 'ResourceAuthorizations', COUNT(*) FROM dbo.ResourceAuthorizations
   "
   ```

**Phase 2: Simulate Failure (T+0 minutes)**

5. **Initiate forced failover**
   ```bash
   # Trigger failover group failover (with data loss potential - for DR testing only)
   az sql failover-group set-primary \
     --resource-group emr-prod-rg \
     --server emr-sql-secondary \
     --name emr-sql-fog \
     --failover-policy Automatic \
     --grace-period 1

   # Note: This will make the secondary the new primary
   ```

6. **Monitor failover progress**
   ```bash
   # Watch failover status
   watch -n 5 'az sql failover-group show \
     --resource-group emr-prod-rg \
     --server emr-sql-secondary \
     --name emr-sql-fog \
     --query "{Primary:replicationRole,State:replicationState}" \
     --output table'

   # Expected: Transition from "Secondary" to "Primary"
   ```

**Phase 3: Verify Application Failover (T+5 minutes)**

7. **Verify connection string failover**
   ```bash
   # Application should automatically connect to new primary via failover group endpoint
   # Check application logs for database connection changes
   az webapp log tail \
     --resource-group emr-prod-rg \
     --name emr-api-prod \
     --filter "Database|Connection|SQL" \
     | head -n 30
   ```

8. **Test database connectivity**
   ```bash
   # Verify application can query new primary
   curl -X GET https://emr-api-prod.azurewebsites.net/api/roles \
     -H "Authorization: Bearer $ADMIN_TOKEN" \
     -w "\nHTTP Status: %{http_code}\n"

   # Expected: 200 OK with role data
   ```

9. **Verify data integrity**
   ```bash
   # Query record counts on new primary
   sqlcmd -S emr-sql-secondary.database.windows.net -d emr-users-db -U $ADMIN_USER -P $ADMIN_PASS -Q "
     SELECT 'Users' as TableName, COUNT(*) as RecordCount FROM dbo.Users
     UNION ALL
     SELECT 'UserRoleAssignments', COUNT(*) FROM dbo.UserRoleAssignments
     UNION ALL
     SELECT 'RolePermissions', COUNT(*) FROM dbo.RolePermissions
     UNION ALL
     SELECT 'ResourceAuthorizations', COUNT(*) FROM dbo.ResourceAuthorizations
   "

   # Compare with baseline counts from Phase 1
   ```

**Phase 4: Functional Verification (T+10 minutes)**

10. **Test user role queries**
    ```bash
    # Test role assignment retrieval
    curl -X GET https://emr-api-prod.azurewebsites.net/api/users/12345/roles \
      -H "Authorization: Bearer $ADMIN_TOKEN" \
      -w "\nHTTP Status: %{http_code}\n"
    ```

11. **Test permission checks**
    ```bash
    # Test permission validation
    curl -X POST https://emr-api-prod.azurewebsites.net/api/auth/check-permission \
      -H "Authorization: Bearer $USER_TOKEN" \
      -H "Content-Type: application/json" \
      -d '{"permission":"PatientsView","resourceId":"patient-123"}' \
      -w "\nHTTP Status: %{http_code}\n"
    ```

12. **Test write operations**
    ```bash
    # Test role assignment (write operation)
    curl -X POST https://emr-api-prod.azurewebsites.net/api/users/12345/roles \
      -H "Authorization: Bearer $ADMIN_TOKEN" \
      -H "Content-Type: application/json" \
      -d '{"roleId":3,"effectiveFrom":"2025-12-28","effectiveTo":"2026-12-28"}' \
      -w "\nHTTP Status: %{http_code}\n"

    # Expected: 201 Created
    ```

**Phase 5: Performance Validation (T+15 minutes)**

13. **Monitor query performance**
    ```bash
    # Check for degraded performance on new primary
    az monitor metrics list \
      --resource $(az sql db show --resource-group emr-prod-rg --server emr-sql-secondary --name emr-users-db --query id -o tsv) \
      --metric "cpu_percent" "dtu_consumption_percent" "avg_cpu_percent" \
      --start-time $(date -u -d '10 minutes ago' '+%Y-%m-%dT%H:%M:%SZ') \
      --end-time $(date -u '+%Y-%m-%dT%H:%M:%SZ') \
      --output table
    ```

14. **Query slow queries**
    ```bash
    # Check for query performance issues
    sqlcmd -S emr-sql-secondary.database.windows.net -d emr-users-db -U $ADMIN_USER -P $ADMIN_PASS -Q "
      SELECT TOP 10
        query_stats.query_hash,
        SUM(query_stats.total_worker_time) / SUM(query_stats.execution_count) AS avg_cpu_time,
        MIN(query_stats.statement_text) AS sample_query
      FROM sys.dm_exec_query_stats AS query_stats
      CROSS APPLY sys.dm_exec_sql_text(query_stats.sql_handle) AS sql_text
      WHERE query_stats.creation_time > DATEADD(MINUTE, -15, GETUTCDATE())
      GROUP BY query_stats.query_hash
      ORDER BY avg_cpu_time DESC
    "
    ```

**Phase 6: Failback (T+30 minutes)**

15. **Plan failback window** (optional - typically left in new state)
    ```bash
    # Optional: Failback to original primary
    # Note: In production, typically remain on new primary unless issues detected

    # If failback required:
    az sql failover-group set-primary \
      --resource-group emr-prod-rg \
      --server emr-sql-primary \
      --name emr-sql-fog
    ```

16. **Document final state**
    ```bash
    # Capture final configuration
    az sql failover-group show \
      --resource-group emr-prod-rg \
      --server emr-sql-secondary \
      --name emr-sql-fog \
      --output json > dr-test-db-failover-final-state.json
    ```

#### Success Criteria

- [ ] Database failover completes within 2 hours (RTO)
- [ ] Data loss < 1 hour of transactions (RPO)
- [ ] Application automatically connects to new primary
- [ ] All user role and permission queries succeed
- [ ] Write operations (role assignments) succeed
- [ ] No connection pool errors or timeouts
- [ ] Query performance within 20% of baseline

#### Expected Results

| Metric | Target | Actual | Status |
|--------|--------|--------|--------|
| Failover Initiation Time | < 2 minutes | ___ | ___ |
| Failover Completion Time | < 10 minutes | ___ | ___ |
| Data Loss (transactions) | < 60 minutes | ___ | ___ |
| Record Count Match | 100% | ___ | ___ |
| Application Auto-Reconnect | Success | ___ | ___ |
| Query Success Rate | > 99% | ___ | ___ |
| Write Operation Success | 100% | ___ | ___ |

---

### DR-AUTH-05: Network Partitioning

#### Scenario Description

Network connectivity issues cause partitioning between application components (Web/Mobile apps, API, Azure AD B2C, Database).

#### Prerequisites

- [ ] Azure Traffic Manager or Front Door configured
- [ ] Multi-region deployment for API and database
- [ ] Network Security Groups (NSGs) documented
- [ ] Circuit breaker pattern implemented in application
- [ ] Health endpoints configured on all services

#### Test Procedure

**Phase 1: Preparation (T-10 minutes)**

1. **Notify stakeholders**
   ```bash
   curl -X POST https://hooks.slack.com/services/YOUR_WEBHOOK \
     -H 'Content-Type: application/json' \
     -d '{"text":"DR TEST STARTING: Network Partitioning Simulation - Expected 15-20 min impact"}'
   ```

2. **Baseline network connectivity**
   ```bash
   # Test connectivity from API to dependencies
   az webapp log tail --resource-group emr-prod-rg --name emr-api-prod &

   # Check current network rules
   az network nsg rule list \
     --resource-group emr-prod-rg \
     --nsg-name emr-api-nsg \
     --output table
   ```

3. **Document current routing**
   ```bash
   # Check Traffic Manager configuration
   az network traffic-manager profile show \
     --resource-group emr-prod-rg \
     --name emr-auth-tm \
     --query "{Status:profileStatus,RoutingMethod:trafficRoutingMethod}" \
     --output table
   ```

**Phase 2: Simulate Network Partition (T+0 minutes)**

4. **Scenario A: Block API access to Azure AD B2C**
   ```bash
   # Add NSG rule to block outbound traffic to Azure AD B2C
   az network nsg rule create \
     --resource-group emr-prod-rg \
     --nsg-name emr-api-nsg \
     --name BlockB2CTraffic \
     --priority 100 \
     --direction Outbound \
     --access Deny \
     --protocol Tcp \
     --destination-address-prefixes AzureActiveDirectory \
     --destination-port-ranges 443 \
     --description "DR Test: Block Azure AD B2C access"
   ```

5. **Scenario B: Block API access to SQL Database**
   ```bash
   # Block database connectivity
   az network nsg rule create \
     --resource-group emr-prod-rg \
     --nsg-name emr-api-nsg \
     --name BlockSQLTraffic \
     --priority 101 \
     --direction Outbound \
     --access Deny \
     --protocol Tcp \
     --destination-address-prefixes Sql \
     --destination-port-ranges 1433 \
     --description "DR Test: Block SQL Database access"
   ```

6. **Monitor impact**
   ```bash
   # Watch for connection failures
   az webapp log tail \
     --resource-group emr-prod-rg \
     --name emr-api-prod \
     --filter "Connection|Timeout|Network" \
     | head -n 50
   ```

**Phase 3: Verify Circuit Breaker Behavior (T+5 minutes)**

7. **Verify circuit breaker activates**
   ```bash
   # Check circuit breaker state via custom metrics
   az monitor app-insights query \
     --app emr-appinsights \
     --analytics-query "customMetrics
       | where timestamp > ago(10m)
       | where name == 'CircuitBreakerState'
       | summarize arg_max(timestamp, *) by DependencyName
       | project DependencyName, State=value, timestamp" \
     --output table

   # Expected: CircuitBreaker state = "Open" for affected dependencies
   ```

8. **Verify graceful degradation**
   ```bash
   # Test authentication with fallback mechanisms
   curl -X GET https://emr-api-prod.azurewebsites.net/api/health \
     -w "\nHTTP Status: %{http_code}\n"

   # Expected: 503 Service Unavailable with degraded mode indicator
   ```

**Phase 4: Execute Recovery (T+10 minutes)**

9. **Remove network restrictions**
   ```bash
   # Remove blocking NSG rules
   az network nsg rule delete \
     --resource-group emr-prod-rg \
     --nsg-name emr-api-nsg \
     --name BlockB2CTraffic

   az network nsg rule delete \
     --resource-group emr-prod-rg \
     --nsg-name emr-api-nsg \
     --name BlockSQLTraffic
   ```

10. **Verify circuit breaker recovery**
    ```bash
    # Wait for circuit breaker to close (typically 30-60 seconds)
    sleep 60

    # Check circuit breaker state
    az monitor app-insights query \
      --app emr-appinsights \
      --analytics-query "customMetrics
        | where timestamp > ago(5m)
        | where name == 'CircuitBreakerState'
        | summarize arg_max(timestamp, *) by DependencyName
        | project DependencyName, State=value, timestamp" \
      --output table

    # Expected: State = "Closed" (healthy)
    ```

**Phase 5: Verification (T+15 minutes)**

11. **Verify full connectivity restored**
    ```bash
    # Test authentication flow
    curl -X POST https://emr-api-prod.azurewebsites.net/api/auth/login \
      -H "Content-Type: application/json" \
      -d '{"username":"test.user@emr.com","password":"Test123!"}' \
      -w "\nHTTP Status: %{http_code}\n"

    # Test database operations
    curl -X GET https://emr-api-prod.azurewebsites.net/api/roles \
      -H "Authorization: Bearer $TOKEN" \
      -w "\nHTTP Status: %{http_code}\n"
    ```

12. **Monitor recovery metrics**
    ```bash
    # Check dependency success rates
    az monitor app-insights query \
      --app emr-appinsights \
      --analytics-query "dependencies
        | where timestamp > ago(10m)
        | summarize SuccessRate=100.0*countif(success==true)/count(),
                    AvgDuration=avg(duration)
          by name
        | order by SuccessRate asc" \
      --output table
    ```

#### Success Criteria

- [ ] Circuit breaker activates within 30 seconds of network partition
- [ ] Application enters graceful degradation mode
- [ ] Health endpoints return appropriate status codes (503)
- [ ] No application crashes or unhandled exceptions
- [ ] Circuit breaker closes automatically after connectivity restored
- [ ] Full service recovery within 2 hours (RTO)
- [ ] Detailed error messages provided to clients

#### Expected Results

| Metric | Target | Actual | Status |
|--------|--------|--------|--------|
| Time to Circuit Breaker Open | < 30 seconds | ___ | ___ |
| Application Crash Events | 0 | ___ | ___ |
| Time to Detect Network Recovery | < 60 seconds | ___ | ___ |
| Time to Circuit Breaker Close | < 120 seconds | ___ | ___ |
| Time to Full Service Recovery | < 120 minutes | ___ | ___ |
| Error Messages Clarity | Clear & Actionable | ___ | ___ |

---

## Rollback Procedures

### General Rollback Principles

1. **Always have a rollback plan** before executing DR tests
2. **Document the current state** before making changes
3. **Use feature flags** to quickly disable problematic features
4. **Maintain rollback scripts** for all common scenarios
5. **Test rollback procedures** as part of DR testing

### Emergency Rollback Checklist

```bash
# 1. Stop all in-progress deployments
az webapp deployment list-publishing-credentials --resource-group emr-prod-rg --name emr-api-prod

# 2. Restore previous application version
az webapp deployment slot swap \
  --resource-group emr-prod-rg \
  --name emr-api-prod \
  --slot staging \
  --target-slot production

# 3. Restore configuration settings
az webapp config appsettings set \
  --resource-group emr-prod-rg \
  --name emr-api-prod \
  --settings @backup-appsettings-$(date +%Y%m%d).json

# 4. Restart all services
az webapp restart --resource-group emr-prod-rg --name emr-api-prod
az webapp restart --resource-group emr-prod-rg --name emr-web-prod

# 5. Verify rollback success
curl -X GET https://emr-api-prod.azurewebsites.net/api/health
```

### Rollback Scenarios

#### Rollback from B2C Tenant Failover

```bash
# Restore primary B2C tenant configuration
az webapp config appsettings set \
  --resource-group emr-prod-rg \
  --name emr-api-prod \
  --settings \
    AzureAdB2C__Instance="https://emrprimary.b2clogin.com/" \
    AzureAdB2C__Domain="emrprimary.onmicrosoft.com" \
    AzureAdB2C__TenantId="$PRIMARY_TENANT_ID"

# Update web application
az webapp config appsettings set \
  --resource-group emr-prod-rg \
  --name emr-web-prod \
  --settings \
    NEXT_PUBLIC_B2C_AUTHORITY="https://emrprimary.b2clogin.com/emrprimary.onmicrosoft.com"

# Restart services
az webapp restart --resource-group emr-prod-rg --name emr-api-prod
az webapp restart --resource-group emr-prod-rg --name emr-web-prod

# Verify
sleep 30
curl -X GET https://emr-api-prod.azurewebsites.net/api/auth/validate \
  -H "Authorization: Bearer $TOKEN"
```

#### Rollback from Key Vault Failover

```bash
# Restore primary Key Vault configuration
az webapp config appsettings set \
  --resource-group emr-prod-rg \
  --name emr-api-prod \
  --settings KeyVault__VaultUri="https://emr-keyvault-prod.vault.azure.net/"

# Ensure firewall access is restored
az keyvault network-rule add \
  --resource-group emr-prod-rg \
  --name emr-keyvault-prod \
  --ip-address $(az webapp show --resource-group emr-prod-rg --name emr-api-prod --query outboundIpAddresses -o tsv | cut -d',' -f1)

# Restart and verify
az webapp restart --resource-group emr-prod-rg --name emr-api-prod
sleep 30

# Test secret retrieval
curl -X GET https://emr-api-prod.azurewebsites.net/api/health/detailed \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

#### Rollback from Database Failover

```bash
# Initiate failback to original primary
az sql failover-group set-primary \
  --resource-group emr-prod-rg \
  --server emr-sql-primary \
  --name emr-sql-fog

# Wait for failover completion
sleep 60

# Verify application connectivity
curl -X GET https://emr-api-prod.azurewebsites.net/api/roles \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Verify data integrity
sqlcmd -S emr-sql-primary.database.windows.net -d emr-users-db -U $ADMIN_USER -P $ADMIN_PASS -Q "
  SELECT 'Users' as TableName, COUNT(*) as RecordCount FROM dbo.Users
  UNION ALL
  SELECT 'UserRoleAssignments', COUNT(*) FROM dbo.UserRoleAssignments
"
```

#### Rollback from Network Changes

```bash
# Remove all test NSG rules
az network nsg rule delete \
  --resource-group emr-prod-rg \
  --nsg-name emr-api-nsg \
  --name BlockB2CTraffic

az network nsg rule delete \
  --resource-group emr-prod-rg \
  --nsg-name emr-api-nsg \
  --name BlockSQLTraffic

# Verify connectivity
curl -X GET https://emr-api-prod.azurewebsites.net/api/health
```

### Rollback Verification Checklist

After any rollback, verify:

- [ ] Authentication flow completes successfully
- [ ] Token validation succeeds
- [ ] User role queries return expected results
- [ ] Permission checks function correctly
- [ ] Application logs show no errors
- [ ] Monitoring dashboards show green status
- [ ] No increase in error rates
- [ ] Response times within normal ranges

---

## Monitoring & Alerts

### Critical Monitoring Metrics

#### Authentication Service Availability

```kusto
// Application Insights Query
requests
| where timestamp > ago(5m)
| where name contains "auth" or url contains "login" or url contains "token"
| summarize
    TotalRequests = count(),
    FailedRequests = countif(success == false),
    AvailabilityPercent = 100.0 * countif(success == true) / count(),
    P50 = percentile(duration, 50),
    P95 = percentile(duration, 95),
    P99 = percentile(duration, 99)
| extend Status = iff(AvailabilityPercent >= 99.9, "Healthy", iff(AvailabilityPercent >= 99.0, "Degraded", "Critical"))
```

**Alert Thresholds:**
- Critical: Availability < 99.0%
- Warning: Availability < 99.9%
- P95 Response Time > 2000ms

#### Azure AD B2C Health

```kusto
// Monitor B2C authentication success rate
dependencies
| where timestamp > ago(5m)
| where type == "HTTP"
| where target contains "b2clogin.com"
| summarize
    TotalCalls = count(),
    FailedCalls = countif(success == false),
    SuccessRate = 100.0 * countif(success == true) / count(),
    AvgDuration = avg(duration)
| extend Status = iff(SuccessRate >= 99.5, "Healthy", iff(SuccessRate >= 95.0, "Degraded", "Critical"))
```

**Alert Thresholds:**
- Critical: Success Rate < 95%
- Warning: Success Rate < 99.5%
- Average Duration > 1000ms

#### Token Validation Performance

```kusto
// Monitor token validation latency and errors
customMetrics
| where timestamp > ago(5m)
| where name == "TokenValidationDuration"
| summarize
    Count = count(),
    AvgDuration = avg(value),
    P95 = percentile(value, 95),
    P99 = percentile(value, 99)
| extend Status = iff(P95 <= 100, "Healthy", iff(P95 <= 500, "Degraded", "Critical"))
```

**Alert Thresholds:**
- Critical: P95 > 500ms
- Warning: P95 > 100ms

#### Database Connection Health

```kusto
// Monitor database connectivity
dependencies
| where timestamp > ago(5m)
| where type == "SQL"
| where target contains "database.windows.net"
| summarize
    TotalQueries = count(),
    FailedQueries = countif(success == false),
    SuccessRate = 100.0 * countif(success == true) / count(),
    AvgDuration = avg(duration),
    P95 = percentile(duration, 95)
| extend Status = iff(SuccessRate >= 99.9 and P95 <= 100, "Healthy",
                      iff(SuccessRate >= 99.0 and P95 <= 500, "Degraded", "Critical"))
```

**Alert Thresholds:**
- Critical: Success Rate < 99% OR P95 > 500ms
- Warning: Success Rate < 99.9% OR P95 > 100ms

#### Key Vault Access Monitoring

```kusto
// Azure Monitor Logs Query for Key Vault
AzureDiagnostics
| where ResourceProvider == "MICROSOFT.KEYVAULT"
| where TimeGenerated > ago(5m)
| summarize
    TotalOperations = count(),
    FailedOperations = countif(httpStatusCode_d >= 400),
    SuccessRate = 100.0 * countif(httpStatusCode_d < 400) / count()
    by OperationName
| extend Status = iff(SuccessRate >= 99.9, "Healthy", iff(SuccessRate >= 95.0, "Degraded", "Critical"))
```

**Alert Thresholds:**
- Critical: Success Rate < 95% OR > 10 failures in 5 minutes
- Warning: Success Rate < 99.9%

### Alert Configuration

#### Azure Monitor Alert Rules

```bash
# Create authentication availability alert
az monitor metrics alert create \
  --name "Auth-Service-Availability-Critical" \
  --resource-group emr-prod-rg \
  --scopes $(az webapp show --resource-group emr-prod-rg --name emr-api-prod --query id -o tsv) \
  --condition "avg availability < 99" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --severity 0 \
  --description "Authentication service availability below 99%" \
  --action emr-critical-action-group

# Create token validation latency alert
az monitor metrics alert create \
  --name "Token-Validation-Latency-Warning" \
  --resource-group emr-prod-rg \
  --scopes $(az monitor app-insights component show --app emr-appinsights --resource-group emr-prod-rg --query id -o tsv) \
  --condition "avg customMetrics/TokenValidationDuration > 500" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --severity 2 \
  --description "Token validation P95 latency exceeds 500ms" \
  --action emr-warning-action-group

# Create B2C dependency alert
az monitor metrics alert create \
  --name "B2C-Dependency-Failure-Critical" \
  --resource-group emr-prod-rg \
  --scopes $(az monitor app-insights component show --app emr-appinsights --resource-group emr-prod-rg --query id -o tsv) \
  --condition "avg dependencies/failed > 5" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --severity 0 \
  --description "Azure AD B2C dependency failures detected" \
  --action emr-critical-action-group
```

### DR Test Monitoring Dashboard

Create a dedicated Azure Dashboard for DR testing:

```bash
# Create dashboard via Azure Portal or ARM template
# Include these tiles:

# 1. Authentication Success Rate (5-minute rolling window)
# 2. Token Validation Latency (P50, P95, P99)
# 3. Azure AD B2C Dependency Health
# 4. SQL Database Connection Success Rate
# 5. Key Vault Access Success Rate
# 6. Circuit Breaker State
# 7. Active User Sessions
# 8. Error Rate by Component
# 9. Geographic Distribution of Requests
# 10. Failover Status (custom metric)
```

### Real-time Monitoring During DR Tests

```bash
# Monitor authentication metrics in real-time
az monitor app-insights query \
  --app emr-appinsights \
  --analytics-query "
    requests
    | where timestamp > ago(1m)
    | where name contains 'auth'
    | summarize
        Requests = count(),
        Success = countif(success==true),
        Failed = countif(success==false),
        SuccessRate = 100.0*countif(success==true)/count(),
        AvgDuration = avg(duration)
    | extend Timestamp = now()
  " \
  --output table \
  --query "tables[0].rows" \
  --output tsv | while read line; do
    echo "$(date '+%Y-%m-%d %H:%M:%S') | $line"
    sleep 5
  done
```

### Log Aggregation Queries

#### Authentication Errors by Type

```kusto
exceptions
| where timestamp > ago(30m)
| where outerMessage contains "auth" or outerMessage contains "token" or outerMessage contains "B2C"
| summarize Count = count() by problemId, outerMessage
| order by Count desc
| take 20
```

#### User Impact Analysis

```kusto
// Count unique users affected during DR test
requests
| where timestamp between (datetime(2025-12-28 10:00:00) .. datetime(2025-12-28 11:00:00))
| where success == false
| where name contains "auth"
| extend UserId = tostring(customDimensions.UserId)
| where isnotempty(UserId)
| summarize AffectedUsers = dcount(UserId), TotalFailures = count()
```

---

## Test Schedule

### Quarterly DR Testing Schedule (2025)

| Test Date | Scenarios | Duration | Responsible Team | Status |
|-----------|-----------|----------|------------------|--------|
| 2025-01-15 | DR-AUTH-01, DR-AUTH-02 | 2 hours | Platform Engineering | Planned |
| 2025-04-15 | DR-AUTH-03, DR-AUTH-04 | 2 hours | Platform Engineering | Planned |
| 2025-07-15 | DR-AUTH-05, DR-AUTH-06 | 2 hours | Platform Engineering | Planned |
| 2025-10-15 | DR-AUTH-01, DR-AUTH-07 | 2 hours | Platform Engineering | Planned |

### Semi-Annual Tests

| Test Date | Scenarios | Duration | Responsible Team | Status |
|-----------|-----------|----------|------------------|--------|
| 2025-02-15 | DR-AUTH-05, DR-AUTH-06 | 1.5 hours | Platform Engineering | Planned |
| 2025-08-15 | DR-AUTH-05, DR-AUTH-06 | 1.5 hours | Platform Engineering | Planned |

### Annual Comprehensive Test

| Test Date | Scenarios | Duration | Responsible Team | Status |
|-----------|-----------|----------|------------------|--------|
| 2025-06-15 | DR-AUTH-08 (Full Region Failover) | 4 hours | Platform Engineering + Security | Planned |

### Test Window Guidelines

- **Preferred Test Window**: Tuesday-Thursday, 10:00 AM - 2:00 PM EST
- **Blackout Periods**:
  - First week of month (billing cycles)
  - Major holidays
  - Planned system maintenance windows
  - Clinical trial enrollment periods
- **Notification Lead Time**: 5 business days minimum
- **Stakeholder Approval Required**: CTO, CISO, VP Engineering

### Pre-Test Preparation Checklist

**1 Week Before Test:**
- [ ] Send calendar invites to all stakeholders
- [ ] Review and update DR procedures
- [ ] Verify all monitoring dashboards are functional
- [ ] Confirm secondary/backup systems are healthy
- [ ] Schedule post-test review meeting

**1 Day Before Test:**
- [ ] Send reminder notification to stakeholders
- [ ] Verify on-call rotation is staffed
- [ ] Backup current configurations
- [ ] Prepare rollback scripts
- [ ] Test communication channels (Slack, Teams, PagerDuty)

**1 Hour Before Test:**
- [ ] Final stakeholder notification
- [ ] Enable detailed logging
- [ ] Open monitoring dashboards
- [ ] Join war room / bridge line
- [ ] Verify rollback procedures are accessible

### Post-Test Activities

**Immediately After Test:**
- [ ] Verify all systems restored to normal
- [ ] Send test completion notification
- [ ] Capture final metrics and screenshots
- [ ] Document any deviations from expected results

**Within 24 Hours:**
- [ ] Complete DR Test Report (template below)
- [ ] Log all issues in tracking system
- [ ] Update runbooks based on lessons learned
- [ ] Share preliminary findings with stakeholders

**Within 1 Week:**
- [ ] Conduct post-test review meeting
- [ ] Update DR procedures with improvements
- [ ] Create action items for failed success criteria
- [ ] Update risk register if new risks identified
- [ ] Archive test artifacts and documentation

---

## Success Criteria

### Overall Test Success Definition

A DR test is considered **SUCCESSFUL** if:

1. **RTO Met**: Recovery completed within target RTO (4 hours max)
2. **RPO Met**: Data loss within target RPO (1 hour max)
3. **Functionality Restored**: All authentication functions operational post-recovery
4. **No Data Corruption**: User data integrity verified
5. **Documented**: Complete test report generated with lessons learned

### Scenario-Specific Success Criteria

#### DR-AUTH-01: Azure AD B2C Regional Outage

| Criterion | Pass Threshold | Measurement Method |
|-----------|----------------|-------------------|
| Time to Detect Failure | < 3 minutes | Application Insights alert timestamp |
| Time to Initiate Failover | < 5 minutes | Runbook execution logs |
| Time to Complete Failover | < 60 minutes | End-to-end authentication test success |
| Data Loss | 0 users | User count comparison |
| Authentication Success Rate | > 99% | Application Insights success rate metric |
| User Impact | < 1% of active users | Failed request analysis by user ID |

**Pass/Fail Criteria:**
- **PASS**: All thresholds met
- **PARTIAL PASS**: RTO met, 1-2 criteria slightly exceeded
- **FAIL**: RTO exceeded OR > 2 criteria failed

#### DR-AUTH-02: Token Service Unavailability

| Criterion | Pass Threshold | Measurement Method |
|-----------|----------------|-------------------|
| Cache Hit Ratio During Outage | > 80% | Custom metric: TokenValidationCacheHitRatio |
| Time to Detect Failure | < 2 minutes | Health probe failure timestamp |
| Time to Full Recovery | < 120 minutes | Service health check success |
| Authentication Success (Cached) | 100% | Requests with cached token validation |
| Autoscale Trigger | < 5 minutes | Azure autoscale event logs |
| User Sessions Lost | 0 | Session store validation |

**Pass/Fail Criteria:**
- **PASS**: All thresholds met
- **PARTIAL PASS**: Recovery time slightly exceeded, no user impact
- **FAIL**: User sessions lost OR recovery time > 150 minutes

#### DR-AUTH-03: Key Vault Unavailability

| Criterion | Pass Threshold | Measurement Method |
|-----------|----------------|-------------------|
| Cache Validity Period | > 60 minutes | Secret last refresh timestamp |
| Auth Success (Cached Secrets) | 100% | Authentication requests during outage |
| Time to Failover | < 60 minutes | Secondary Key Vault first access |
| Secret Retrieval Success | 100% | Key Vault API success rate |
| Certificate Operations | 100% | TLS handshake success rate |

**Pass/Fail Criteria:**
- **PASS**: All thresholds met
- **PARTIAL PASS**: Failover time slightly exceeded, no auth failures
- **FAIL**: Authentication failures occurred OR secrets not retrieved

#### DR-AUTH-04: Database Failover

| Criterion | Pass Threshold | Measurement Method |
|-----------|----------------|-------------------|
| Failover Completion Time | < 10 minutes | SQL failover group status change |
| Data Loss | < 60 minutes | Transaction log analysis |
| Record Count Match | 100% | Table row count comparison |
| Application Auto-Reconnect | Success | Connection string failover validation |
| Query Success Rate | > 99% | SQL dependency success rate |
| Write Operation Success | 100% | INSERT/UPDATE statement success |

**Pass/Fail Criteria:**
- **PASS**: All thresholds met
- **PARTIAL PASS**: Minor data loss within RPO, all functions restored
- **FAIL**: Data loss > RPO OR write operations failed

#### DR-AUTH-05: Network Partitioning

| Criterion | Pass Threshold | Measurement Method |
|-----------|----------------|-------------------|
| Circuit Breaker Activation | < 30 seconds | Circuit breaker state change timestamp |
| Application Stability | No crashes | Exception count = 0 |
| Graceful Degradation | Enabled | 503 responses with retry-after headers |
| Circuit Breaker Recovery | < 120 seconds | State change to "Closed" after restore |
| Full Service Recovery | < 120 minutes | All dependencies healthy |
| Error Message Clarity | Clear & actionable | Manual review of error responses |

**Pass/Fail Criteria:**
- **PASS**: All thresholds met
- **PARTIAL PASS**: Recovery time exceeded, no crashes
- **FAIL**: Application crashed OR circuit breaker failed to activate

### Compliance Success Criteria

#### HIPAA Compliance Validation

| Requirement | Validation Method | Pass Criteria |
|-------------|-------------------|---------------|
| 45 CFR 164.308(a)(7)(ii)(B) - DR Plan Exists | Document review | DR procedures documented and current |
| 45 CFR 164.308(a)(7)(ii)(C) - Emergency Mode | Test execution | Emergency mode successfully activated |
| 45 CFR 164.308(a)(7)(ii)(E) - Testing | Test completion | Quarterly tests executed on schedule |
| Audit Trail Maintained | Log review | All DR activities logged with timestamp |
| Data Encryption During Failover | Encryption validation | TLS/encryption maintained throughout |

**Overall Compliance Pass:** All 5 criteria met

### Performance Success Criteria

| Metric | Normal Operations | During Failover | Pass Criteria |
|--------|------------------|----------------|---------------|
| Authentication Latency (P50) | < 200ms | < 500ms | Within threshold |
| Authentication Latency (P95) | < 500ms | < 2000ms | Within threshold |
| Token Validation (P95) | < 100ms | < 500ms | Within threshold |
| Database Query (P95) | < 50ms | < 200ms | Within threshold |
| Overall Availability | > 99.9% | > 99.0% | Within threshold |

**Performance Pass:** At least 4 of 5 metrics within threshold during failover

---

## Contact List & Escalation Matrix

### Primary Response Team

| Role | Name | Primary Contact | Secondary Contact | Availability |
|------|------|----------------|-------------------|--------------|
| DR Test Lead | [TBD] | [Email/Phone] | [Email/Phone] | During test window |
| Platform Engineering Lead | [TBD] | [Email/Phone] | [Email/Phone] | 24/7 on-call |
| Security Engineer | [TBD] | [Email/Phone] | [Email/Phone] | During test window |
| Database Administrator | [TBD] | [Email/Phone] | [Email/Phone] | During test window |
| Network Engineer | [TBD] | [Email/Phone] | [Email/Phone] | On-call |

### Management Escalation

| Level | Role | Name | Contact | Escalation Criteria |
|-------|------|------|---------|-------------------|
| Level 1 | Engineering Manager | [TBD] | [Email/Phone] | RTO at risk (> 50% elapsed) |
| Level 2 | Director of Engineering | [TBD] | [Email/Phone] | RTO exceeded OR data loss detected |
| Level 3 | VP Engineering | [TBD] | [Email/Phone] | Critical business impact |
| Level 4 | CTO | [TBD] | [Email/Phone] | Regulatory/compliance concern |
| Level 5 | CEO | [TBD] | [Email/Phone] | Company-wide impact |

### Vendor Contacts

| Vendor | Service | Contact Type | Contact Info | Support Tier |
|--------|---------|--------------|--------------|--------------|
| Microsoft Azure | Infrastructure & B2C | Premier Support | [Support Portal] | Severity A |
| Microsoft | Identity Platform | Support Case | 1-800-MICROSOFT | Premier |
| [Monitoring Vendor] | APM/Monitoring | TAM | [Email/Phone] | Enterprise |
| [Network Provider] | Connectivity | NOC | [Phone/Portal] | 24/7 |

### Communication Channels

| Channel | Purpose | Primary Use | Access |
|---------|---------|-------------|--------|
| Slack: #dr-testing | Real-time coordination | Test execution updates | All team members |
| Slack: #incidents-critical | Critical issues | RTO at risk, failures | On-call team |
| Teams: DR War Room | Video conference | Test coordination meetings | Stakeholders |
| PagerDuty | Alert routing | Critical alerts only | On-call engineers |
| Email: dr-team@emr.com | Official communication | Test notifications | Distribution list |

### Stakeholder Notification List

| Stakeholder Group | Notification Type | Lead Time | Contact Method |
|------------------|-------------------|-----------|----------------|
| Engineering Team | Full details | 5 business days | Email + Slack |
| Product Management | Impact summary | 5 business days | Email |
| Clinical Operations | User impact notice | 5 business days | Email |
| Customer Support | Support readiness | 3 business days | Email + training |
| Executive Leadership | Executive summary | 5 business days | Email |
| Compliance/Legal | Compliance validation | 5 business days | Email |

### Escalation Procedures

#### Level 1 Escalation (15 minutes into test)

**Trigger:**
- Test not progressing as planned
- Unexpected errors encountered
- Monitoring gaps identified

**Action:**
1. Alert DR Test Lead
2. Post update in #dr-testing Slack channel
3. Assess whether to continue or abort test

#### Level 2 Escalation (50% of RTO elapsed)

**Trigger:**
- Recovery taking longer than expected
- RTO at risk (> 50% of time elapsed)
- Multiple recovery attempts failed

**Action:**
1. Page Engineering Manager via PagerDuty
2. Escalate to #incidents-critical
3. Convene emergency bridge call
4. Consider engaging vendor support

#### Level 3 Escalation (RTO exceeded)

**Trigger:**
- RTO exceeded
- Data loss detected
- Unable to restore service

**Action:**
1. Page Director of Engineering
2. Notify VP Engineering and CTO
3. Engage Microsoft Premier Support (Severity A)
4. Activate incident management process
5. Prepare executive briefing

#### Level 4 Escalation (Business continuity at risk)

**Trigger:**
- Multiple recovery attempts exhausted
- Production data integrity concern
- Regulatory reporting required

**Action:**
1. Activate Crisis Management Team
2. Notify CEO and Board
3. Engage legal counsel
4. Prepare regulatory notifications (if HIPAA breach suspected)
5. Coordinate external communications

### Post-Incident Communication

**Within 1 Hour of Test Completion:**
- Slack update to #dr-testing with initial results
- Email to stakeholder notification list with summary

**Within 24 Hours:**
- Detailed test report to Engineering leadership
- Action items created in tracking system
- Post-mortem meeting scheduled

**Within 1 Week:**
- Executive summary to C-level
- Updated DR procedures published
- Lessons learned shared with team

---

## Test Report Template

### DR Test Report

**Test Date:** _____________________
**Test Duration:** _____ hours _____ minutes
**Test Lead:** _____________________
**Participants:** _____________________

#### Executive Summary

**Overall Result:** [ ] PASS  [ ] PARTIAL PASS  [ ] FAIL

**RTO Achievement:** Actual: _____ hours | Target: 4 hours | [ ] Met  [ ] Exceeded

**RPO Achievement:** Actual: _____ minutes | Target: 60 minutes | [ ] Met  [ ] Exceeded

**Brief Summary:**
[2-3 sentences describing test outcome]

#### Scenarios Tested

| Scenario ID | Scenario Name | Result | RTO (Actual) | Notes |
|-------------|---------------|--------|--------------|-------|
| DR-AUTH-01 | Azure AD B2C Outage | [ ] Pass [ ] Fail | _____ min | |
| DR-AUTH-02 | Token Service Unavailability | [ ] Pass [ ] Fail | _____ min | |
| DR-AUTH-03 | Key Vault Unavailability | [ ] Pass [ ] Fail | _____ min | |
| DR-AUTH-04 | Database Failover | [ ] Pass [ ] Fail | _____ min | |
| DR-AUTH-05 | Network Partitioning | [ ] Pass [ ] Fail | _____ min | |

#### Detailed Results

**Scenario:** [Name]

**Timeline:**
- T+0: [Event description]
- T+5: [Event description]
- T+10: [Event description]
- T+XX: Recovery complete

**Success Criteria Results:**

| Criterion | Target | Actual | Status |
|-----------|--------|--------|--------|
| [Criterion 1] | [Value] | [Value] | [ ] Pass [ ] Fail |
| [Criterion 2] | [Value] | [Value] | [ ] Pass [ ] Fail |

**Metrics:**

| Metric | Pre-Test | During Test | Post-Recovery |
|--------|----------|-------------|---------------|
| Auth Success Rate | __% | __% | __% |
| P95 Latency | __ms | __ms | __ms |
| Active Sessions | ___ | ___ | ___ |

#### Issues Encountered

| Issue ID | Severity | Description | Resolution | Owner |
|----------|----------|-------------|------------|-------|
| DR-2025-001 | High/Med/Low | [Description] | [Resolution] | [Owner] |

#### Lessons Learned

**What Went Well:**
1. [Item]
2. [Item]
3. [Item]

**What Needs Improvement:**
1. [Item] - Action: [Action item] - Owner: [Owner] - Due: [Date]
2. [Item] - Action: [Action item] - Owner: [Owner] - Due: [Date]

**Documentation Gaps:**
1. [Gap identified] - Action: [Update needed]

**Process Improvements:**
1. [Improvement] - Priority: [High/Med/Low]

#### Action Items

| ID | Action | Owner | Due Date | Priority | Status |
|----|--------|-------|----------|----------|--------|
| AI-001 | [Action] | [Owner] | [Date] | High/Med/Low | Open |

#### Compliance Validation

- [ ] HIPAA DR testing requirement met (45 CFR 164.308(a)(7)(ii)(E))
- [ ] All DR activities logged and auditable
- [ ] Data encryption maintained throughout test
- [ ] No PHI exposed or compromised
- [ ] Test artifacts archived per retention policy

#### Approvals

**Test Lead:** _____________________ Date: _____

**Engineering Manager:** _____________________ Date: _____

**CISO (if compliance issues):** _____________________ Date: _____

---

## Appendix

### Useful Commands Reference

```bash
# Quick health check
curl https://emr-api-prod.azurewebsites.net/api/health

# Check application logs
az webapp log tail --resource-group emr-prod-rg --name emr-api-prod

# Get current B2C configuration
az webapp config appsettings list --resource-group emr-prod-rg --name emr-api-prod \
  | jq '.[] | select(.name | contains("AzureAdB2C"))'

# Check SQL failover group status
az sql failover-group show --resource-group emr-prod-rg --server emr-sql-primary --name emr-sql-fog

# View recent Application Insights metrics
az monitor app-insights query --app emr-appinsights \
  --analytics-query "requests | where timestamp > ago(15m) | summarize count() by resultCode"

# Test authentication endpoint
curl -X POST https://emrprimary.b2clogin.com/emrprimary.onmicrosoft.com/oauth2/v2.0/token \
  -d "grant_type=client_credentials&client_id=$CLIENT_ID&client_secret=$CLIENT_SECRET&scope=https://emrprimary.onmicrosoft.com/.default"
```

### Related Documentation

- [EMR RBAC Implementation Guide](./RBAC_IMPLEMENTATION.md)
- [EMR Authentication Architecture](./AUTHENTICATION_ARCHITECTURE.md) _(if exists)_
- [Incident Response Procedures](./INCIDENT_RESPONSE.md) _(if exists)_
- [HIPAA Compliance Guide](./HIPAA_COMPLIANCE.md) _(if exists)_

### Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-12-28 | Initial Author | Initial creation |

---

**Document Classification:** Internal - Confidential
**Next Review Date:** 2025-03-28
**Document Owner:** Platform Engineering Team

