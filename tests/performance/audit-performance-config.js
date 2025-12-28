/**
 * EMR Audit Performance Test Configuration
 *
 * Shared configuration, helper functions, and metrics for HIPAA Audit
 * Logging performance tests. Validates TimescaleDB query performance
 * and 7-year data retention query requirements.
 *
 * HIPAA COMPLIANCE REQUIREMENTS:
 * - 7-year audit log retention (2,555 days)
 * - Query performance: 7-year range < 5 seconds
 * - 100% audit coverage (no dropped logs under load)
 * - Continuous aggregate query performance < 100ms
 */

import { check } from 'k6';
import { Rate, Counter, Trend, Gauge } from 'k6/metrics';

// ============================================================================
// ENVIRONMENT CONFIGURATION
// ============================================================================

export const config = {
    // API endpoint configuration
    baseUrl: __ENV.BASE_URL || 'https://localhost:5001',

    // Admin authentication (required for audit endpoints)
    adminToken: __ENV.ADMIN_TOKEN || '',

    // Test data configuration
    testData: {
        // Date ranges for testing
        dateRanges: {
            day: { days: 1 },
            week: { days: 7 },
            month: { days: 30 },
            quarter: { days: 90 },
            year: { days: 365 },
            sevenYears: { days: 2555 }, // HIPAA requirement
        },
        // Sample user IDs for testing
        userIds: [
            'user-admin-1',
            'user-doctor-1',
            'user-nurse-1',
            'user-patient-1',
        ],
        // Sample resource types
        resourceTypes: ['Patient', 'Appointment', 'Prescription', 'LabResult'],
        // Event types for filtering
        eventTypes: ['Login', 'Logout', 'View', 'Create', 'Update', 'Delete', 'Export', 'AccessDenied'],
    },

    // Performance thresholds - HIPAA Audit specific
    thresholds: {
        // Response time requirements
        p50: 100,    // 50th percentile < 100ms (uses continuous aggregates)
        p95: 500,    // 95th percentile < 500ms
        p99: 1000,   // 99th percentile < 1000ms
        sevenYearQuery: 5000, // 7-year range query < 5 seconds (critical)

        // Error rate requirements
        errorRate: 0.01, // < 1% error rate

        // Audit-specific requirements
        exportTimeout: 120000, // 2 minutes for large exports
        aggregateRefresh: 30000, // 30 seconds for aggregate refresh
    },

    // Load test parameters
    load: {
        vus: {
            min: 5,
            target: parseInt(__ENV.TARGET_VUS || '50'),
            max: 100,
            stress: 200,
        },
        duration: {
            rampUp: '1m',
            steadyState: '5m',
            rampDown: '30s',
            soak: '15m',
        },
        think_time: {
            min: 0.5,
            max: 2,
        }
    }
};

// ============================================================================
// CUSTOM METRICS - AUDIT SPECIFIC
// ============================================================================

export const metrics = {
    // Query performance tracking
    auditLogQueryDuration: new Trend('audit_log_query_duration'),
    complianceMetricsDuration: new Trend('compliance_metrics_duration'),
    dailySummaryDuration: new Trend('daily_summary_duration'),
    userActivityDuration: new Trend('user_activity_duration'),
    resourceAccessDuration: new Trend('resource_access_duration'),
    storageStatsDuration: new Trend('storage_stats_duration'),
    exportDuration: new Trend('export_duration'),
    sevenYearQueryDuration: new Trend('seven_year_query_duration'),

    // Error tracking
    auditQueryErrors: new Rate('audit_query_errors'),
    exportErrors: new Rate('export_errors'),
    timeoutErrors: new Rate('timeout_errors'),
    authErrors: new Rate('auth_errors'),

    // Success tracking
    successfulQueries: new Counter('successful_audit_queries'),
    successfulExports: new Counter('successful_exports'),
    totalRecordsQueried: new Counter('total_records_queried'),

    // TimescaleDB specific
    compressionRatio: new Gauge('compression_ratio'),
    chunkCount: new Gauge('chunk_count'),
    totalStorageBytes: new Gauge('total_storage_bytes'),

    // Aggregate performance
    aggregateQueryDuration: new Trend('aggregate_query_duration'),
    aggregateHits: new Counter('aggregate_hits'),
    aggregateMisses: new Counter('aggregate_misses'),
};

// ============================================================================
// DATE HELPERS
// ============================================================================

/**
 * Get date range for testing
 * @param {number} daysBack - Number of days to go back
 * @returns {Object} Object with fromDate and toDate in ISO format
 */
export function getDateRange(daysBack) {
    const toDate = new Date();
    const fromDate = new Date();
    fromDate.setDate(fromDate.getDate() - daysBack);

    return {
        fromDate: fromDate.toISOString().split('T')[0],
        toDate: toDate.toISOString().split('T')[0],
    };
}

/**
 * Get 7-year date range for HIPAA compliance testing
 * @returns {Object} Object with fromDate and toDate
 */
export function getSevenYearRange() {
    return getDateRange(config.testData.dateRanges.sevenYears.days);
}

/**
 * Get random date range for varied testing
 * @returns {Object} Object with fromDate, toDate, and days
 */
export function getRandomDateRange() {
    const ranges = Object.values(config.testData.dateRanges);
    const range = ranges[Math.floor(Math.random() * ranges.length)];
    return {
        ...getDateRange(range.days),
        days: range.days,
    };
}

// ============================================================================
// TEST DATA GENERATORS
// ============================================================================

/**
 * Generate random audit log query parameters
 * @returns {Object} Query parameters for audit log endpoint
 */
export function generateAuditLogQuery() {
    const dateRange = getRandomDateRange();
    const pageSize = [10, 25, 50, 100][Math.floor(Math.random() * 4)];
    const pageNumber = Math.floor(Math.random() * 10) + 1;

    const query = {
        fromDate: dateRange.fromDate,
        toDate: dateRange.toDate,
        pageNumber: pageNumber,
        pageSize: pageSize,
    };

    // Randomly add filters
    if (Math.random() > 0.5) {
        query.eventType = config.testData.eventTypes[
            Math.floor(Math.random() * config.testData.eventTypes.length)
        ];
    }

    if (Math.random() > 0.7) {
        query.userId = config.testData.userIds[
            Math.floor(Math.random() * config.testData.userIds.length)
        ];
    }

    return query;
}

/**
 * Generate compliance metrics query parameters
 * @returns {Object} Query parameters
 */
export function generateComplianceMetricsQuery() {
    const dateRange = getRandomDateRange();
    return {
        fromDate: dateRange.fromDate,
        toDate: dateRange.toDate,
    };
}

/**
 * Generate resource access query
 * @returns {Object} Resource type and ID
 */
export function generateResourceAccessQuery() {
    const resourceType = config.testData.resourceTypes[
        Math.floor(Math.random() * config.testData.resourceTypes.length)
    ];
    const resourceId = `${resourceType.toLowerCase()}-${Math.floor(Math.random() * 1000)}`;

    return { resourceType, resourceId };
}

// ============================================================================
// VALIDATION HELPERS
// ============================================================================

/**
 * Check if response is successful
 * @param {Response} response - HTTP response
 * @returns {boolean}
 */
export function isSuccessful(response) {
    return response.status >= 200 && response.status < 300;
}

/**
 * Validate audit query response
 * @param {Response} response - HTTP response
 * @param {string} endpoint - Endpoint name for logging
 * @param {number} maxDuration - Maximum acceptable duration in ms
 * @returns {boolean}
 */
export function validateAuditResponse(response, endpoint, maxDuration = config.thresholds.p95) {
    const checks = {
        [`${endpoint}: status is 2xx`]: isSuccessful(response),
        [`${endpoint}: response time < ${maxDuration}ms`]: response.timings.duration < maxDuration,
        [`${endpoint}: has response body`]: response.body && response.body.length > 0,
    };

    const result = check(response, checks);

    // Track metrics
    if (isSuccessful(response)) {
        metrics.successfulQueries.add(1);
        metrics.auditQueryErrors.add(0);
    } else {
        metrics.auditQueryErrors.add(1);
    }

    return result;
}

/**
 * Validate 7-year query response (stricter requirements)
 * @param {Response} response - HTTP response
 * @returns {boolean}
 */
export function validateSevenYearQuery(response) {
    const checks = {
        '7-year query: status is 2xx': isSuccessful(response),
        '7-year query: response time < 5000ms': response.timings.duration < config.thresholds.sevenYearQuery,
        '7-year query: has data': response.body && response.body.length > 0,
    };

    const result = check(response, checks);
    metrics.sevenYearQueryDuration.add(response.timings.duration);

    return result;
}

/**
 * Validate export response
 * @param {Response} response - HTTP response
 * @returns {boolean}
 */
export function validateExportResponse(response) {
    const checks = {
        'export: status is 2xx': isSuccessful(response),
        'export: response time < 120s': response.timings.duration < config.thresholds.exportTimeout,
        'export: has content': response.body && response.body.length > 0,
    };

    const result = check(response, checks);

    if (isSuccessful(response)) {
        metrics.successfulExports.add(1);
        metrics.exportDuration.add(response.timings.duration);
    } else {
        metrics.exportErrors.add(1);
    }

    return result;
}

// ============================================================================
// REQUEST HELPERS
// ============================================================================

/**
 * Get headers for audit API requests
 * @param {string} authToken - JWT token (optional, uses config default)
 * @returns {Object} Headers object
 */
export function getAuditHeaders(authToken = null) {
    const token = authToken || config.adminToken;

    if (!token) {
        console.warn('No admin token provided. Audit endpoints require Admin role.');
    }

    return {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        'Authorization': `Bearer ${token}`,
    };
}

/**
 * Build audit API URL with query parameters
 * @param {string} endpoint - API endpoint path
 * @param {Object} params - Query parameters
 * @returns {string} Full URL with query string
 */
export function buildAuditUrl(endpoint, params = {}) {
    const path = endpoint.startsWith('/') ? endpoint.substring(1) : endpoint;
    const url = new URL(`${config.baseUrl}/api/${path}`);

    Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
            url.searchParams.append(key, value);
        }
    });

    return url.toString();
}

// ============================================================================
// THRESHOLD CONFIGURATION
// ============================================================================

/**
 * Get k6 thresholds for audit performance tests
 * @returns {Object} Thresholds configuration
 */
export function getAuditThresholds() {
    return {
        // HTTP request duration thresholds
        'http_req_duration': [
            `p(50)<${config.thresholds.p50}`,
            `p(95)<${config.thresholds.p95}`,
            `p(99)<${config.thresholds.p99}`,
        ],

        // Error rate thresholds
        'http_req_failed': [
            `rate<${config.thresholds.errorRate}`,
        ],

        // Audit-specific thresholds
        'audit_query_errors': [
            `rate<${config.thresholds.errorRate}`,
        ],
        'seven_year_query_duration': [
            `p(95)<${config.thresholds.sevenYearQuery}`,
        ],
        'compliance_metrics_duration': [
            `p(95)<${config.thresholds.p50 * 2}`, // Aggregates should be fast
        ],
        'daily_summary_duration': [
            `p(95)<${config.thresholds.p50 * 2}`,
        ],
        'export_errors': [
            'rate<0.05', // < 5% export errors acceptable
        ],
    };
}

// ============================================================================
// SCENARIO CONFIGURATIONS
// ============================================================================

export const scenarios = {
    /**
     * Compliance officer reviewing daily metrics
     */
    complianceReview: {
        executor: 'ramping-vus',
        startVUs: 0,
        stages: [
            { duration: '30s', target: 10 },
            { duration: '3m', target: 10 },
            { duration: '30s', target: 0 },
        ],
        gracefulRampDown: '30s',
        tags: { scenario: 'compliance_review' },
    },

    /**
     * Audit log search - typical admin usage
     */
    auditLogSearch: {
        executor: 'ramping-vus',
        startVUs: 0,
        stages: [
            { duration: '30s', target: 20 },
            { duration: '3m', target: 20 },
            { duration: '30s', target: 0 },
        ],
        gracefulRampDown: '30s',
        tags: { scenario: 'audit_log_search' },
    },

    /**
     * 7-year query stress test (HIPAA requirement)
     */
    sevenYearQueryTest: {
        executor: 'constant-vus',
        vus: 5,
        duration: '2m',
        tags: { scenario: 'seven_year_query' },
    },

    /**
     * Export stress test - large data extraction
     */
    exportStress: {
        executor: 'per-vu-iterations',
        vus: 3,
        iterations: 5,
        maxDuration: '10m',
        tags: { scenario: 'export_stress' },
    },

    /**
     * Mixed workload - realistic usage pattern
     */
    mixedWorkload: {
        executor: 'ramping-vus',
        startVUs: 0,
        stages: [
            { duration: '1m', target: 30 },
            { duration: '5m', target: 30 },
            { duration: '1m', target: 0 },
        ],
        gracefulRampDown: '30s',
        tags: { scenario: 'mixed_workload' },
    },
};

// ============================================================================
// THINK TIME HELPERS
// ============================================================================

/**
 * Simulate realistic user think time
 * @returns {number} Think time in seconds
 */
export function thinkTime() {
    const min = config.load.think_time.min;
    const max = config.load.think_time.max;
    return Math.random() * (max - min) + min;
}

// ============================================================================
// REPORTING HELPERS
// ============================================================================

/**
 * Format audit test summary
 * @param {Object} data - Test execution data
 * @returns {string} Formatted summary
 */
export function formatAuditTestSummary(data) {
    return `
========================================
EMR HIPAA Audit Performance Test Summary
========================================

Test Configuration:
- Base URL: ${config.baseUrl}
- Target VUs: ${config.load.vus.target}
- Duration: ${config.load.duration.steadyState}

Performance Metrics:
- HTTP Req Duration (p95): ${data.metrics.http_req_duration?.['p(95)']?.toFixed(2)}ms
- HTTP Req Duration (p99): ${data.metrics.http_req_duration?.['p(99)']?.toFixed(2)}ms
- Error Rate: ${(data.metrics.http_req_failed?.rate * 100)?.toFixed(2)}%

HIPAA Audit Metrics:
- 7-Year Query (p95): ${data.metrics.seven_year_query_duration?.['p(95)']?.toFixed(2)}ms
- Compliance Metrics (p95): ${data.metrics.compliance_metrics_duration?.['p(95)']?.toFixed(2)}ms
- Successful Queries: ${data.metrics.successful_audit_queries?.count || 0}
- Successful Exports: ${data.metrics.successful_exports?.count || 0}

Thresholds:
- 7-Year Query Target: < ${config.thresholds.sevenYearQuery}ms
- Standard Query (p95): < ${config.thresholds.p95}ms
- Aggregate Query (p95): < ${config.thresholds.p50 * 2}ms

========================================
`;
}

// ============================================================================
// EXPORTS
// ============================================================================

export default {
    config,
    metrics,
    getDateRange,
    getSevenYearRange,
    getRandomDateRange,
    generateAuditLogQuery,
    generateComplianceMetricsQuery,
    generateResourceAccessQuery,
    isSuccessful,
    validateAuditResponse,
    validateSevenYearQuery,
    validateExportResponse,
    getAuditHeaders,
    buildAuditUrl,
    getAuditThresholds,
    scenarios,
    thinkTime,
    formatAuditTestSummary,
};
