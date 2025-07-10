/**
 * Simple API connectivity test
 */

const API_BASE = 'http://localhost:5074/api';

async function testConnection() {
    console.log('🧪 Testing API connection...');
    
    try {
        console.log('📡 Making request to:', `${API_BASE}/vehicles`);
        
        const response = await fetch(`${API_BASE}/vehicles`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            }
        });
        
        console.log('📊 Response status:', response.status);
        console.log('📊 Response headers:', [...response.headers.entries()]);
        
        if (response.ok) {
            const data = await response.json();
            console.log('✅ API is responding. Data:', data);
            return true;
        } else {
            console.log('❌ API returned error status:', response.status);
            const errorText = await response.text();
            console.log('❌ Error response:', errorText);
            return false;
        }
    } catch (error) {
        console.log('❌ Connection failed:', error.message);
        console.log('❌ Full error:', error);
        return false;
    }
}

async function testVehicleCreate() {
    console.log('🧪 Testing vehicle creation...');
    
    const testVehicle = {
        name: "Test Vehicle",
        vin: "1HGBH41JXMN109186",
        make: "Toyota",
        model: "Camry",
        year: 2023,
        licensePlate: "TEST123",
        odometerReading: 50000,
        status: 0, // Active = 0
        acquisitionDate: "2023-01-01T00:00:00Z"
    };
    
    try {
        console.log('📡 Creating vehicle:', testVehicle);
        
        const response = await fetch(`${API_BASE}/vehicles`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify(testVehicle)
        });
        
        console.log('📊 Create response status:', response.status);
        
        if (response.ok) {
            const data = await response.json();
            console.log('✅ Vehicle created successfully:', data);
            return data;
        } else {
            console.log('❌ Failed to create vehicle. Status:', response.status);
            const errorText = await response.text();
            console.log('❌ Error response:', errorText);
            return null;
        }
    } catch (error) {
        console.log('❌ Vehicle creation failed:', error.message);
        console.log('❌ Full error:', error);
        return null;
    }
}

async function runTests() {
    console.log('🚀 Starting simple API tests\n');
    
    // Test basic connectivity
    const connectionOk = await testConnection();
    console.log('');
    
    if (connectionOk) {
        // Test vehicle creation
        const vehicle = await testVehicleCreate();
        console.log('');
        
        if (vehicle) {
            console.log('🎉 All tests passed!');
        } else {
            console.log('⚠️  Connection works but creation failed');
        }
    } else {
        console.log('❌ Basic connectivity failed');
    }
}

runTests().catch(console.error);
