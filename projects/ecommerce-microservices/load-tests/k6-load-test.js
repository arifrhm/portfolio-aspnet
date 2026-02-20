import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const latency = new Trend('latency');

// Test configuration
export const options = {
  stages: [
    { duration: '1m', target: 10 },   // Ramp up to 10 users
    { duration: '2m', target: 50 },   // Ramp up to 50 users
    { duration: '3m', target: 100 },  // Ramp up to 100 users
    { duration: '2m', target: 50 },   // Ramp down to 50 users
    { duration: '1m', target: 10 },   // Ramp down to 10 users
    { duration: '1m', target: 0 },     // Ramp down to 0
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],  // 95% of requests must complete below 500ms
    http_req_failed: ['rate<0.05'],    // Error rate must be less than 5%
    errors: ['rate<0.05'],
  },
};

const BASE_URL = 'http://localhost:5000';
const TENANTS = ['company-a', 'company-b', 'company-c'];

// Helper function to get random tenant
function getRandomTenant() {
  return TENANTS[Math.floor(Math.random() * TENANTS.length)];
}

// Test data generator
function generateProduct() {
  return JSON.stringify({
    name: `Load Test Product ${__VU}-${__ITER}`,
    description: 'Product generated during load testing',
    price: 10000 + Math.floor(Math.random() * 90000),
    stockQuantity: 10 + Math.floor(Math.random() * 90),
    category: 'Electronics',
    sku: `SKU-${Date.now()}-${__VU}-${__ITER}`
  });
}

// Scenario 1: Get all products
export function getAllProducts(tenant) {
  const params = {
    headers: {
      'Accept': 'application/json',
      'X-Tenant-Slug': tenant,
    },
  };

  const res = http.get(`${BASE_URL}/api/products`, params);

  const success = check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 200ms': (r) => r.timings.duration < 200,
  });

  errorRate.add(!success);
  latency.add(res.timings.duration);

  return success;
}

// Scenario 2: Get product by ID
export function getProductById(tenant) {
  const productId = '00000000-0000-0000-0000-000000000001'; // Mock product ID
  const params = {
    headers: {
      'Accept': 'application/json',
      'X-Tenant-Slug': tenant,
    },
  };

  const res = http.get(`${BASE_URL}/api/products/${productId}`, params);

  const success = check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 100ms': (r) => r.timings.duration < 100,
  });

  errorRate.add(!success);
  latency.add(res.timings.duration);

  return success;
}

// Scenario 3: Create product
export function createProduct(tenant) {
  const params = {
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      'X-Tenant-Slug': tenant,
    },
  };

  const res = http.post(`${BASE_URL}/api/products`, generateProduct(), params);

  const success = check(res, {
    'status is 201': (r) => r.status === 201,
    'has product data': (r) => JSON.parse(r.body).id !== undefined,
  });

  errorRate.add(!success);
  latency.add(res.timings.duration);

  return success;
}

// Scenario 4: Multi-tenant operations
export function multiTenantOperations() {
  const tenant = getRandomTenant();
  const operation = Math.random();

  if (operation < 0.6) {
    // 60% reads
    return getAllProducts(tenant);
  } else if (operation < 0.8) {
    // 20% get by ID
    return getProductById(tenant);
  } else {
    // 20% writes
    return createProduct(tenant);
  }
}

// Main test function
export default function () {
  const tenant = getRandomTenant();
  multiTenantOperations();

  // Random sleep between requests (0.1s to 0.5s)
  sleep(Math.random() * 0.4 + 0.1);
}

// Setup function (runs once at the beginning)
export function setup() {
  console.log('Starting load test...');
  console.log(`Target: ${BASE_URL}`);
  console.log(`Tenants: ${TENANTS.join(', ')}`);
}

// Teardown function (runs once at the end)
export function teardown(data) {
  console.log('Load test completed!');
  console.log(`Total requests: ${__ITER}`);
}
