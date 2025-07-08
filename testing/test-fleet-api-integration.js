/**
 * Fleet Management API Integration Test
 * Tests all CRUD operations and analytics endpoints
 */

const BASE_URL = 'http://localhost:5074/api';
let testVehicleId = null;
let testFuelLogId = null;
let testMaintenanceId = null;
let testFinancialId = null;
let testInsuranceId = null;

// Test data
const testVehicle = {
    name: "Test Fleet Vehicle",
    vin: "1FTFW1ET5DFC71234", // Different VIN for each test run
    make: "Ford",
    model: "F-150",
    year: 2023,
    licensePlate: "FLEET1",
    odometerReading: 15000,
    status: 0, // Active = 0
    acquisitionDate: "2023-01-15T00:00:00Z"
};

const testFuelLog = {
    vehicleId: null, // Will be set after vehicle creation
    fuelDate: "2024-01-15T00:00:00Z",
    odometerReading: 16500,
    gallons: 12.5,
    pricePerGallon: 3.45,
    totalCost: 43.13,
    fuelType: "Regular",
    location: "Shell Station Downtown",
    notes: "Regular fill-up"
};

const testMaintenance = {
    vehicleId: null, // Will be set after vehicle creation
    maintenanceType: 0, // OilChange = 0
    maintenanceDate: "2024-01-10T00:00:00Z",
    odometerReading: 16000,
    description: "Regular oil change and filter replacement",
    cost: 45.99,
    performedBy: "Quick Lube Express",
    nextMaintenanceDate: "2024-04-10T00:00:00Z",
    warranty: "90 days"
};

const testFinancial = {
    vehicleId: null, // Will be set after vehicle creation
    financialType: 8, // Insurance = 8
    amount: 150.00,
    startDate: "2024-01-01T00:00:00Z",
    endDate: "2024-12-31T23:59:59Z",
    paymentFrequency: 0, // Monthly = 0
    description: "Monthly insurance premium",
    vendor: "Test Insurance Co"
};

const testInsurance = {
    policyNumber: "POL-TEST-001",
    providerName: "Test Insurance Co",
    providerContact: "test@insurance.com",
    startDate: "2024-01-01T00:00:00Z",
    endDate: "2024-12-31T23:59:59Z",
    premiumAmount: 1800.00,
    paymentFrequency: 3, // Annually = 3
    deductible: 500.00,
    coverage: [1, 2, 3], // Liability, Collision, Comprehensive
    isActive: true
};

// Utility functions
async function apiCall(method, endpoint, data = null) {
    const url = `${BASE_URL}${endpoint}`;
    const options = {
        method,
        headers: {
            'Content-Type': 'application/json',
        }
    };

    if (data) {
        options.body = JSON.stringify(data);
    }

    try {
        const response = await fetch(url, options);
        const responseData = await response.text();
        
        let jsonData = null;
        try {
            jsonData = JSON.parse(responseData);
        } catch (e) {
            jsonData = responseData;
        }

        return {
            status: response.status,
            data: jsonData,
            ok: response.ok
        };
    } catch (error) {
        console.error(`API call failed: ${method} ${url}`, error);
        return {
            status: 0,
            data: { error: error.message },
            ok: false
        };
    }
}

async function runTest(name, testFunction) {
    console.log(`\n🧪 Running test: ${name}`);
    try {
        await testFunction();
        console.log(`✅ PASSED: ${name}`);
        return true;
    } catch (error) {
        console.error(`❌ FAILED: ${name} - ${error.message}`);
        return false;
    }
}

// Test functions
async function testVehiclesCRUD() {
    // Create vehicle
    let response = await apiCall('POST', '/vehicles', testVehicle);
    if (!response.ok) throw new Error(`Failed to create vehicle: ${response.status} - ${JSON.stringify(response.data)}`);
    testVehicleId = response.data.id;
    console.log(`   Created vehicle ID: ${testVehicleId}`);

    // Get vehicle
    response = await apiCall('GET', `/vehicles/${testVehicleId}`);
    if (!response.ok) throw new Error(`Failed to get vehicle: ${response.status}`);
    console.log(`   Retrieved vehicle: ${response.data.name}`);

    // Update vehicle
    const updatedVehicle = { ...testVehicle, odometerReading: 16000 };
    response = await apiCall('PUT', `/vehicles/${testVehicleId}`, updatedVehicle);
    if (!response.ok) throw new Error(`Failed to update vehicle: ${response.status}`);
    console.log(`   Updated vehicle odometer`);

    // Get all vehicles
    response = await apiCall('GET', '/vehicles');
    if (!response.ok) throw new Error(`Failed to get all vehicles: ${response.status}`);
    console.log(`   Retrieved ${response.data.length} vehicles`);
}

async function testVehicleAnalytics() {
    // Get vehicle statistics
    let response = await apiCall('GET', '/vehicles/statistics');
    if (!response.ok) throw new Error(`Failed to get vehicle statistics: ${response.status}`);
    console.log(`   Vehicle statistics: ${response.data.totalVehicles} total vehicles`);

    // Filter vehicles by status
    response = await apiCall('GET', '/vehicles/filter?status=0'); // Active status
    if (!response.ok) throw new Error(`Failed to filter vehicles: ${response.status}`);
    console.log(`   Filtered vehicles: ${response.data.length} active vehicles`);
}

async function testFuelLogsCRUD() {
    if (!testVehicleId) throw new Error('Vehicle ID not available for fuel log test');

    // Create fuel log
    const fuelLogData = { ...testFuelLog, vehicleId: testVehicleId };
    let response = await apiCall('POST', '/fuellogs', fuelLogData);
    if (!response.ok) throw new Error(`Failed to create fuel log: ${response.status} - ${JSON.stringify(response.data)}`);
    testFuelLogId = response.data.id;
    console.log(`   Created fuel log ID: ${testFuelLogId}`);

    // Get fuel log
    response = await apiCall('GET', `/fuellogs/${testFuelLogId}`);
    if (!response.ok) throw new Error(`Failed to get fuel log: ${response.status}`);
    console.log(`   Retrieved fuel log: ${response.data.gallons} gallons`);

    // Get fuel logs for vehicle
    response = await apiCall('GET', `/fuellogs/vehicle/${testVehicleId}`);
    if (!response.ok) throw new Error(`Failed to get vehicle fuel logs: ${response.status}`);
    console.log(`   Retrieved ${response.data.length} fuel logs for vehicle`);
}

async function testFuelAnalytics() {
    if (!testVehicleId) throw new Error('Vehicle ID not available for fuel analytics test');

    // Get fuel efficiency statistics
    let response = await apiCall('GET', `/fuellogs/vehicle/${testVehicleId}/statistics`);
    if (!response.ok) throw new Error(`Failed to get fuel statistics: ${response.status}`);
    console.log(`   Fuel statistics retrieved`);

    // Get fleet fuel statistics
    response = await apiCall('GET', '/fuellogs/statistics');
    if (!response.ok) throw new Error(`Failed to get fleet fuel statistics: ${response.status}`);
    console.log(`   Fleet fuel statistics retrieved`);
}

async function testMaintenanceCRUD() {
    if (!testVehicleId) throw new Error('Vehicle ID not available for maintenance test');

    // Create maintenance record
    const maintenanceData = { ...testMaintenance, vehicleId: testVehicleId };
    let response = await apiCall('POST', '/maintenance', maintenanceData);
    if (!response.ok) throw new Error(`Failed to create maintenance: ${response.status} - ${JSON.stringify(response.data)}`);
    testMaintenanceId = response.data.id;
    console.log(`   Created maintenance ID: ${testMaintenanceId}`);

    // Get maintenance record
    response = await apiCall('GET', `/maintenance/${testMaintenanceId}`);
    if (!response.ok) throw new Error(`Failed to get maintenance: ${response.status}`);
    console.log(`   Retrieved maintenance: ${response.data.maintenanceType}`);

    // Get upcoming maintenance
    response = await apiCall('GET', '/maintenance/upcoming');
    if (!response.ok) throw new Error(`Failed to get upcoming maintenance: ${response.status}`);
    console.log(`   Retrieved ${response.data.length} upcoming maintenance items`);
}

async function testFinancialsCRUD() {
    if (!testVehicleId) throw new Error('Vehicle ID not available for financials test');

    // Create financial record
    const financialData = { ...testFinancial, vehicleId: testVehicleId };
    let response = await apiCall('POST', '/financials', financialData);
    if (!response.ok) throw new Error(`Failed to create financial: ${response.status} - ${JSON.stringify(response.data)}`);
    testFinancialId = response.data.id;
    console.log(`   Created financial ID: ${testFinancialId}`);

    // Get financial record
    response = await apiCall('GET', `/financials/${testFinancialId}`);
    if (!response.ok) throw new Error(`Failed to get financial: ${response.status}`);
    console.log(`   Retrieved financial: ${response.data.recordType}`);

    // Get financial statistics
    response = await apiCall('GET', '/financials/statistics');
    if (!response.ok) throw new Error(`Failed to get financial statistics: ${response.status}`);
    console.log(`   Financial statistics retrieved`);
}

async function testInsuranceCRUD() {
    if (!testVehicleId) throw new Error('Vehicle ID not available for insurance test');

    // Create insurance policy
    const insuranceData = { ...testInsurance, vehicleId: testVehicleId };
    let response = await apiCall('POST', '/insurance', insuranceData);
    if (!response.ok) throw new Error(`Failed to create insurance: ${response.status} - ${JSON.stringify(response.data)}`);
    testInsuranceId = response.data.id;
    console.log(`   Created insurance ID: ${testInsuranceId}`);

    // Get insurance policy
    response = await apiCall('GET', `/insurance/${testInsuranceId}`);
    if (!response.ok) throw new Error(`Failed to get insurance: ${response.status}`);
    console.log(`   Retrieved insurance: ${response.data.provider}`);

    // Get expiring policies
    response = await apiCall('GET', '/insurance/expiring');
    if (!response.ok) throw new Error(`Failed to get expiring insurance: ${response.status}`);
    console.log(`   Retrieved ${response.data.length} expiring policies`);
}

// Cleanup function
async function cleanupTestData() {
    console.log('\n🧹 Cleaning up test data...');
    
    const cleanup = [
        { id: testInsuranceId, endpoint: '/insurance' },
        { id: testFinancialId, endpoint: '/financials' },
        { id: testMaintenanceId, endpoint: '/maintenance' },
        { id: testFuelLogId, endpoint: '/fuellogs' },
        { id: testVehicleId, endpoint: '/vehicles' }
    ];

    for (const item of cleanup) {
        if (item.id) {
            try {
                const response = await apiCall('DELETE', `${item.endpoint}/${item.id}`);
                if (response.ok) {
                    console.log(`   ✅ Deleted ${item.endpoint}/${item.id}`);
                } else {
                    console.log(`   ⚠️  Failed to delete ${item.endpoint}/${item.id}: ${response.status}`);
                }
            } catch (error) {
                console.log(`   ⚠️  Error deleting ${item.endpoint}/${item.id}: ${error.message}`);
            }
        }
    }
}

// Main test runner
async function runAllTests() {
    console.log('🚀 Starting Fleet Management API Integration Tests');
    console.log(`📍 Testing API at: ${BASE_URL}`);

    const tests = [
        ['Vehicle CRUD Operations', testVehiclesCRUD],
        ['Vehicle Analytics', testVehicleAnalytics],
        ['Fuel Logs CRUD Operations', testFuelLogsCRUD],
        ['Fuel Analytics', testFuelAnalytics],
        ['Maintenance CRUD Operations', testMaintenanceCRUD],
        ['Financial CRUD Operations', testFinancialsCRUD],
        ['Insurance CRUD Operations', testInsuranceCRUD]
    ];

    let passed = 0;
    let failed = 0;

    for (const [name, testFunction] of tests) {
        const result = await runTest(name, testFunction);
        if (result) {
            passed++;
        } else {
            failed++;
        }
    }

    // Cleanup regardless of test results
    await cleanupTestData();

    console.log('\n📊 Test Results Summary:');
    console.log(`✅ Passed: ${passed}`);
    console.log(`❌ Failed: ${failed}`);
    console.log(`📈 Success Rate: ${((passed / (passed + failed)) * 100).toFixed(1)}%`);

    if (failed === 0) {
        console.log('\n🎉 All tests passed! API is working correctly.');
    } else {
        console.log('\n⚠️  Some tests failed. Check the logs above for details.');
        process.exit(1);
    }
}

// Run the tests
runAllTests().catch(error => {
    console.error('💥 Test runner crashed:', error);
    process.exit(1);
});
