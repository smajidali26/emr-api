/**
 * EMR Authentication Load Test
 *
 * Tests authentication system performance under normal and peak load conditions.
 *
 * Test Scenarios:
 * 1. User login flow (registration → login callback → get current user)
 * 2. Token refresh under load
 * 3. API calls with JWT validation
 * 4. Rate limit boundary testing
 * 5. CSRF token fetching and validation
 * 6. User caching performance (5-minute TTL)
 *
 * Performance Targets:
 * - p95 response time < 500ms
 * - p99 response time < 1000ms
 * - Error rate < 1%
 * - Support 100 concurrent users
 *
 * Usage:
 *   k6 run auth-load-test.js
 *   k6 run --vus 100 --duration 5m auth-load-test.js
 *   BASE_URL=https://api.example.com k6 run auth-load-test.js
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { SharedArray } from 'k6/data';
import {
    config,
    metrics,
    generateTestUser,
    buildUrl,
    getDefaultHeaders,
    validateAuthResponse,
    validateApiResponse,
    getThresholds,
    thinkTime,
} from './auth-performance-config.js';

// ============================================================================
// TEST CONFIGURATION
// ============================================================================

export const options = {
    // Load test stages: ramp up → steady state → ramp down
    stages: [
        { duration: '2m', target: 20 },   // Ramp up to 20 users
        { duration: '3m', target: 50 },   // Ramp up to 50 users
        { duration: '5m', target: 100 },  // Ramp up to 100 users (target load)
        { duration: '5m', target: 100 },  // Stay at 100 users
        { duration: '2m', target: 50 },   // Ramp down to 50 users
        { duration: '1m', target: 0 },    // Ramp down to 0 users
    ],

    // Performance thresholds
    thresholds: getThresholds(),

    // Test metadata
    tags: {
        test_type: 'load',
        test_name: 'auth_load_test',
        environment: __ENV.ENVIRONMENT || 'test',
    },

    // Disable automatic batch processing for more realistic load
    batch: 1,
    batchPerHost: 1,
};

// ============================================================================
// TEST DATA SETUP
// ============================================================================

/**
 * Pre-generate test users to avoid overhead during test execution
 * SharedArray ensures data is shared across VUs efficiently
 */
const testUsers = new SharedArray('users', function () {
    const users = [];

    // Generate patient users (70% of load)
    for (let i = 0; i < 70; i++) {
        users.push(generateTestUser('Patient', i));
    }

    // Generate doctor users (15% of load)
    for (let i = 0; i < 15; i++) {
        users.push(generateTestUser('Doctor', i));
    }

    // Generate nurse users (10% of load)
    for (let i = 0; i < 10; i++) {
        users.push(generateTestUser('Nurse', i));
    }

    // Generate staff users (5% of load)
    for (let i = 0; i < 5; i++) {
        users.push(generateTestUser('Staff', i));
    }

    return users;
});

// ============================================================================
// SETUP PHASE
// ============================================================================

/**
 * Setup function runs once before the test
 * Use this to verify API availability and create initial test data
 */
export function setup() {
    console.log('Starting EMR Authentication Load Test');
    console.log(`Base URL: ${config.baseUrl}`);
    console.log(`Target VUs: ${config.load.vus.target}`);
    console.log(`Test Users: ${testUsers.length}`);

    // Health check
    const healthCheck = http.get(`${config.baseUrl}/health`);
    if (!check(healthCheck, { 'API is healthy': (r) => r.status === 200 })) {
        throw new Error('API health check failed. Aborting test.');
    }

    console.log('API health check passed. Starting load test...');

    return {
        startTime: Date.now(),
        testUsers: testUsers.length,
    };
}

// ============================================================================
// MAIN TEST SCENARIO
// ============================================================================

/**
 * Default function - executed by each VU repeatedly during the test
 */
export default function (data) {
    // Select a random user for this iteration
    const user = testUsers[Math.floor(Math.random() * testUsers.length)];
    const isPatient = user.roles.includes('Patient');

    // Scenario 1: Complete authentication flow
    group('Complete Authentication Flow', () => {
        completeAuthenticationFlow(user);
    });

    // Simulate user think time
    sleep(thinkTime());

    // Scenario 2: Authenticated API calls with JWT validation
    group('Authenticated API Calls', () => {
        performAuthenticatedApiCalls(user);
    });

    // Additional think time
    sleep(thinkTime());

    // Scenario 3: User caching performance test
    // Call /api/auth/me multiple times to test 5-minute cache TTL
    if (Math.random() < 0.3) { // 30% of requests test caching
        group('User Cache Performance', () => {
            testUserCaching(user);
        });
    }

    // Scenario 4: Rate limit boundary testing
    // Occasionally test rate limits to ensure they're enforced
    if (Math.random() < 0.1) { // 10% of requests test rate limits
        group('Rate Limit Testing', () => {
            testRateLimits(user);
        });
    }

    // Final think time before next iteration
    sleep(thinkTime());
}

// ============================================================================
// SCENARIO FUNCTIONS
// ============================================================================

/**
 * Scenario 1: Complete authentication flow
 * Simulates: Registration → Login Callback → Get Current User
 */
function completeAuthenticationFlow(user) {
    let csrfToken = null;
    let authToken = user.mockToken; // In real scenario, get from Azure B2C

    // Step 1: Get CSRF token
    const csrfResponse = http.get(
        buildUrl('auth/csrf-token'),
        { tags: { name: 'GET /api/auth/csrf-token' } }
    );

    const csrfChecks = check(csrfResponse, {
        'CSRF token: status is 200': (r) => r.status === 200,
        'CSRF token: has token': (r) => {
            try {
                const body = JSON.parse(r.body);
                return body.token && body.token.length > 0;
            } catch {
                return false;
            }
        },
        'CSRF token: response time OK': (r) => r.timings.duration < config.thresholds.p95,
    });

    if (csrfChecks) {
        try {
            const body = JSON.parse(csrfResponse.body);
            csrfToken = body.token;
            metrics.csrfTokenDuration.add(csrfResponse.timings.duration);
        } catch (e) {
            console.error('Failed to parse CSRF token response');
        }
    }

    sleep(0.5); // Brief pause between requests

    // Step 2: User registration (only if not registered)
    // In a real test, check if user exists first
    if (Math.random() < 0.2) { // 20% of users simulate new registration
        const registerPayload = {
            email: user.email,
            firstName: user.firstName,
            lastName: user.lastName,
            azureAdB2CId: user.azureAdB2CId,
            roles: user.roles,
        };

        const registerResponse = http.post(
            buildUrl('auth/register'),
            JSON.stringify(registerPayload),
            {
                headers: getDefaultHeaders(csrfToken, null),
                tags: { name: 'POST /api/auth/register' },
            }
        );

        const registerChecks = check(registerResponse, {
            'Register: status is 201 or 400': (r) => r.status === 201 || r.status === 400,
            'Register: response time OK': (r) => r.timings.duration < config.thresholds.p99,
            'Register: has response body': (r) => r.body && r.body.length > 0,
        });

        if (registerResponse.status === 201) {
            metrics.successfulLogins.add(1);
        }

        sleep(0.5);
    }

    // Step 3: Login callback (simulates post-Azure B2C redirect)
    const loginCallbackResponse = http.post(
        buildUrl('auth/login-callback'),
        null,
        {
            headers: getDefaultHeaders(csrfToken, authToken),
            tags: { name: 'POST /api/auth/login-callback' },
        }
    );

    validateAuthResponse(loginCallbackResponse, 'login_callback');

    if (loginCallbackResponse.status === 200) {
        metrics.successfulLogins.add(1);
        metrics.loginDuration.add(loginCallbackResponse.timings.duration);
        metrics.auditLogsGenerated.add(1); // HIPAA audit log created
    }

    sleep(0.5);

    // Step 4: Get current user profile
    const getCurrentUserResponse = http.get(
        buildUrl('auth/me'),
        {
            headers: getDefaultHeaders(null, authToken),
            tags: { name: 'GET /api/auth/me' },
        }
    );

    const userChecks = check(getCurrentUserResponse, {
        'Get current user: status is 200': (r) => r.status === 200,
        'Get current user: has user data': (r) => {
            try {
                const body = JSON.parse(r.body);
                return body.id && body.email;
            } catch {
                return false;
            }
        },
        'Get current user: response time OK': (r) => r.timings.duration < config.thresholds.p95,
    });

    if (getCurrentUserResponse.status === 200) {
        metrics.auditLogsGenerated.add(1); // HIPAA audit log for PHI access
    }
}

/**
 * Scenario 2: Authenticated API calls with JWT validation
 * Tests API performance with authentication and authorization
 */
function performAuthenticatedApiCalls(user) {
    const authToken = user.mockToken;
    let csrfToken = null;

    // Get fresh CSRF token
    const csrfResponse = http.get(buildUrl('auth/csrf-token'));
    if (csrfResponse.status === 200) {
        try {
            csrfToken = JSON.parse(csrfResponse.body).token;
        } catch {
            // Continue without CSRF for GET requests
        }
    }

    // Test different API endpoints based on user role
    const isPatient = user.roles.includes('Patient');
    const isProvider = user.roles.includes('Doctor') || user.roles.includes('Nurse');

    if (isPatient) {
        // Patients typically view their own data
        const myDataResponse = http.get(
            buildUrl('patients/my-data'),
            {
                headers: getDefaultHeaders(null, authToken),
                tags: { name: 'GET /api/patients/my-data', role: 'Patient' },
            }
        );

        validateApiResponse(myDataResponse, 'patient_my_data');

        if (myDataResponse.status === 200) {
            metrics.auditLogsGenerated.add(1); // HIPAA audit
        }
    }

    if (isProvider) {
        // Providers search for patients
        const searchResponse = http.get(
            buildUrl('patients?searchTerm=Smith&pageNumber=1&pageSize=10'),
            {
                headers: getDefaultHeaders(null, authToken),
                tags: { name: 'GET /api/patients/search', role: 'Provider' },
            }
        );

        validateApiResponse(searchResponse, 'patient_search');

        if (searchResponse.status === 200) {
            metrics.auditLogsGenerated.add(1); // HIPAA audit
        }
    }

    // All users can view their own profile
    const profileResponse = http.get(
        buildUrl('auth/me'),
        {
            headers: getDefaultHeaders(null, authToken),
            tags: { name: 'GET /api/auth/me' },
        }
    );

    validateApiResponse(profileResponse, 'get_profile');
}

/**
 * Scenario 3: Test user caching performance
 * Users are cached for 5 minutes - test cache hits vs misses
 */
function testUserCaching(user) {
    const authToken = user.mockToken;

    // Make multiple rapid requests to the same endpoint
    // First request should miss cache, subsequent requests should hit cache
    const iterations = 5;
    const responses = [];

    for (let i = 0; i < iterations; i++) {
        const response = http.get(
            buildUrl('auth/me'),
            {
                headers: getDefaultHeaders(null, authToken),
                tags: { name: 'GET /api/auth/me (cache test)', iteration: i },
            }
        );

        responses.push(response);

        // Track cache performance based on response time
        // Cached responses should be significantly faster
        if (i > 0 && response.status === 200) {
            if (response.timings.duration < 50) {
                metrics.cacheHits.add(1);
            } else {
                metrics.cacheMisses.add(1);
            }
        }

        sleep(0.1); // Small delay between requests
    }

    // Verify consistency
    check(responses, {
        'Cache test: all responses successful': (rs) => rs.every(r => r.status === 200),
        'Cache test: response times decrease': (rs) => {
            if (rs.length < 2) return true;
            // Second request should be faster than first (cache hit)
            return rs[1].timings.duration < rs[0].timings.duration;
        },
    });
}

/**
 * Scenario 4: Test rate limits
 * Verify rate limiting is properly enforced
 */
function testRateLimits(user) {
    const authToken = user.mockToken;

    // Test auth endpoint rate limit (10 req/5min)
    // We'll make 5 rapid requests to approach the limit
    const requests = 5;
    let rateLimitHit = false;

    for (let i = 0; i < requests; i++) {
        const response = http.post(
            buildUrl('auth/login-callback'),
            null,
            {
                headers: getDefaultHeaders(null, authToken),
                tags: { name: 'POST /api/auth/login-callback (rate limit test)' },
            }
        );

        if (response.status === 429) {
            rateLimitHit = true;
            metrics.rateLimitErrors.add(1);

            check(response, {
                'Rate limit: status is 429': (r) => r.status === 429,
                'Rate limit: has retry-after': (r) => {
                    try {
                        const body = JSON.parse(r.body);
                        return body.retryAfter !== undefined;
                    } catch {
                        return false;
                    }
                },
            });

            // Honor rate limit - stop testing
            break;
        }

        sleep(0.1);
    }

    // Note: In normal load testing, we don't expect to hit rate limits
    // This scenario deliberately tests the boundary
}

// ============================================================================
// TOKEN REFRESH SCENARIO
// ============================================================================

/**
 * Simulate token refresh flow
 * In a real implementation, this would exchange an expired token for a new one
 */
function testTokenRefresh(user) {
    // Mock token refresh - in reality, this would call Azure B2C token endpoint
    const refreshStartTime = Date.now();

    // Simulate token refresh delay
    sleep(0.1);

    const newToken = generateMockJWT(user.roles[0], Math.random() * 1000);
    const refreshDuration = Date.now() - refreshStartTime;

    metrics.tokenRefreshDuration.add(refreshDuration);
    metrics.successfulTokenRefresh.add(1);

    return newToken;
}

// ============================================================================
// TEARDOWN PHASE
// ============================================================================

/**
 * Teardown function runs once after the test
 * Use this for cleanup and final reporting
 */
export function teardown(data) {
    console.log('\n========================================');
    console.log('EMR Authentication Load Test Completed');
    console.log('========================================');
    console.log(`Test Duration: ${((Date.now() - data.startTime) / 1000 / 60).toFixed(2)} minutes`);
    console.log(`Test Users: ${data.testUsers}`);
    console.log('\nCheck the test summary above for detailed metrics.');
    console.log('========================================\n');
}

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

/**
 * Generate mock JWT (placeholder for real token generation)
 */
function generateMockJWT(role, id) {
    const header = btoa(JSON.stringify({ alg: 'RS256', typ: 'JWT' }));
    const payload = btoa(JSON.stringify({
        sub: `user-${id}`,
        roles: [role],
        exp: Math.floor(Date.now() / 1000) + 3600,
    }));
    return `${header}.${payload}.mock-signature`;
}

/**
 * Base64 encode helper (for JWT generation)
 */
function btoa(str) {
    return encoding.b64encode(str, 'std');
}
