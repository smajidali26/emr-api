# EMR Performance Test Suite

Comprehensive performance testing suite for the EMR API using [k6](https://k6.io/), a modern load testing tool. Covers both User Authentication and HIPAA Audit Logging systems.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Test Files](#test-files)
  - [Authentication Tests](#authentication-tests)
  - [HIPAA Audit Tests](#hipaa-audit-tests)
- [Running Tests](#running-tests)
  - [Authentication Tests](#running-authentication-tests)
  - [Audit Tests](#running-audit-tests)
- [Test Scenarios](#test-scenarios)
- [Performance Targets](#performance-targets)
- [Interpreting Results](#interpreting-results)
- [CI/CD Integration](#cicd-integration)
- [Troubleshooting](#troubleshooting)
- [Best Practices](#best-practices)

---

## Overview

This test suite validates the performance, scalability, and reliability of the EMR API under various load conditions. It covers two main areas:

### Authentication Testing
- **Authentication flows**: Registration, login, token refresh, CSRF protection
- **Rate limiting**: Boundary testing for all rate limit policies
- **JWT validation**: Performance impact of token validation on every request
- **User caching**: 5-minute TTL cache performance
- **System resilience**: Stress, spike, and soak testing

### HIPAA Audit Logging Testing
- **7-year query performance**: Validate queries across 2,555 days complete in < 5 seconds (HIPAA requirement)
- **TimescaleDB aggregates**: Test continuous aggregate query performance (< 100ms target)
- **Audit log writes**: Validate audit logging overhead doesn't impact API response times
- **Export functionality**: Test streaming export for large compliance reports
- **100% coverage**: Verify no audit logs are dropped under load

### System Under Test

- **Backend API**: ASP.NET Core 8.0 with Azure AD B2C authentication
- **Database**: PostgreSQL with TimescaleDB extension for audit logging
- **Authentication Endpoints**:
  - `POST /api/auth/register` - User registration
  - `POST /api/auth/login-callback` - Post-login processing
  - `GET /api/auth/me` - Get current user profile
  - `GET /api/auth/csrf-token` - CSRF token generation
- **Audit Endpoints**:
  - `GET /api/audit` - Paginated audit log query
  - `GET /api/audit/compliance/metrics` - Compliance metrics (uses continuous aggregates)
  - `GET /api/audit/daily-summaries` - Daily event summaries (uses continuous aggregates)
  - `GET /api/audit/users/{userId}/activity` - User activity timeline
  - `GET /api/audit/resources/{type}/{id}/access` - Resource access history
  - `GET /api/audit/storage/stats` - TimescaleDB storage statistics
  - `GET /api/audit/export/stream` - Streaming export for compliance reports
- **Rate Limits**:
  - Global: 100 requests/minute per IP
  - Auth endpoints: 10 requests/5 minutes per IP
  - Patient search: 30 requests/minute per IP
- **Security**: JWT validation on every request, CSRF protection, HIPAA audit logging

---

## Prerequisites

### Required Software

1. **k6** (latest version)
   - Installation: https://k6.io/docs/get-started/installation/
   - Verify: `k6 version`

2. **Node.js** (v14+ for helper scripts)
   - Optional, only if using data generation scripts

### Environment Setup

The API endpoint must be accessible and running before executing tests.

**Default Configuration**:
- Base URL: `https://localhost:5001`
- Environment: `test`

**Override via Environment Variables**:
```bash
export BASE_URL=https://api.example.com
export ENVIRONMENT=staging
export TARGET_VUS=200
export PATIENT_COUNT=100
```

### API Requirements

Ensure the API is configured with:
- ✅ Rate limiting enabled
- ✅ CSRF protection active
- ✅ JWT authentication configured
- ✅ HIPAA audit logging enabled
- ✅ User caching with 5-minute TTL
- ✅ Health check endpoint available (`/health`)

---

## Installation

### 1. Install k6

**macOS** (Homebrew):
```bash
brew install k6
```

**Windows** (Chocolatey):
```bash
choco install k6
```

**Linux** (Debian/Ubuntu):
```bash
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

**Docker**:
```bash
docker pull grafana/k6:latest
```

### 2. Verify Installation

```bash
k6 version
# Expected output: k6 v0.48.0 (or later)
```

### 3. Clone or Navigate to Test Directory

```bash
cd D:\code-source\EMR\source\emr-api\tests\performance
```

---

## Test Files

### 1. `auth-performance-config.js`

**Purpose**: Shared configuration, helper functions, and metrics

**Key Components**:
- Environment configuration (base URL, Azure B2C settings)
- Test data generators (users, search queries)
- Custom metrics (login duration, cache hits, audit logs)
- Validation helpers (response checking, error tracking)
- Performance thresholds (p50 < 200ms, p95 < 500ms, p99 < 1000ms)

**Usage**: Imported by all test files

### 2. `auth-load-test.js`

**Purpose**: Standard load testing under normal and peak conditions

**Test Scenarios**:
- Complete authentication flow (register → login → get user)
- Token refresh under load
- Authenticated API calls with JWT validation
- Rate limit boundary testing
- CSRF token performance
- User cache effectiveness (5-minute TTL)

**Default Load Profile**:
- Ramp up: 0 → 100 VUs over 10 minutes
- Steady state: 100 VUs for 5 minutes
- Ramp down: 100 → 0 VUs over 3 minutes

**Performance Targets**:
- p95 response time: < 500ms
- p99 response time: < 1000ms
- Error rate: < 1%
- Rate limit hits: < 5% (under normal load)

### 3. `auth-stress-test.js`

**Purpose**: Test system behavior under extreme conditions

**Test Types**:

1. **Stress Test**: Gradually increase load to find breaking point (up to 500 VUs)
2. **Spike Test**: Sudden load increase (50 → 500 VUs in 30 seconds)
3. **Soak Test**: Extended duration test (30 minutes at 100 VUs) to detect memory leaks
4. **Breakpoint Test**: Find maximum sustainable throughput

**Goals**:
- Identify system capacity limits
- Verify graceful degradation under overload
- Test rate limiting effectiveness at scale
- Detect resource exhaustion and memory leaks
- Validate error handling under stress

### HIPAA Audit Tests

### 4. `audit-performance-config.js`

**Purpose**: Shared configuration and helpers for HIPAA audit performance tests

**Key Components**:
- Environment configuration (base URL, admin token)
- Custom metrics for audit-specific tracking:
  - `seven_year_query_duration`: Critical HIPAA metric
  - `compliance_metrics_duration`: Aggregate query performance
  - `audit_log_query_duration`: Paginated query performance
  - `export_duration`: Export operation time
- Date range generators (7-year, random ranges)
- Response validators with HIPAA-specific thresholds
- Test data generators for realistic queries

**HIPAA Thresholds**:
- 7-year query: < 5000ms (< 5 seconds)
- Aggregate queries (p95): < 200ms
- Standard queries (p95): < 500ms
- Error rate: < 1%

### 5. `audit-load-test.js`

**Purpose**: Standard load testing for HIPAA audit endpoints

**Test Scenarios**:
- **Warmup**: 5 VUs for 30 seconds
- **Load Test**: Ramp from 0 → 25 → 50 VUs over 9 minutes
- **7-Year Queries**: 3 VUs dedicated to testing HIPAA retention queries

**Endpoints Tested**:
- Paginated audit log queries with filters
- Compliance metrics (continuous aggregates)
- Daily summaries (continuous aggregates)
- 7-year range queries (critical HIPAA requirement)
- Storage statistics
- User activity timelines

**Performance Targets**:
- 7-year query (p95): < 5000ms
- Aggregate queries (p95): < 200ms
- Standard queries (p95): < 500ms
- Error rate: < 1%

### 6. `audit-stress-test.js`

**Purpose**: Stress testing HIPAA audit system under extreme conditions

**Test Types**:

1. **Spike Test** (`STRESS_TEST_TYPE=spike`):
   - Baseline at 10 VUs → spike to 100 VUs → spike to 150 VUs
   - Tests system recovery from sudden traffic surges

2. **Ramp Test** (`STRESS_TEST_TYPE=ramp`):
   - Gradual increase: 0 → 25 → 50 → 75 → 100 → 125 → 150 → 175 → 200 VUs
   - Identifies breaking point

3. **Soak Test** (`STRESS_TEST_TYPE=soak`):
   - 50 VUs for 30 minutes
   - Detects memory leaks and resource exhaustion

4. **7-Year Stress** (`STRESS_TEST_TYPE=seven_year_stress`):
   - Multiple concurrent 7-year range queries
   - Validates HIPAA compliance under load

5. **Export Stress** (`STRESS_TEST_TYPE=export_stress`):
   - 10 VUs each running 3 large exports
   - Tests streaming export performance

**Relaxed Thresholds** (stress conditions):
- Error rate: < 20% (vs. < 1% normal)
- 7-year query (p95): < 10000ms (vs. < 5000ms normal)

### 7. `audit-write-test.js`

**Purpose**: Test audit log write performance and coverage

**Test Scenarios**:
- **Sustained Writes**: 20 VUs for 3 minutes
- **Burst Writes**: Ramping arrival rate 10 → 50 → 100 requests/second

**Metrics Tracked**:
- Audit overhead per request
- Total auditable requests
- Audit log verification rate
- Batch write performance

**Targets**:
- Audit overhead (p95): < 50ms
- 100% audit coverage (no dropped logs)
- Request duration (p95): < 500ms

---

## Running Tests

### Running Authentication Tests

### Basic Usage

**Run Load Test** (Default configuration):
```bash
k6 run auth-load-test.js
```

**Run Stress Test**:
```bash
k6 run auth-stress-test.js
```

### Customized Execution

**Override Virtual Users**:
```bash
k6 run --vus 200 auth-load-test.js
```

**Override Duration**:
```bash
k6 run --duration 10m auth-load-test.js
```

**Override Base URL**:
```bash
BASE_URL=https://staging-api.example.com k6 run auth-load-test.js
```

**Run Specific Scenario** (Stress test):
```bash
k6 run --scenario stress auth-stress-test.js
k6 run --scenario spike auth-stress-test.js
k6 run --scenario soak auth-stress-test.js
```

### Advanced Options

**Output Results to JSON**:
```bash
k6 run --out json=results.json auth-load-test.js
```

**Output to InfluxDB** (for Grafana dashboards):
```bash
k6 run --out influxdb=http://localhost:8086/k6 auth-load-test.js
```

**Output to Cloud** (k6 Cloud):
```bash
k6 cloud auth-load-test.js
```

**Run with Docker**:
```bash
docker run --rm -i grafana/k6 run - < auth-load-test.js
```

### Environment Variables

**Available Configuration**:

| Variable | Default | Description |
|----------|---------|-------------|
| `BASE_URL` | `https://localhost:5001` | API base URL |
| `ENVIRONMENT` | `test` | Environment name |
| `TARGET_VUS` | `100` | Target virtual users |
| `PATIENT_COUNT` | `50` | Number of test patients |
| `DOCTOR_COUNT` | `10` | Number of test doctors |
| `NURSE_COUNT` | `10` | Number of test nurses |
| `AZURE_B2C_AUTHORITY` | (see config) | Azure B2C authority URL |
| `AZURE_B2C_CLIENT_ID` | (see config) | Azure B2C client ID |

**Example with Multiple Overrides**:
```bash
BASE_URL=https://prod-api.example.com \
ENVIRONMENT=production \
TARGET_VUS=500 \
PATIENT_COUNT=200 \
k6 run auth-load-test.js
```

### Running Audit Tests

#### Using npm Scripts

**Run Audit Load Test**:
```bash
npm run audit:load
```

**Run Audit Stress Test** (default ramp):
```bash
npm run audit:stress
```

**Run Specific Stress Scenario**:
```bash
npm run audit:stress:spike       # Spike test
npm run audit:stress:ramp        # Ramp to breaking point
npm run audit:stress:soak        # Extended duration
npm run audit:stress:7year       # 7-year query stress
npm run audit:stress:export      # Export stress test
```

**Run Audit Write Test**:
```bash
npm run audit:write
npm run audit:write:quick        # Quick 1-minute version
```

**Run All Audit Tests**:
```bash
npm run audit:all
```

#### Direct k6 Execution

**Run with Custom Base URL**:
```bash
k6 run -e BASE_URL=https://api.example.com -e ADMIN_TOKEN=your-token audit-load-test.js
```

**Run with JSON Output**:
```bash
k6 run --out json=audit-results.json audit-load-test.js
```

**Override Virtual Users**:
```bash
k6 run --vus 100 audit-load-test.js
```

#### Audit Test Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `BASE_URL` | `https://localhost:5001` | API base URL |
| `ADMIN_TOKEN` | (required) | JWT token with Admin role |
| `TARGET_VUS` | `50` | Target virtual users |
| `STRESS_TEST_TYPE` | `ramp` | Stress test type (spike/ramp/soak/seven_year_stress/export_stress) |

**Example**:
```bash
BASE_URL=https://staging-api.example.com \
ADMIN_TOKEN=eyJhbGciOiJSUzI1NiIsInR5cCI6... \
STRESS_TEST_TYPE=seven_year_stress \
k6 run audit-stress-test.js
```

---

## Test Scenarios

### Healthcare-Specific Patterns

The test suite simulates realistic healthcare authentication patterns:

#### 1. Patient Portal Login (70% of traffic)
- Patient users accessing their own medical records
- Personal health information (PHI) access
- Lower privileges, higher volume

#### 2. Provider Workflow (20% of traffic)
- Doctors and nurses accessing patient records
- Patient search functionality
- Higher privileges, moderate volume

#### 3. Administrative Tasks (10% of traffic)
- Staff managing user accounts
- Administrative operations
- Highest privileges, lower volume

### Load Distribution

**User Role Distribution**:
- Patients: 70%
- Doctors: 15%
- Nurses: 10%
- Staff/Admin: 5%

**Request Distribution**:
- Authentication (login/register): 30%
- Profile access (/auth/me): 40%
- API calls with JWT validation: 25%
- CSRF token fetching: 5%

---

## Performance Targets

### Response Time Targets

| Metric | Target | Threshold |
|--------|--------|-----------|
| p50 (median) | < 200ms | Excellent user experience |
| p95 (95th percentile) | < 500ms | Acceptable for most users |
| p99 (99th percentile) | < 1000ms | Maximum acceptable |
| Max | < 5000ms | Hard limit (stress test) |

### Error Rate Targets

| Metric | Target | Threshold |
|--------|--------|-----------|
| HTTP errors | < 1% | Normal load |
| HTTP errors | < 20% | Stress test |
| Server errors (5xx) | < 0.1% | Critical |
| Rate limit hits | < 5% | Normal load |
| Rate limit hits | < 50% | Stress test |

### Throughput Targets

| Scenario | Target | Description |
|----------|--------|-------------|
| Normal load | 100 req/s | Typical production load |
| Peak load | 200 req/s | Holiday/high-traffic periods |
| Maximum sustainable | TBD | Found via breakpoint test |

### System-Specific Targets

**Rate Limiting**:
- Global: 100 requests/minute should be enforced
- Auth endpoints: 10 requests/5 minutes should block excess
- Patient search: 30 requests/minute should be enforced

**Caching**:
- Cache hit rate: > 80% for repeated /auth/me calls
- Cache response time: < 50ms (vs. 200ms uncached)

**Audit Logging**:
- 100% of PHI access must be logged
- Audit logging should add < 10ms overhead
- No silent audit failures

### HIPAA Audit Performance Targets

**Query Performance** (Critical HIPAA Requirements):

| Query Type | Target (p95) | Threshold | Notes |
|------------|--------------|-----------|-------|
| 7-year range query | < 5000ms | Critical | HIPAA retention requirement |
| Compliance metrics | < 200ms | Target | Uses continuous aggregates |
| Daily summaries | < 200ms | Target | Uses continuous aggregates |
| Paginated audit logs | < 500ms | Standard | With filters |
| User activity | < 500ms | Standard | Individual user timeline |
| Storage stats | < 1000ms | Acceptable | TimescaleDB metadata |

**Export Performance**:

| Export Size | Target | Threshold |
|-------------|--------|-----------|
| 7 days | < 30s | Normal |
| 30 days | < 60s | Normal |
| 90 days | < 120s | Maximum |
| 365 days | < 180s | Large export |

**Write Performance**:

| Metric | Target | Threshold |
|--------|--------|-----------|
| Audit overhead per request | < 50ms (p95) | Minimal impact |
| Audit coverage rate | 100% | No dropped logs |
| Batch write (per log) | < 100ms | Bulk operations |

**TimescaleDB-Specific**:
- Compression ratio: > 10:1 after 30 days
- Chunk management: < 1 second for chunk operations
- Aggregate refresh: < 30 seconds

---

## Interpreting Results

### k6 Output Explained

**Sample Output**:
```
     ✓ login_callback: status is 2xx
     ✓ login_callback: response time < 500ms

     checks.........................: 98.52% ✓ 9852  ✗ 148
     data_received..................: 15 MB  50 kB/s
     data_sent......................: 3.2 MB 11 kB/s
     http_req_blocked...............: avg=1.23ms  min=0s    med=0s    max=1.5s  p(90)=0s    p(95)=0s
     http_req_connecting............: avg=615µs   min=0s    med=0s    max=1.2s  p(90)=0s    p(95)=0s
     http_req_duration..............: avg=145ms   min=12ms  med=98ms  max=3.2s  p(90)=285ms p(95)=421ms
       { expected_response:true }...: avg=142ms   min=12ms  med=97ms  max=1.1s  p(90)=280ms p(95)=410ms
     http_req_failed................: 1.47%  ✓ 148   ✗ 9852
     http_req_receiving.............: avg=245µs   min=0s    med=0s    max=156ms p(90)=0s    p(95)=498µs
     http_req_sending...............: avg=89µs    min=0s    med=0s    max=48ms  p(90)=0s    p(95)=0s
     http_req_tls_handshaking.......: avg=615µs   min=0s    med=0s    max=1.2s  p(90)=0s    p(95)=0s
     http_req_waiting...............: avg=144ms   min=12ms  med=97ms  max=3.2s  p(90)=284ms p(95)=420ms
     http_reqs......................: 10000  33.33/s
     iteration_duration.............: avg=2.9s    min=2.1s  med=2.8s  max=8.5s  p(90)=3.5s  p(95)=4.1s
     iterations.....................: 1000   3.33/s
     vus............................: 100    min=0   max=100
     vus_max........................: 100    min=100 max=100
```

### Key Metrics

**1. Checks** (`checks`):
- Percentage of validation checks that passed
- **Target**: > 99%
- **Action if low**: Investigate validation failures

**2. HTTP Request Duration** (`http_req_duration`):
- Time from request sent to response received
- **Key values**: p(95) and p(99)
- **Target**: p(95) < 500ms
- **Action if high**: Investigate slow endpoints, database queries, or external dependencies

**3. HTTP Request Failed** (`http_req_failed`):
- Percentage of requests that failed (non-2xx status)
- **Target**: < 1%
- **Action if high**: Check server logs, investigate errors

**4. Requests per Second** (`http_reqs`):
- Total throughput
- **Target**: Depends on scenario (100 req/s for normal load)
- **Action if low**: Check if rate limiting is too aggressive or system is overloaded

**5. Virtual Users** (`vus` / `vus_max`):
- Number of concurrent users
- **Use**: Verify test executed with expected load

### Custom Metrics

**Authentication Metrics**:
- `login_duration`: Time to complete login flow
- `successful_logins`: Count of successful authentications
- `auth_errors`: Rate of authentication failures

**Performance Metrics**:
- `csrf_token_duration`: CSRF token fetch time
- `token_refresh_duration`: Token refresh time
- `api_call_with_auth_duration`: API call time including JWT validation

**System Metrics**:
- `rate_limit_errors`: Percentage of rate-limited requests
- `user_cache_hits`: Cache hit count
- `user_cache_misses`: Cache miss count
- `audit_logs_generated`: HIPAA audit log count

**HIPAA Audit Metrics**:
- `seven_year_query_duration`: Time to query 7-year range (critical)
- `compliance_metrics_duration`: Aggregate query performance
- `daily_summary_duration`: Daily summary query time
- `audit_log_query_duration`: Paginated query performance
- `export_duration`: Export operation time
- `audit_query_errors`: Rate of audit query failures
- `aggregate_hits`: Count of aggregate queries
- `compression_ratio`: TimescaleDB compression ratio
- `auditable_request_duration`: Request time for audited operations
- `audit_overhead`: Estimated audit logging overhead
- `audit_coverage_rate`: Percentage of requests with verified audit logs

### Threshold Violations

**Pass/Fail Indicators**:
- ✅ **Green checkmark**: Threshold passed
- ❌ **Red X**: Threshold failed

**Example**:
```
✅ http_req_duration.............: p(95) < 500ms  [actual: 421ms]
❌ http_req_failed...............: rate < 0.01    [actual: 0.0147 (1.47%)]
```

**Action on Failures**:
1. Review detailed metrics to identify bottlenecks
2. Check server logs for errors and warnings
3. Analyze database query performance
4. Review rate limiting configuration
5. Investigate HIPAA audit logging overhead

---

## CI/CD Integration

### GitHub Actions

**Example Workflow** (`.github/workflows/performance-test.yml`):

```yaml
name: Performance Tests

on:
  schedule:
    - cron: '0 2 * * *'  # Daily at 2 AM
  workflow_dispatch:      # Manual trigger

jobs:
  performance-test:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout code
        uses: actions/checkout@v3

      - name: Install k6
        run: |
          sudo gpg -k
          sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
          echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
          sudo apt-get update
          sudo apt-get install k6

      - name: Run Load Test
        env:
          BASE_URL: ${{ secrets.API_BASE_URL }}
          ENVIRONMENT: staging
        run: |
          cd tests/performance
          k6 run --out json=load-test-results.json auth-load-test.js

      - name: Upload Results
        uses: actions/upload-artifact@v3
        with:
          name: performance-results
          path: tests/performance/load-test-results.json

      - name: Fail on Threshold Violations
        run: |
          # k6 exits with non-zero code if thresholds fail
          exit $?
```

### Azure DevOps

**Example Pipeline** (`azure-pipelines.yml`):

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: Bash@3
    displayName: 'Install k6'
    inputs:
      targetType: 'inline'
      script: |
        sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
        echo "deb https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
        sudo apt-get update
        sudo apt-get install k6

  - task: Bash@3
    displayName: 'Run Performance Tests'
    inputs:
      targetType: 'inline'
      script: |
        cd tests/performance
        k6 run --out json=results.json auth-load-test.js
    env:
      BASE_URL: $(API_BASE_URL)
      ENVIRONMENT: $(ENVIRONMENT)

  - task: PublishBuildArtifacts@1
    displayName: 'Publish Results'
    inputs:
      pathToPublish: 'tests/performance/results.json'
      artifactName: 'performance-results'
```

### Jenkins

**Example Jenkinsfile**:

```groovy
pipeline {
    agent any

    environment {
        BASE_URL = credentials('api-base-url')
        ENVIRONMENT = 'staging'
    }

    stages {
        stage('Install k6') {
            steps {
                sh '''
                    sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
                    echo "deb https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
                    sudo apt-get update
                    sudo apt-get install k6
                '''
            }
        }

        stage('Run Performance Tests') {
            steps {
                dir('tests/performance') {
                    sh 'k6 run --out json=results.json auth-load-test.js'
                }
            }
        }

        stage('Archive Results') {
            steps {
                archiveArtifacts artifacts: 'tests/performance/results.json', fingerprint: true
            }
        }
    }

    post {
        always {
            publishHTML([
                reportDir: 'tests/performance',
                reportFiles: 'results.json',
                reportName: 'Performance Test Results'
            ])
        }
    }
}
```

### Performance Regression Detection

**Automated Comparison**:

```bash
# Run baseline test
k6 run --out json=baseline.json auth-load-test.js

# Run current test
k6 run --out json=current.json auth-load-test.js

# Compare results (custom script)
node compare-results.js baseline.json current.json
```

**Alerting on Degradation**:
- Set up alerts for > 20% increase in p95 response time
- Alert on > 2% increase in error rate
- Notify on rate limit threshold violations

---

## Troubleshooting

### Common Issues

#### 1. Connection Refused

**Error**:
```
ERRO[0001] GoError: Get "https://localhost:5001/health": dial tcp [::1]:5001: connect: connection refused
```

**Solution**:
- Verify API is running: `curl https://localhost:5001/health`
- Check BASE_URL is correct
- Ensure SSL certificate is trusted (or use `--insecure-skip-tls-verify`)

#### 2. High Error Rate

**Symptoms**: `http_req_failed > 10%`

**Potential Causes**:
- API is overloaded (reduce VUs or increase resources)
- Rate limiting is too aggressive
- Authentication tokens are invalid
- CSRF protection is blocking requests

**Debugging**:
```bash
# Run with verbose output
k6 run --http-debug=full auth-load-test.js

# Check specific error responses
k6 run --console-output=stdout auth-load-test.js | grep "status"
```

#### 3. Slow Response Times

**Symptoms**: `http_req_duration p(95) > 2000ms`

**Potential Causes**:
- Database query performance
- External dependency latency (Azure B2C)
- Insufficient server resources
- Network latency

**Debugging**:
1. Check server CPU/memory usage during test
2. Review database slow query logs
3. Analyze k6 output for specific slow endpoints
4. Use APM tools (Application Insights, New Relic, etc.)

#### 4. Rate Limit Not Enforced

**Symptoms**: `rate_limit_errors = 0%` when expected

**Solution**:
- Verify rate limiting middleware is enabled
- Check rate limit configuration in `Program.cs`
- Ensure tests are using same IP address (not distributed)
- Review rate limit window timing

#### 5. CSRF Token Failures

**Symptoms**: Many 403 Forbidden responses

**Solution**:
- Verify CSRF middleware is configured correctly
- Check cookie settings (Secure, SameSite)
- Ensure `X-CSRF-Token` header is included
- Test CSRF flow manually first

### Debug Mode

**Enable Verbose Logging**:
```bash
k6 run --http-debug=full --verbose auth-load-test.js
```

**Capture HTTP Traffic**:
```bash
k6 run --http-debug=headers auth-load-test.js > debug.log 2>&1
```

### Performance Profiling

**Identify Bottlenecks**:

1. **Review k6 Metrics**:
   - Sort by slowest p(95): `http_req_duration{endpoint:...}`
   - Identify endpoints with high error rates

2. **Server-Side Profiling**:
   - Enable ASP.NET Core diagnostic logging
   - Use Application Insights or similar APM
   - Review database query execution plans

3. **Network Profiling**:
   - Use Wireshark or tcpdump to capture traffic
   - Analyze SSL/TLS handshake times
   - Check for packet loss or retransmissions

---

## Best Practices

### Test Design

1. **Start Small**: Begin with low VUs and gradually increase
2. **Isolate Variables**: Test one scenario at a time
3. **Use Realistic Data**: Generate representative test users
4. **Simulate Think Time**: Add appropriate delays between requests
5. **Test in Stages**: Load → Stress → Soak progression

### Execution

1. **Dedicated Environment**: Run tests against non-production environments
2. **Consistent Baseline**: Always compare against baseline measurements
3. **Monitor System Resources**: CPU, memory, disk I/O during tests
4. **Network Stability**: Ensure stable network connection
5. **Avoid Interference**: Don't run other heavy processes during tests

### Analysis

1. **Focus on Trends**: Look for patterns over time, not single data points
2. **Correlate Metrics**: Cross-reference k6 metrics with server logs
3. **Document Findings**: Record observations and action items
4. **Iterate**: Re-test after optimizations to validate improvements
5. **Share Results**: Communicate findings with team

### Healthcare-Specific Considerations

1. **HIPAA Compliance**: Ensure test data contains no real PHI
2. **Audit Logging**: Verify all PHI access is logged even under load
3. **Rate Limiting**: Confirm rate limits protect against abuse
4. **User Roles**: Test with realistic role distribution
5. **Cache Effectiveness**: Validate caching doesn't compromise security

### Maintenance

1. **Regular Execution**: Run tests weekly or on major changes
2. **Update Thresholds**: Adjust targets as system evolves
3. **Review Dependencies**: Keep k6 and test scripts up to date
4. **Retire Obsolete Tests**: Remove tests for deprecated endpoints
5. **Document Changes**: Track modifications to test suite

---

## Additional Resources

### k6 Documentation

- [Official k6 Docs](https://k6.io/docs/)
- [k6 Examples](https://k6.io/docs/examples/)
- [k6 Cloud](https://k6.io/cloud/)

### Healthcare Performance Testing

- [HIPAA Security Rule - Technical Safeguards](https://www.hhs.gov/hipaa/for-professionals/security/laws-regulations/index.html)
- [Healthcare IT Performance Standards](https://www.healthit.gov/)

### Related Tools

- [Grafana](https://grafana.com/) - Visualization
- [InfluxDB](https://www.influxdata.com/) - Time-series database
- [Prometheus](https://prometheus.io/) - Monitoring
- [Application Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview) - Azure APM

---

## Support

For questions or issues:

1. **Check Documentation**: Review this README and k6 docs
2. **Review Logs**: Check k6 output and server logs
3. **Search Issues**: Look for similar problems in project issues
4. **Contact Team**: Reach out to performance testing team
5. **File Bug Report**: Create detailed issue with reproduction steps

---

## License

This test suite is part of the EMR project and follows the same license.

---

**Last Updated**: 2025-12-28
**Version**: 1.0.0
**Maintainer**: EMR Performance Testing Team
