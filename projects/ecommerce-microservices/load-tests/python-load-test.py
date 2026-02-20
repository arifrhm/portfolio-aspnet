#!/usr/bin/env python3
"""
Load Test Script for Product Service
Uses Python's asyncio and aiohttp for concurrent HTTP requests
"""

import asyncio
import time
import random
import statistics
from datetime import datetime
from typing import List, Dict
from dataclasses import dataclass
import aiohttp
from aiohttp import ClientSession


@dataclass
class TestResult:
    endpoint: str
    status_code: int
    response_time_ms: float
    success: bool


class LoadTestRunner:
    def __init__(self, base_url: str = "http://localhost:5000"):
        self.base_url = base_url
        self.tenants = ["company-a", "company-b", "company-c"]
        self.results: List[TestResult] = []

    async def get_all_products(self, session: ClientSession, tenant: str) -> TestResult:
        """GET /api/products"""
        start_time = time.time()

        headers = {
            "Accept": "application/json",
            "X-Tenant-Slug": tenant
        }

        try:
            async with session.get(
                f"{self.base_url}/api/products",
                headers=headers
            ) as response:
                response_time = (time.time() - start_time) * 1000

                return TestResult(
                    endpoint="GET /api/products",
                    status_code=response.status,
                    response_time_ms=response_time,
                    success=response.status == 200
                )
        except Exception as e:
            response_time = (time.time() - start_time) * 1000
            return TestResult(
                endpoint="GET /api/products",
                status_code=0,
                response_time_ms=response_time,
                success=False
            )

    async def get_product_by_id(self, session: ClientSession, tenant: str, product_id: str) -> TestResult:
        """GET /api/products/{id}"""
        start_time = time.time()

        headers = {
            "Accept": "application/json",
            "X-Tenant-Slug": tenant
        }

        try:
            async with session.get(
                f"{self.base_url}/api/products/{product_id}",
                headers=headers
            ) as response:
                response_time = (time.time() - start_time) * 1000

                return TestResult(
                    endpoint="GET /api/products/{id}",
                    status_code=response.status,
                    response_time_ms=response_time,
                    success=response.status == 200
                )
        except Exception as e:
            response_time = (time.time() - start_time) * 1000
            return TestResult(
                endpoint="GET /api/products/{id}",
                status_code=0,
                response_time_ms=response_time,
                success=False
            )

    async def create_product(self, session: ClientSession, tenant: str) -> TestResult:
        """POST /api/products"""
        start_time = time.time()

        product_data = {
            "name": f"Load Test Product {random.randint(1, 100000)}",
            "description": "Product generated during load testing",
            "price": random.randint(10000, 100000),
            "stockQuantity": random.randint(10, 100),
            "category": "Electronics",
            "sku": f"SKU-{int(time.time() * 1000)}-{random.randint(1, 1000)}"
        }

        headers = {
            "Content-Type": "application/json",
            "Accept": "application/json",
            "X-Tenant-Slug": tenant
        }

        try:
            async with session.post(
                f"{self.base_url}/api/products",
                json=product_data,
                headers=headers
            ) as response:
                response_time = (time.time() - start_time) * 1000

                return TestResult(
                    endpoint="POST /api/products",
                    status_code=response.status,
                    response_time_ms=response_time,
                    success=response.status == 201
                )
        except Exception as e:
            response_time = (time.time() - start_time) * 1000
            return TestResult(
                endpoint="POST /api/products",
                status_code=0,
                response_time_ms=response_time,
                success=False
            )

    async def run_scenario(
        self,
        scenario_name: str,
        num_requests: int,
        concurrency: int
    ):
        """Run a load test scenario"""
        print(f"\n{'='*60}")
        print(f"Running: {scenario_name}")
        print(f"Requests: {num_requests}, Concurrency: {concurrency}")
        print(f"{'='*60}\n")

        self.results.clear()
        start_time = time.time()

        # Create tasks
        tasks = []
        semaphore = asyncio.Semaphore(concurrency)

        async def bounded_request():
            async with semaphore:
                tenant = random.choice(self.tenants)
                operation = random.random()

                if operation < 0.5:
                    # 50% get all products
                    result = await self.get_all_products(await self._get_session(), tenant)
                elif operation < 0.8:
                    # 30% get by ID
                    product_id = str(random.randint(1, 1000))
                    result = await self.get_product_by_id(await self._get_session(), tenant, product_id)
                else:
                    # 20% create product
                    result = await self.create_product(await self._get_session(), tenant)

                self.results.append(result)

        # Create all tasks
        for _ in range(num_requests):
            tasks.append(bounded_request())

        # Wait for all tasks to complete
        await asyncio.gather(*tasks)

        total_time = time.time() - start_time

        # Print results
        self._print_results(scenario_name, num_requests, total_time)

    async def _get_session(self) -> ClientSession:
        """Get or create an HTTP session"""
        if not hasattr(self, '_session') or self._session.closed:
            timeout = aiohttp.ClientTimeout(total=30)
            self._session = ClientSession(timeout=timeout)
        return self._session

    async def close_session(self):
        """Close the HTTP session"""
        if hasattr(self, '_session'):
            await self._session.close()

    def _print_results(self, scenario_name: str, num_requests: int, total_time: float):
        """Print test results"""
        successful_results = [r for r in self.results if r.success]
        failed_results = [r for r in self.results if not r.success]

        success_rate = (len(successful_results) / num_requests) * 100
        req_per_sec = num_requests / total_time

        response_times = [r.response_time_ms for r in successful_results]
        avg_response = statistics.mean(response_times) if response_times else 0
        p50_response = statistics.quantiles(response_times, n=2)[0] if len(response_times) >= 2 else 0
        p95_response = statistics.quantiles(response_times, n=20)[18] if len(response_times) >= 20 else 0
        min_response = min(response_times) if response_times else 0
        max_response = max(response_times) if response_times else 0

        print(f"Results for {scenario_name}:")
        print(f"  Total Requests: {num_requests}")
        print(f"  Successful: {len(successful_results)}")
        print(f"  Failed: {len(failed_results)}")
        print(f"  Success Rate: {success_rate:.2f}%")
        print(f"  Total Time: {total_time:.2f}s")
        print(f"  Requests/sec: {req_per_sec:.2f}")
        print(f"\nResponse Times (successful requests):")
        print(f"  Average: {avg_response:.2f}ms")
        print(f"  Median (p50): {p50_response:.2f}ms")
        print(f"  95th percentile: {p95_response:.2f}ms")
        print(f"  Min: {min_response:.2f}ms")
        print(f"  Max: {max_response:.2f}ms")

        # Results by endpoint
        print(f"\nResults by Endpoint:")
        endpoints = set(r.endpoint for r in self.results)
        for endpoint in sorted(endpoints):
            endpoint_results = [r for r in self.results if r.endpoint == endpoint]
            endpoint_success = [r for r in endpoint_results if r.success]
            endpoint_rate = (len(endpoint_success) / len(endpoint_results)) * 100
            print(f"  {endpoint}: {len(endpoint_success)}/{len(endpoint_results)} ({endpoint_rate:.1f}%)")


async def main():
    """Main test runner"""
    print("="*60)
    print("Product Service Load Test")
    print(f"Target: http://localhost:5000")
    print(f"Started at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("="*60)

    runner = LoadTestRunner()

    try:
        # Scenario 1: Low concurrency (10 users, 100 requests)
        await runner.run_scenario("Low Concurrency Test", num_requests=100, concurrency=10)

        # Scenario 2: Medium concurrency (50 users, 500 requests)
        await runner.run_scenario("Medium Concurrency Test", num_requests=500, concurrency=50)

        # Scenario 3: High concurrency (100 users, 1000 requests)
        await runner.run_scenario("High Concurrency Test", num_requests=1000, concurrency=100)

        # Scenario 4: Stress test (200 users, 2000 requests)
        await runner.run_scenario("Stress Test", num_requests=2000, concurrency=200)

        print(f"\n{'='*60}")
        print("All tests completed!")
        print(f"Finished at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        print("="*60)

    finally:
        await runner.close_session()


if __name__ == "__main__":
    asyncio.run(main())
