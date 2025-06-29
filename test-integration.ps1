# Test script for the refactored Fleet Assistant application
# This script tests the integration between the Next.js frontend and Azure Functions backend

Write-Host "🚀 Fleet Assistant Integration Test" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

# Test 1: Check if Azure Functions is running
Write-Host "`n📡 Testing Azure Functions endpoint..." -ForegroundColor Yellow

$azureFunctionsUrl = "http://localhost:7071/api/chat"
$testPayload = @{
    messages = @(
        @{
            role = "user"
            content = "Tell me about fleet maintenance schedules"
        }
    )
} | ConvertTo-Json -Depth 3

try {
    $response = Invoke-RestMethod -Uri $azureFunctionsUrl -Method Post -Body $testPayload -ContentType "application/json" -TimeoutSec 30
    Write-Host "✅ Azure Functions endpoint is working!" -ForegroundColor Green
    Write-Host "Response: $($response.Substring(0, [Math]::Min(100, $response.Length)))..." -ForegroundColor Gray
} catch {
    Write-Host "❌ Azure Functions endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure Azure Functions is running on port 7071" -ForegroundColor Yellow
}

# Test 2: Check if frontend is running
Write-Host "`n🌐 Testing frontend availability..." -ForegroundColor Yellow

$frontendUrl = "http://localhost:3000"
try {
    $frontendResponse = Invoke-WebRequest -Uri $frontendUrl -TimeoutSec 10
    if ($frontendResponse.StatusCode -eq 200) {
        Write-Host "✅ Frontend is running on port 3000!" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Frontend not available: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure the Next.js app is running on port 3000" -ForegroundColor Yellow
}

# Test 3: CORS check
Write-Host "`n🔗 Testing CORS configuration..." -ForegroundColor Yellow

try {
    $corsResponse = Invoke-WebRequest -Uri $azureFunctionsUrl -Method Options -TimeoutSec 10
    $corsHeaders = $corsResponse.Headers
    
    if ($corsHeaders.ContainsKey("Access-Control-Allow-Origin")) {
        Write-Host "✅ CORS headers are configured!" -ForegroundColor Green
    } else {
        Write-Host "⚠️  CORS headers might not be properly configured" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ CORS test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n📋 Test Summary:" -ForegroundColor Cyan
Write-Host "- Azure Functions should be running: func start (in src/backend/FleetAssistant.Api)" -ForegroundColor Gray
Write-Host "- Frontend should be running: npm run dev (in src/frontend/ai-chatbot)" -ForegroundColor Gray
Write-Host "- Open http://localhost:3000 to test the chat interface" -ForegroundColor Gray

Write-Host "`n🎯 Manual Test Steps:" -ForegroundColor Cyan
Write-Host "1. Open browser to http://localhost:3000" -ForegroundColor Gray
Write-Host "2. Type a message like 'Help with fleet maintenance'" -ForegroundColor Gray
Write-Host "3. Verify you see a streaming response" -ForegroundColor Gray
Write-Host "4. Check browser developer tools for any CORS errors" -ForegroundColor Gray
