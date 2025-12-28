/**
 * EMR HIPAA Audit Stress Test
 *
 * Stress testing for HIPAA Audit Logging endpoints.
 * Tests system behavior under extreme load conditions,
 * validates graceful degradation, and identifies breaking points.
 *
 * Usage:
 *   k6 run audit-stress-test.js
 *   k6 run -e BASE_URL=https://api.example.com -e ADMIN_TOKEN=xxx audit-stress-test.js
 *
 * STRESS TEST SCENARIOS:
 * - Spike test: Sudden traffic surge
 * - Ramp test: Gradual increase to breaking point
 * - Soak test: Extended duration at moderate load
 * - Concurrent 7-year queries: Multiple admins running reports
 */

import http from 'k6/http';
import { sleep, group, check } from 'k6';
import {
    config,
    metrics,
    getDateRange,
    getSevenYearRange,
    generateAuditLogQuery,
    generateComplianceMetricsQuery,
    validateAuditResponse,
    validateSevenYearQuery,
    validateExportResponse,
    getAuditHeaders,
    buildAuditUrl,
    thinkTime,
    formatAuditTestSummary,
} from './audit-performance-config.js';

// ============================================================================
// STRESS TEST OPTIONS
// ============================================================================

const testType = __ENV.STRESS_TEST_TYPE || 'ramp';

const stressScenarios = {
    // Spike test - sudden traffic surge
    spike: {
        executor: 'ramping-vus',
        startVUs: 10,
        stages: [
            { duration: '30s', target: 10 },   // Baseline
            { duration: '10s', target: 100 },  // Spike up
            { duration: '1m', target: 100 },   // Hold spike
            { duration: '10s', target: 10 },   // Spike down
            { duration: '30s', target: 10 },   // Recovery
            { duration: '10s', target: 150 },  // Second spike (higher)
            { duration: '1m', target: 150 },   // Hold
            { duration: '30s', target: 0 },    // Ramp down
        ],
        gracefulRampDown: '30s',
    },

    // Ramp to breaking point
    ramp: {
        executor: 'ramping-vus',
        startVUs: 0,
        stages: [
            { duration: '1m', target: 25 },
            { duration: '2m', target: 50 },
            { duration: '2m', target: 75 },
            { duration: '2m', target: 100 },
            { duration: '2m', target: 125 },
            { duration: '2m', target: 150 },
            { duration: '2m', target: 175 },
            { duration: '2m', target: 200 },
            { duration: '1m', target: 0 },
        ],
        gracefulRampDown: '1m',
    },

    // Soak test - extended duration
    soak: {
        executor: 'constant-vus',
        vus: 50,
        duration: '30m',
        gracefulStop: '1m',
    },

    // Concurrent 7-year query stress
    seven_year_stress: {
        executor: 'ramping-vus',
        startVUs: 1,
        stages: [
            { duration: '30s', target: 5 },
            { duration: '2m', target: 10 },
            { duration: '2m', target: 15 },
            { duration: '2m', target: 20 },
            { duration: '1m', target: 0 },
        ],
        gracefulRampDown: '30s',
    },

    // Export stress - multiple concurrent exports
    export_stress: {
        executor: 'per-vu-iterations',
        vus: 10,
        iterations: 3,
        maxDuration: '15m',
    },
};

export const options = {
    scenarios: {
        stress_test: stressScenarios[testType] || stressScenarios.ramp,
    },

    thresholds: {
        // Relaxed thresholds for stress testing
        'http_req_duration': [
            'p(50)<500',
            'p(95)<2000',
            'p(99)<5000',
        ],
        'http_req_failed': [
            'rate<0.20', // Allow up to 20% errors under stress
        ],
        'audit_query_errors': [
            'rate<0.25',
        ],
        'seven_year_query_duration': [
            'p(95)<10000', // 10 seconds under stress
        ],
    },

    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
};

// ============================================================================
// SETUP
// ============================================================================

export function setup() {
    console.log(`Starting HIPAA Audit Stress Test: ${testType}`);
    console.log(`Base URL: ${config.baseUrl}`);

    return {
        startTime: new Date().toISOString(),
        testType: testType,
        headers: getAuditHeaders(),
    };
}

// ============================================================================
// MAIN TEST FUNCTION
// ============================================================================

export default function (data) {
    const headers = data.headers;

    // Select test based on stress type
    switch (data.testType) {
        case 'seven_year_stress':
            stressTestSevenYearQueries(headers);
            break;
        case 'export_stress':
            stressTestExports(headers);
            break;
        case 'spike':
        case 'ramp':
        case 'soak':
        default:
            mixedStressTest(headers);
            break;
    }
}

// ============================================================================
// STRESS TEST FUNCTIONS
// ============================================================================

/**
 * Mixed stress test - combination of all endpoints
 */
function mixedStressTest(headers) {
    const iteration = __ITER;

    // Distribute load across endpoints
    const endpointChoice = iteration % 10;

    if (endpointChoice < 4) {
        // 40% - Audit log queries
        group('Audit Log Query', () => {
            testAuditLogQuery(headers);
        });
    } else if (endpointChoice < 6) {
        // 20% - Compliance metrics
        group('Compliance Metrics', () => {
            testComplianceMetrics(headers);
        });
    } else if (endpointChoice < 8) {
        // 20% - Daily summaries
        group('Daily Summaries', () => {
            testDailySummaries(headers);
        });
    } else if (endpointChoice === 8) {
        // 10% - 7-year queries
        group('7-Year Query', () => {
            testSevenYearQuery(headers);
        });
    } else {
        // 10% - Storage stats
        group('Storage Stats', () => {
            testStorageStats(headers);
        });
    }

    sleep(thinkTime() * 0.5); // Reduced think time for stress
}

/**
 * Stress test 7-year queries specifically
 */
function stressTestSevenYearQueries(headers) {
    group('7-Year Query Stress', () => {
        // Continuous 7-year metric queries
        testSevenYearQuery(headers);
    });

    sleep(thinkTime());

    group('7-Year Daily Summaries', () => {
        const dateRange = getSevenYearRange();
        const url = buildAuditUrl('audit/daily-summaries', dateRange);

        const response = http.get(url, {
            headers: headers,
            tags: { name: 'seven_year_daily_summaries' },
            timeout: '60s',
        });

        validateSevenYearQuery(response);
    });

    sleep(thinkTime());
}

/**
 * Stress test export functionality
 */
function stressTestExports(headers) {
    const exportRanges = [
        { days: 7, name: 'week' },
        { days: 30, name: 'month' },
        { days: 90, name: 'quarter' },
        { days: 365, name: 'year' },
    ];

    for (const range of exportRanges) {
        group(`Export ${range.name}`, () => {
            const dateRange = getDateRange(range.days);
            const url = buildAuditUrl('audit/export/stream', {
                ...dateRange,
                format: Math.random() > 0.5 ? 'csv' : 'json',
            });

            const response = http.get(url, {
                headers: headers,
                tags: { name: `export_${range.name}` },
                timeout: '180s', // 3 minute timeout
            });

            validateExportResponse(response);

            // Track export size
            if (response.status === 200 && response.body) {
                console.log(`Export ${range.name}: ${response.body.length} bytes`);
            }
        });

        sleep(thinkTime() * 2); // Longer pause between exports
    }
}

// ============================================================================
// INDIVIDUAL TEST FUNCTIONS
// ============================================================================

function testAuditLogQuery(headers) {
    const query = generateAuditLogQuery();
    // Increase page size for stress
    query.pageSize = 100;

    const url = buildAuditUrl('audit', query);

    const response = http.get(url, {
        headers: headers,
        tags: { name: 'audit_log_query_stress' },
        timeout: '30s',
    });

    validateAuditResponse(response, 'audit_log_query', 2000); // Relaxed threshold
    metrics.auditLogQueryDuration.add(response.timings.duration);
}

function testComplianceMetrics(headers) {
    const query = generateComplianceMetricsQuery();
    const url = buildAuditUrl('audit/compliance/metrics', query);

    const response = http.get(url, {
        headers: headers,
        tags: { name: 'compliance_metrics_stress' },
        timeout: '15s',
    });

    validateAuditResponse(response, 'compliance_metrics', 500);
    metrics.complianceMetricsDuration.add(response.timings.duration);
}

function testDailySummaries(headers) {
    const dateRange = getDateRange(90); // 90 days for stress
    const url = buildAuditUrl('audit/daily-summaries', dateRange);

    const response = http.get(url, {
        headers: headers,
        tags: { name: 'daily_summaries_stress' },
        timeout: '15s',
    });

    validateAuditResponse(response, 'daily_summaries', 500);
    metrics.dailySummaryDuration.add(response.timings.duration);
}

function testSevenYearQuery(headers) {
    const dateRange = getSevenYearRange();
    const url = buildAuditUrl('audit/compliance/metrics', dateRange);

    const response = http.get(url, {
        headers: headers,
        tags: { name: 'seven_year_query_stress' },
        timeout: '60s',
    });

    validateSevenYearQuery(response);

    // Track if we're hitting degraded performance
    if (response.timings.duration > config.thresholds.sevenYearQuery) {
        console.warn(`7-year query exceeded threshold: ${response.timings.duration}ms`);
    }
}

function testStorageStats(headers) {
    const url = buildAuditUrl('audit/storage/stats');

    const response = http.get(url, {
        headers: headers,
        tags: { name: 'storage_stats_stress' },
        timeout: '15s',
    });

    validateAuditResponse(response, 'storage_stats', 1000);
    metrics.storageStatsDuration.add(response.timings.duration);
}

// ============================================================================
// TEARDOWN
// ============================================================================

export function teardown(data) {
    console.log(`Stress test completed: ${data.testType}`);
    console.log(`Started at: ${data.startTime}`);
    console.log(`Ended at: ${new Date().toISOString()}`);
}

// ============================================================================
// CUSTOM SUMMARY
// ============================================================================

export function handleSummary(data) {
    const summary = `
========================================
EMR HIPAA Audit STRESS Test Summary
========================================
Test Type: ${testType}

Performance Under Stress:
- HTTP Req Duration (p50): ${data.metrics.http_req_duration?.['p(50)']?.toFixed(2)}ms
- HTTP Req Duration (p95): ${data.metrics.http_req_duration?.['p(95)']?.toFixed(2)}ms
- HTTP Req Duration (p99): ${data.metrics.http_req_duration?.['p(99)']?.toFixed(2)}ms
- HTTP Req Duration (max): ${data.metrics.http_req_duration?.max?.toFixed(2)}ms

Error Rates:
- HTTP Request Failures: ${(data.metrics.http_req_failed?.rate * 100)?.toFixed(2)}%
- Audit Query Errors: ${(data.metrics.audit_query_errors?.rate * 100)?.toFixed(2)}%

7-Year Query Performance:
- p95: ${data.metrics.seven_year_query_duration?.['p(95)']?.toFixed(2)}ms
- max: ${data.metrics.seven_year_query_duration?.max?.toFixed(2)}ms

Aggregate Query Performance:
- Compliance Metrics (p95): ${data.metrics.compliance_metrics_duration?.['p(95)']?.toFixed(2)}ms
- Daily Summaries (p95): ${data.metrics.daily_summary_duration?.['p(95)']?.toFixed(2)}ms

Total Requests: ${data.metrics.http_reqs?.count || 0}
Successful Queries: ${data.metrics.successful_audit_queries?.count || 0}
========================================
`;

    console.log(summary);

    return {
        'stdout': summary,
        'audit-stress-test-results.json': JSON.stringify(data, null, 2),
    };
}
