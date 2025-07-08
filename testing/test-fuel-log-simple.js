/**
 * Test individual fuel log creation
 */

const API_BASE = 'http://localhost:5074/api';

async function testFuelLogCreation() {
    console.log('🧪 Testing fuel log creation...');
    
    // First create a vehicle
    const testVehicle = {
        name: "Test Vehicle for Fuel",
        vin: "2FTFW1ET5DFC56789",
        make: "Ford",
        model: "F-150",
        year: 2023,
        licensePlate: "FUEL1",
        odometerReading: 15000,
        status: 0, // Active = 0
        acquisitionDate: "2023-01-15T00:00:00Z"
    };
    
    try {
        console.log('📡 Creating vehicle...');
        
        let response = await fetch(`${API_BASE}/vehicles`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify(testVehicle)
        });
        
        if (!response.ok) {
            console.log('❌ Failed to create vehicle:', response.status);
            const errorText = await response.text();
            console.log('❌ Error response:', errorText);
            return;
        }
        
        const vehicle = await response.json();
        console.log('✅ Vehicle created with ID:', vehicle.id);
        
        // Now create fuel log
        const testFuelLog = {
            vehicleId: vehicle.id,
            fuelDate: "2024-01-15T00:00:00Z",
            odometerReading: 16500,
            gallons: 12.5,
            pricePerGallon: 3.45,
            totalCost: 43.13,
            fuelType: "Regular",
            location: "Shell Station Downtown",
            notes: "Regular fill-up"
        };
        
        console.log('📡 Creating fuel log:', testFuelLog);
        
        response = await fetch(`${API_BASE}/fuellogs`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify(testFuelLog)
        });
        
        console.log('📊 Fuel log response status:', response.status);
        
        if (response.ok) {
            const fuelLog = await response.json();
            console.log('✅ Fuel log created successfully:', fuelLog);
        } else {
            console.log('❌ Failed to create fuel log. Status:', response.status);
            const errorText = await response.text();
            console.log('❌ Error response:', errorText);
        }
        
    } catch (error) {
        console.log('❌ Test failed:', error.message);
        console.log('❌ Full error:', error);
    }
}

testFuelLogCreation().catch(console.error);
