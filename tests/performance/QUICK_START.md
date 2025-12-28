# Quick Start Guide - EMR Authentication Performance Tests

Get up and running with performance tests in 5 minutes.

## Prerequisites

Install k6:

**macOS**:
```bash
brew install k6
```

**Windows**:
```bash
choco install k6
```

**Linux**:
```bash
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

Verify installation:
```bash
k6 version
```

## Running Your First Test

### 1. Basic Load Test

```bash
cd D:\code-source\EMR\source\emr-api\tests\performance
k6 run auth-load-test.js
```

This runs a 18-minute load test:
- Ramps up from 0 to 100 concurrent users
- Tests authentication, API calls, rate limits
- Provides detailed performance metrics

### 2. Quick Smoke Test

For faster feedback during development:

```bash
k6 run --vus 10 --duration 2m auth-load-test.js
```

This runs a 2-minute test with 10 users.

### 3. Custom Target Load

```bash
k6 run --vus 200 --duration 5m auth-load-test.js
```

Test with 200 concurrent users for 5 minutes.

## Understanding Results

After the test completes, you'll see:

```
✓ login_callback: status is 2xx
✓ login_callback: response time < 500ms

checks.........................: 98.52% ✓ 9852  ✗ 148
http_req_duration..............: avg=145ms med=98ms p(95)=421ms p(99)=875ms
http_req_failed................: 1.47%
successful_logins..............: 1000
```

### Key Metrics to Watch

| Metric | Good | Warning | Critical |
|--------|------|---------|----------|
| **http_req_duration p(95)** | < 500ms | 500-1000ms | > 1000ms |
| **http_req_failed** | < 1% | 1-5% | > 5% |
| **checks** | > 99% | 95-99% | < 95% |

## Common Scenarios

### Test Against Staging

```bash
BASE_URL=https://staging-api.example.com k6 run auth-load-test.js
```

### Run Stress Test

```bash
k6 run --scenario stress auth-stress-test.js
```

### Run Spike Test

```bash
k6 run --scenario spike auth-stress-test.js
```

### Run 30-Minute Soak Test

```bash
k6 run --scenario soak auth-stress-test.js
```

## Troubleshooting

### "Connection Refused"

Ensure the API is running:
```bash
curl https://localhost:5001/health
```

### High Error Rate

Check if rate limiting is working correctly:
```bash
k6 run --vus 5 auth-load-test.js
```

Start with fewer users to verify basic functionality.

### Slow Response Times

Run against local environment first:
```bash
BASE_URL=http://localhost:5000 k6 run auth-load-test.js
```

## Next Steps

1. Read the full [README.md](./README.md) for detailed documentation
2. Customize test scenarios in `auth-load-test.js`
3. Adjust thresholds in `auth-performance-config.js`
4. Integrate tests into CI/CD pipeline

## Quick Tips

- Always run a smoke test first (10 VUs, 2 min)
- Compare results against baseline measurements
- Monitor server resources during tests
- Review server logs for errors
- Start small and gradually increase load

## Need Help?

- Check [README.md](./README.md) for comprehensive documentation
- Review [k6 documentation](https://k6.io/docs/)
- Contact the performance testing team

---

Happy testing!
