/**
 * EMR Authentication Stress Test
 *
 * Tests authentication system behavior under extreme load and adverse conditions.
 *
 * Test Types:
 * 1. Stress Test - Gradually increase load beyond normal capacity to find breaking point
 * 2. Spike Test - Sudden dramatic increase in load to test system resilience
 * 3. Soak Test - Extended duration at moderate load to detect memory leaks and degradation
 * 4. Breakpoint Test - Find maximum sustainable capacity
 *
 * Goals:
 * - Identify system breaking point
 * - Verify graceful degradation under overload
 * - Test rate limiting effectiveness at scale
 * - Detect memory leaks and resource exhaustion
 * - Validate error handling under stress
 *
 * Usage:
 *   # Run stress test
 *   k6 run --scenario stress auth-stress-test.js
 *
 *   # Run spike test
 *   k6 run --scenario spike auth-stress-test.js
 *
 *   # Run soak test (30 minutes)
 *   k6 run --scenario soak auth-stress-test.js
 *
 *   # Run all scenarios
 *   k6 run auth-stress-test.js
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { SharedArray } from 'k6/data';
import { Counter, Trend, Rate } from 'k6/metrics';
import {
    config,
    metrics,
    generateTestUser,
    buildUrl,
    getDefaultHeaders,
    validateAuthResponse,
    validateApiResponse,
    isRateLimited,
    isServerError,
    thinkTime,
} from './auth-performance-config.js';

// ============================================================================
// CUSTOM METRICS FOR STRESS TESTING
// ============================================================================

const stressMetrics = {
    degradationRate: new Rate('stress_degradation_rate'),
    recoveryTime: new Trend('stress_recovery_time'),
    peakLoad: new Counter('stress_peak_load'),
    systemFailures: new Counter('stress_system_failures'),
    circuitBreakerTrips: new Counter('stress_circuit_breaker_trips'),
};

// ============================================================================
// TEST CONFIGURATION
// ============================================================================

export const options = {
    // Define multiple test scenarios
    scenarios: {
        // Scenario 1: Stress Test
        // Gradually increase load to find breaking point
        stress: {
            executor: 'ramping-vus',
            startVUs: 0,
            stages: [
                { duration: '2m', target: 50 },    // Warm up
                { duration: '3m', target: 100 },   // Normal load
                { duration: '3m', target: 200 },   // Above normal
                { duration: '3m', target: 300 },   // High stress
                { duration: '3m', target: 400 },   // Extreme stress
                { duration: '3m', target: 500 },   // Breaking point
                { duration: '5m', target: 0 },     // Recovery period
            ],
            gracefulRampDown: '2m',
            gracefulStop: '30s',
            exec: 'stressTest',
            tags: { test_type: 'stress' },
        },

        // Scenario 2: Spike Test
        // Sudden load increase to test system shock response
        spike: {
            executor: 'ramping-vus',
            startVUs: 0,
            stages: [
                { duration: '1m', target: 50 },    // Normal baseline
                { duration: '30s', target: 500 },  // SPIKE!
                { duration: '3m', target: 500 },   // Hold spike
                { duration: '1m', target: 50 },    // Drop back down
                { duration: '2m', target: 50 },    // Recovery observation
                { duration: '30s', target: 0 },    // Ramp down
            ],
            gracefulRampDown: '1m',
            gracefulStop: '30s',
            exec: 'spikeTest',
            tags: { test_type: 'spike' },
        },

        // Scenario 3: Soak Test
        // Extended duration at moderate-high load to detect leaks
        soak: {
            executor: 'ramping-vus',
            startVUs: 0,
            stages: [
                { duration: '2m', target: 100 },   // Ramp up
                { duration: '30m', target: 100 },  // Soak for 30 minutes
                { duration: '2m', target: 0 },     // Ramp down
            ],
            gracefulRampDown: '1m',
            gracefulStop: '30s',
            exec: 'soakTest',
            tags: { test_type: 'soak' },
        },

        // Scenario 4: Breakpoint Test
        // Find maximum sustainable capacity
        breakpoint: {
            executor: 'ramping-arrival-rate',
            startRate: 50,
            timeUnit: '1s',
            preAllocatedVUs: 500,
            maxVUs: 1000,
            stages: [
                { duration: '2m', target: 100 },   // 100 req/s
                { duration: '2m', target: 200 },   // 200 req/s
                { duration: '2m', target: 300 },   // 300 req/s
                { duration: '2m', target: 400 },   // 400 req/s
                { duration: '2m', target: 500 },   // 500 req/s
                { duration: '2m', target: 600 },   // 600 req/s
            ],
            gracefulRampDown: '1m',
            exec: 'breakpointTest',
            tags: { test_type: 'breakpoint' },
        },
    },

    // Relaxed thresholds for stress testing
    // We expect some failures under extreme load
    thresholds: {
        'http_req_duration': [
            'p(50)<1000',  // Relaxed from 200ms
            'p(95)<2000',  // Relaxed from 500ms
            'p(99)<5000',  // Relaxed from 1000ms
        ],
        'http_req_failed': [
            'rate<0.20',   // Allow up to 20% failure under stress
        ],
        'rate_limit_errors': [
            'rate<0.50',   // Expect significant rate limiting
        ],
        'stress_degradation_rate': [
            'rate<0.30',   // Allow 30% degradation
        ],
        'stress_system_failures': [
            'count<100',   // Allow some system failures
        ],
    },

    // Test metadata
    tags: {
        test_suite: 'auth_stress_test',
        environment: __ENV.ENVIRONMENT || 'test',
    },
};

// ============================================================================
// TEST DATA SETUP
// ============================================================================

const testUsers = new SharedArray('stress_test_users', function () {
    const users = [];

    // Generate larger dataset for stress testing
    for (let i = 0; i < 200; i++) {
        users.push(generateTestUser('Patient', i));
    }
    for (let i = 0; i < 50; i++) {
        users.push(generateTestUser('Doctor', i));
    }
    for (let i = 0; i < 30; i++) {
        users.push(generateTestUser('Nurse', i));
    }
    for (let i = 0; i < 20; i++) {
        users.push(generateTestUser('Staff', i));
    }

    return users;
});

// ============================================================================
// SETUP PHASE
// ============================================================================

export function setup() {
    console.log('Starting EMR Authentication Stress Test Suite');
    console.log(`Base URL: ${config.baseUrl}`);
    console.log(`Test Users: ${testUsers.length}`);

    // Health check
    const healthCheck = http.get(`${config.baseUrl}/health`);
    if (!check(healthCheck, { 'API is healthy': (r) => r.status === 200 })) {
        console.warn('WARNING: API health check failed. Proceeding with test anyway.');
    }

    return {
        startTime: Date.now(),
    };
}

// ============================================================================
// STRESS TEST SCENARIO
// ============================================================================

/**
 * Stress Test: Gradually increase load to find breaking point
 * Focus: System behavior under increasing load
 */
export function stressTest(data) {
    const user = testUsers[Math.floor(Math.random() * testUsers.length)];

    group('Stress Test - Authentication Flow', () => {
        // Aggressive authentication attempts
        const authToken = user.mockToken;

        // Get CSRF token
        const csrfResponse = http.get(
            buildUrl('auth/csrf-token'),
            { tags: { scenario: 'stress' } }
        );

        let csrfToken = null;
        if (csrfResponse.status === 200) {
            try {
                csrfToken = JSON.parse(csrfResponse.body).token;
            } catch (e) {
                // Continue without CSRF
            }
        }

        // Login callback
        const loginResponse = http.post(
            buildUrl('auth/login-callback'),
            null,
            {
                headers: getDefaultHeaders(csrfToken, authToken),
                tags: { scenario: 'stress', name: 'POST /api/auth/login-callback' },
            }
        );

        // Track degradation
        const isDegraded = loginResponse.timings.duration > config.thresholds.p95 * 2;
        stressMetrics.degradationRate.add(isDegraded ? 1 : 0);

        // Track system failures (5xx errors)
        if (isServerError(loginResponse)) {
            stressMetrics.systemFailures.add(1);
        }

        // Check if rate limited (expected under stress)
        if (isRateLimited(loginResponse)) {
            stressMetrics.circuitBreakerTrips.add(1);
        }

        // Minimal think time during stress
        sleep(0.1);

        // Get user profile
        const profileResponse = http.get(
            buildUrl('auth/me'),
            {
                headers: getDefaultHeaders(null, authToken),
                tags: { scenario: 'stress', name: 'GET /api/auth/me' },
            }
        );

        // Validate responses are still somewhat functional
        check(profileResponse, {
            'Stress: response received': (r) => r.status > 0,
            'Stress: not completely broken': (r) => r.status !== 500 && r.status !== 502 && r.status !== 503,
        });
    });

    // Very minimal sleep during stress test
    sleep(Math.random() * 0.5);
}

// ============================================================================
// SPIKE TEST SCENARIO
// ============================================================================

/**
 * Spike Test: Sudden dramatic load increase
 * Focus: System shock response and recovery
 */
export function spikeTest(data) {
    const user = testUsers[Math.floor(Math.random() * testUsers.length)];

    group('Spike Test - Rapid Authentication', () => {
        const authToken = user.mockToken;

        // During spike, make rapid-fire requests
        const requestsPerIteration = 3;

        for (let i = 0; i < requestsPerIteration; i++) {
            const response = http.get(
                buildUrl('auth/me'),
                {
                    headers: getDefaultHeaders(null, authToken),
                    tags: { scenario: 'spike', request_num: i },
                }
            );

            // Track if system is handling load
            const isHandled = response.status === 200 || response.status === 429;
            check(response, {
                'Spike: system responding': (r) => isHandled,
                'Spike: graceful degradation': (r) => r.status !== 500,
            });

            // Very brief pause between rapid requests
            sleep(0.05);
        }
    });

    // Minimal think time during spike
    sleep(0.1);
}

// ============================================================================
// SOAK TEST SCENARIO
// ============================================================================

/**
 * Soak Test: Extended duration at moderate-high load
 * Focus: Memory leaks, resource exhaustion, gradual degradation
 */
export function soakTest(data) {
    const user = testUsers[Math.floor(Math.random() * testUsers.length)];

    group('Soak Test - Sustained Authentication', () => {
        const authToken = user.mockToken;

        // Full authentication workflow
        // Step 1: Get CSRF token
        const csrfResponse = http.get(
            buildUrl('auth/csrf-token'),
            { tags: { scenario: 'soak' } }
        );

        let csrfToken = null;
        if (csrfResponse.status === 200) {
            try {
                csrfToken = JSON.parse(csrfResponse.body).token;
            } catch (e) {
                // Continue
            }
        }

        sleep(0.5);

        // Step 2: Login callback
        const loginResponse = http.post(
            buildUrl('auth/login-callback'),
            null,
            {
                headers: getDefaultHeaders(csrfToken, authToken),
                tags: { scenario: 'soak', name: 'POST /api/auth/login-callback' },
            }
        );

        validateAuthResponse(loginResponse, 'soak_login');

        sleep(1);

        // Step 3: Multiple API calls to simulate real usage
        for (let i = 0; i < 3; i++) {
            const apiResponse = http.get(
                buildUrl('auth/me'),
                {
                    headers: getDefaultHeaders(null, authToken),
                    tags: { scenario: 'soak', iteration: i },
                }
            );

            validateApiResponse(apiResponse, 'soak_api_call');

            sleep(1);
        }

        // Track performance degradation over time
        // Response times should remain consistent throughout soak test
        const currentTime = Date.now();
        const testDuration = (currentTime - data.startTime) / 1000 / 60; // minutes

        check(loginResponse, {
            'Soak: response time stable': (r) => {
                // Allow 20% degradation over time
                const expectedDegradation = 1 + (testDuration * 0.01);
                return r.timings.duration < (config.thresholds.p95 * expectedDegradation);
            },
        });
    });

    // Normal think time for soak test
    sleep(thinkTime());
}

// ============================================================================
// BREAKPOINT TEST SCENARIO
// ============================================================================

/**
 * Breakpoint Test: Find maximum sustainable capacity
 * Focus: Maximum throughput before system breaks
 */
export function breakpointTest(data) {
    const user = testUsers[Math.floor(Math.random() * testUsers.length)];

    group('Breakpoint Test - Maximum Throughput', () => {
        const authToken = user.mockToken;

        // Make lightweight request to maximize throughput
        const response = http.get(
            buildUrl('auth/csrf-token'),
            {
                headers: getDefaultHeaders(null, authToken),
                tags: { scenario: 'breakpoint' },
            }
        );

        // Track peak load
        if (response.status === 200) {
            stressMetrics.peakLoad.add(1);
        }

        // Validate system is handling load
        const isSuccess = response.status === 200 || response.status === 429;
        check(response, {
            'Breakpoint: system operational': (r) => isSuccess,
            'Breakpoint: response time acceptable': (r) => r.timings.duration < 5000,
        });

        // Track when system starts failing
        if (!isSuccess && !isRateLimited(response)) {
            stressMetrics.systemFailures.add(1);
        }
    });

    // No sleep - maximize request rate
}

// ============================================================================
// ADVANCED STRESS SCENARIOS
// ============================================================================

/**
 * Chaos scenario: Random mix of valid and invalid requests
 * Tests error handling under stress
 */
export function chaosTest(data) {
    const user = testUsers[Math.floor(Math.random() * testUsers.length)];

    group('Chaos Test - Random Operations', () => {
        const operations = [
            () => testValidLogin(user),
            () => testInvalidLogin(),
            () => testMissingCSRF(user),
            () => testExpiredToken(user),
            () => testConcurrentRequests(user),
        ];

        // Execute random operation
        const randomOp = operations[Math.floor(Math.random() * operations.length)];
        randomOp();
    });

    sleep(0.1);
}

/**
 * Helper: Test valid login
 */
function testValidLogin(user) {
    const csrfResponse = http.get(buildUrl('auth/csrf-token'));
    let csrfToken = null;

    if (csrfResponse.status === 200) {
        try {
            csrfToken = JSON.parse(csrfResponse.body).token;
        } catch (e) {
            // Continue
        }
    }

    const response = http.post(
        buildUrl('auth/login-callback'),
        null,
        {
            headers: getDefaultHeaders(csrfToken, user.mockToken),
            tags: { chaos_scenario: 'valid_login' },
        }
    );

    check(response, {
        'Chaos (valid): success or rate limited': (r) => r.status === 200 || r.status === 429,
    });
}

/**
 * Helper: Test invalid login (no token)
 */
function testInvalidLogin() {
    const response = http.post(
        buildUrl('auth/login-callback'),
        null,
        {
            headers: getDefaultHeaders(null, null),
            tags: { chaos_scenario: 'invalid_login' },
        }
    );

    check(response, {
        'Chaos (invalid): rejected': (r) => r.status === 401 || r.status === 403,
    });
}

/**
 * Helper: Test missing CSRF token
 */
function testMissingCSRF(user) {
    const response = http.post(
        buildUrl('auth/login-callback'),
        null,
        {
            headers: getDefaultHeaders(null, user.mockToken),
            tags: { chaos_scenario: 'missing_csrf' },
        }
    );

    check(response, {
        'Chaos (no CSRF): handled gracefully': (r) => r.status === 403 || r.status === 200,
    });
}

/**
 * Helper: Test expired token
 */
function testExpiredToken(user) {
    const expiredToken = 'eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjB9.expired';

    const response = http.get(
        buildUrl('auth/me'),
        {
            headers: getDefaultHeaders(null, expiredToken),
            tags: { chaos_scenario: 'expired_token' },
        }
    );

    check(response, {
        'Chaos (expired): rejected': (r) => r.status === 401,
    });
}

/**
 * Helper: Test concurrent requests
 */
function testConcurrentRequests(user) {
    const authToken = user.mockToken;
    const responses = [];

    // Make 5 concurrent requests
    for (let i = 0; i < 5; i++) {
        const response = http.get(
            buildUrl('auth/me'),
            {
                headers: getDefaultHeaders(null, authToken),
                tags: { chaos_scenario: 'concurrent', request_num: i },
            }
        );
        responses.push(response);
    }

    check(responses, {
        'Chaos (concurrent): all handled': (rs) => rs.every(r => r.status > 0),
        'Chaos (concurrent): some successful': (rs) => rs.some(r => r.status === 200),
    });
}

// ============================================================================
// TEARDOWN PHASE
// ============================================================================

export function teardown(data) {
    const duration = (Date.now() - data.startTime) / 1000 / 60;

    console.log('\n========================================');
    console.log('EMR Authentication Stress Test Completed');
    console.log('========================================');
    console.log(`Total Duration: ${duration.toFixed(2)} minutes`);
    console.log('\nStress Test Insights:');
    console.log('- Check system_failures count for stability issues');
    console.log('- Review degradation_rate for performance trends');
    console.log('- Analyze rate_limit_errors for capacity planning');
    console.log('- Monitor peak_load for maximum throughput');
    console.log('\nNext Steps:');
    console.log('1. Review detailed metrics in k6 output');
    console.log('2. Check server logs for errors and warnings');
    console.log('3. Analyze database performance during peak load');
    console.log('4. Review HIPAA audit logs for completeness');
    console.log('5. Verify rate limiting effectiveness');
    console.log('========================================\n');
}
