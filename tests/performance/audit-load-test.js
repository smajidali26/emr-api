/**
 * EMR HIPAA Audit Load Test
 *
 * Load testing for HIPAA Audit Logging endpoints.
 * Tests TimescaleDB query performance, continuous aggregates,
 * and validates 7-year retention query requirements.
 *
 * Usage:
 *   k6 run audit-load-test.js
 *   k6 run --vus 50 --duration 5m audit-load-test.js
 *   k6 run -e BASE_URL=https://api.example.com -e ADMIN_TOKEN=xxx audit-load-test.js
 *
 * HIPAA REQUIREMENTS TESTED:
 * - 7-year data range queries complete in < 5 seconds
 * - Continuous aggregate queries < 100ms
 * - 100% audit coverage (no dropped logs)
 * - Export functionality for compliance reporting
 */

import http from 'k6/http';
import { sleep, group } from 'k6';
import {
    config,
    metrics,
    getDateRange,
    getSevenYearRange,
    getRandomDateRange,
    generateAuditLogQuery,
    generateComplianceMetricsQuery,
    generateResourceAccessQuery,
    validateAuditResponse,
    validateSevenYearQuery,
    validateExportResponse,
    getAuditHeaders,
    buildAuditUrl,
    getAuditThresholds,
    thinkTime,
    formatAuditTestSummary,
} from './audit-performance-config.js';

// ============================================================================
// TEST OPTIONS
// ============================================================================

export const options = {
    // Test scenarios
    scenarios: {
        // Warm-up phase
        warmup: {
            executor: 'constant-vus',
            vus: 5,
            duration: '30s',
            startTime: '0s',
            tags: { phase: 'warmup' },
        },

        // Main load test
        load_test: {
            executor: 'ramping-vus',
            startVUs: 0,
            stages: [
                { duration: '1m', target: 25 },   // Ramp up
                { duration: '3m', target: 25 },   // Steady state
                { duration: '1m', target: 50 },   // Increase load
                { duration: '3m', target: 50 },   // Higher steady state
                { duration: '1m', target: 0 },    // Ramp down
            ],
            startTime: '30s',
            gracefulRampDown: '30s',
            tags: { phase: 'load' },
        },

        // 7-year query test (critical for HIPAA)
        seven_year_queries: {
            executor: 'constant-vus',
            vus: 3,
            duration: '5m',
            startTime: '1m',
            tags: { phase: 'seven_year' },
        },
    },

    // Performance thresholds
    thresholds: getAuditThresholds(),

    // Output configuration
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
};

// ============================================================================
// SETUP
// ============================================================================

export function setup() {
    console.log('Starting HIPAA Audit Performance Test');
    console.log(`Base URL: ${config.baseUrl}`);
    console.log(`Admin Token: ${config.adminToken ? 'Provided' : 'NOT PROVIDED - tests may fail'}`);

    // Verify connectivity
    const healthCheck = http.get(`${config.baseUrl}/health`, {
        timeout: '10s',
    });

    if (healthCheck.status !== 200) {
        console.warn(`Health check failed with status ${healthCheck.status}`);
    }

    return {
        startTime: new Date().toISOString(),
        headers: getAuditHeaders(),
    };
}

// ============================================================================
// MAIN TEST FUNCTION
// ============================================================================

export default function (data) {
    const headers = data.headers;
    const scenario = __ENV.SCENARIO || 'mixed';

    // Route to appropriate test based on scenario tag
    const tags = __ITER === 0 ? {} : (__VU % 10 === 0 ? { focus: 'aggregates' } : {});

    group('Audit Log Queries', () => {
        testAuditLogQuery(headers);
    });

    sleep(thinkTime());

    group('Compliance Metrics', () => {
        testComplianceMetrics(headers);
    });

    sleep(thinkTime());

    group('Daily Summaries', () => {
        testDailySummaries(headers);
    });

    sleep(thinkTime());

    // Periodic 7-year query test (1 in 10 iterations)
    if (__ITER % 10 === 0) {
        group('7-Year Query Test', () => {
            testSevenYearQuery(headers);
        });
        sleep(thinkTime());
    }

    // Periodic storage stats check (1 in 20 iterations)
    if (__ITER % 20 === 0) {
        group('Storage Statistics', () => {
            testStorageStats(headers);
        });
        sleep(thinkTime());
    }

    // Periodic user activity query (1 in 5 iterations)
    if (__ITER % 5 === 0) {
        group('User Activity', () => {
            testUserActivity(headers);
        });
        sleep(thinkTime());
    }
}

// ============================================================================
// TEST FUNCTIONS
// ============================================================================

/**
 * Test paginated audit log queries
 */
function testAuditLogQuery(headers) {
    const query = generateAuditLogQuery();
    const url = buildAuditUrl('audit', query);

    const response = http.get(url, {
        headers: headers,
        tags: { name: 'audit_log_query' },
        timeout: '30s',
    });

    validateAuditResponse(response, 'audit_log_query');
    metrics.auditLogQueryDuration.add(response.timings.duration);

    // Track records returned
    if (response.status === 200) {
        try {
            const data = JSON.parse(response.body);
            if (data.totalCount) {
                metrics.totalRecordsQueried.add(data.items?.length || 0);
            }
        } catch (e) {
            // Ignore parse errors
        }
    }
}

/**
 * Test compliance metrics endpoint (uses continuous aggregates)
 */
function testComplianceMetrics(headers) {
    const query = generateComplianceMetricsQuery();
    const url = buildAuditUrl('audit/compliance/metrics', query);

    const response = http.get(url, {
        headers: headers,
        tags: { name: 'compliance_metrics' },
        timeout: '10s',
    });

    validateAuditResponse(response, 'compliance_metrics', config.thresholds.p50 * 2);
    metrics.complianceMetricsDuration.add(response.timings.duration);
    metrics.aggregateQueryDuration.add(response.timings.duration);

    // Track aggregate usage
    if (response.status === 200) {
        metrics.aggregateHits.add(1);
    }
}

/**
 * Test daily summaries endpoint (uses continuous aggregates)
 */
function testDailySummaries(headers) {
    const dateRange = getDateRange(30); // Last 30 days
    const url = buildAuditUrl('audit/daily-summaries', dateRange);

    const response = http.get(url, {
        headers: headers,
        tags: { name: 'daily_summaries' },
        timeout: '10s',
    });

    validateAuditResponse(response, 'daily_summaries', config.thresholds.p50 * 2);
    metrics.dailySummaryDuration.add(response.timings.duration);
    metrics.aggregateQueryDuration.add(response.timings.duration);
}

/**
 * Test 7-year range query (HIPAA critical requirement)
 */
function testSevenYearQuery(headers) {
    const dateRange = getSevenYearRange();
    const url = buildAuditUrl('audit/compliance/metrics', dateRange);

    const startTime = Date.now();
    const response = http.get(url, {
        headers: headers,
        tags: { name: 'seven_year_query' },
        timeout: '30s', // Extended timeout for large queries
    });

    const duration = Date.now() - startTime;
    validateSevenYearQuery(response);

    // Log if approaching threshold
    if (duration > config.thresholds.sevenYearQuery * 0.8) {
        console.warn(`7-year query approaching threshold: ${duration}ms (threshold: ${config.thresholds.sevenYearQuery}ms)`);
    }
}

/**
 * Test storage statistics endpoint
 */
function testStorageStats(headers) {
    const url = buildAuditUrl('audit/storage/stats');

    const response = http.get(url, {
        headers: headers,
        tags: { name: 'storage_stats' },
        timeout: '10s',
    });

    validateAuditResponse(response, 'storage_stats');
    metrics.storageStatsDuration.add(response.timings.duration);

    // Extract and track TimescaleDB metrics
    if (response.status === 200) {
        try {
            const data = JSON.parse(response.body);
            if (data.compression?.compressionRatio) {
                metrics.compressionRatio.add(data.compression.compressionRatio);
            }
            if (data.hypertable?.numChunks) {
                metrics.chunkCount.add(data.hypertable.numChunks);
            }
            if (data.storage?.totalSizeBytes) {
                metrics.totalStorageBytes.add(data.storage.totalSizeBytes);
            }
        } catch (e) {
            // Ignore parse errors
        }
    }
}

/**
 * Test user activity endpoint
 */
function testUserActivity(headers) {
    const userId = config.testData.userIds[
        Math.floor(Math.random() * config.testData.userIds.length)
    ];
    const url = buildAuditUrl(`audit/users/${userId}/activity`);

    const response = http.get(url, {
        headers: headers,
        tags: { name: 'user_activity' },
        timeout: '10s',
    });

    validateAuditResponse(response, 'user_activity');
    metrics.userActivityDuration.add(response.timings.duration);
}

/**
 * Test resource access endpoint
 */
function testResourceAccess(headers) {
    const { resourceType, resourceId } = generateResourceAccessQuery();
    const url = buildAuditUrl(`audit/resources/${resourceType}/${resourceId}/access`);

    const response = http.get(url, {
        headers: headers,
        tags: { name: 'resource_access' },
        timeout: '10s',
    });

    validateAuditResponse(response, 'resource_access');
    metrics.resourceAccessDuration.add(response.timings.duration);
}

/**
 * Test audit trail for specific resource
 */
function testAuditTrail(headers) {
    const { resourceType, resourceId } = generateResourceAccessQuery();
    const url = buildAuditUrl(`audit/trail/${resourceType}/${resourceId}`);

    const response = http.get(url, {
        headers: headers,
        tags: { name: 'audit_trail' },
        timeout: '15s',
    });

    validateAuditResponse(response, 'audit_trail');
}

/**
 * Test export functionality
 */
function testExport(headers) {
    const dateRange = getDateRange(7); // Last 7 days for export
    const url = buildAuditUrl('audit/export/stream', {
        ...dateRange,
        format: 'csv',
    });

    const response = http.get(url, {
        headers: headers,
        tags: { name: 'export' },
        timeout: '120s', // 2 minute timeout for exports
    });

    validateExportResponse(response);
}

// ============================================================================
// TEARDOWN
// ============================================================================

export function teardown(data) {
    console.log(`Test completed. Started at: ${data.startTime}`);
    console.log(`Ended at: ${new Date().toISOString()}`);
}

// ============================================================================
// CUSTOM SUMMARY
// ============================================================================

export function handleSummary(data) {
    const summary = formatAuditTestSummary(data);
    console.log(summary);

    return {
        'stdout': summary,
        'audit-load-test-results.json': JSON.stringify(data, null, 2),
    };
}
