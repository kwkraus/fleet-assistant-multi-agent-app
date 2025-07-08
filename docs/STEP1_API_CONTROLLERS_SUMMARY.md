# Fleet Assistant API - Step 1: Controllers and Endpoints

## Overview
This document outlines the comprehensive RESTful API controllers and endpoints that have been implemented for the Fleet Assistant application. The API provides full CRUD operations for managing fleet vehicles and their associated data across multiple domains.

## Architecture

### Project Structure
```
FleetAssistant.Shared/
├── Models/           # Entity models for all domains
├── DTOs/            # Data Transfer Objects for API requests/responses
└── Services/        # Shared service interfaces

FleetAssistant.WebApi/
├── Controllers/     # API controllers for each domain
├── Options/         # Configuration options
└── Services/        # Service implementations
```

### Technology Stack
- **Framework**: ASP.NET Core 8.0 Web API
- **Language**: C# with nullable reference types enabled
- **Documentation**: Swagger/OpenAPI with XML comments
- **Architecture**: Clean Architecture with DTOs and domain separation

## Domain Models

### 1. Vehicle Information
**Model**: `Vehicle`
- Core vehicle data (ID, Name, VIN, Make, Model, Year)
- Operational data (Odometer, Status, License Plate, Color)
- Acquisition and metadata (Acquisition Date, Details, Created/Updated timestamps)

### 2. Fuel Logs
**Model**: `FuelLog`
- Fuel purchase tracking (Date, Gallons, Price, Total Cost)
- Efficiency metrics (MPG calculation, Miles driven)
- Location and fuel type information

### 3. Maintenance Records
**Model**: `MaintenanceRecord`
- Maintenance tracking (Type, Date, Description, Cost)
- Service provider information
- Warranty and scheduling (Next maintenance date/odometer)

### 4. Insurance Information
**Models**: `InsurancePolicy`, `VehicleInsurance`
- Policy details (Number, Provider, Coverage, Premium)
- Vehicle coverage relationships (many-to-many)
- Renewal and payment tracking

### 5. Financial Records
**Model**: `VehicleFinancial`
- Financial obligations (Lease, Loan, Registration, etc.)
- Payment schedules and amounts
- Depreciation tracking and calculations

## API Controllers

### 1. VehiclesController (`/api/vehicles`)
**Core Operations**:
- `GET /api/vehicles` - List all vehicles with filtering
- `GET /api/vehicles/{id}` - Get specific vehicle
- `POST /api/vehicles` - Create new vehicle
- `PUT /api/vehicles/{id}` - Update vehicle
- `DELETE /api/vehicles/{id}` - Delete vehicle

**Additional Endpoints**:
- `GET /api/vehicles/statistics` - Vehicle statistics

**Filtering Options**:
- Status, Make, Model, Year
- Pagination support (page, pageSize)

### 2. FuelLogsController (`/api/fuellogs`)
**Core Operations**:
- `GET /api/fuellogs` - List all fuel logs with filtering
- `GET /api/fuellogs/{id}` - Get specific fuel log
- `POST /api/fuellogs` - Create new fuel log
- `PUT /api/fuellogs/{id}` - Update fuel log
- `DELETE /api/fuellogs/{id}` - Delete fuel log

**Vehicle-Specific Endpoints**:
- `GET /api/fuellogs/vehicle/{vehicleId}` - Get fuel logs for vehicle
- `GET /api/fuellogs/vehicle/{vehicleId}/statistics` - Fuel efficiency statistics

**Filtering Options**:
- Vehicle ID, Date range, Fuel type
- Pagination support

### 3. MaintenanceController (`/api/maintenance`)
**Core Operations**:
- `GET /api/maintenance` - List all maintenance records
- `GET /api/maintenance/{id}` - Get specific maintenance record
- `POST /api/maintenance` - Create new maintenance record
- `PUT /api/maintenance/{id}` - Update maintenance record
- `DELETE /api/maintenance/{id}` - Delete maintenance record

**Specialized Endpoints**:
- `GET /api/maintenance/vehicle/{vehicleId}` - Get maintenance for vehicle
- `GET /api/maintenance/upcoming` - Get upcoming maintenance
- `GET /api/maintenance/vehicle/{vehicleId}/statistics` - Maintenance statistics

**Filtering Options**:
- Vehicle ID, Maintenance type, Date range, Service provider
- Pagination support

### 4. InsuranceController (`/api/insurance`)
**Core Operations**:
- `GET /api/insurance` - List all insurance policies
- `GET /api/insurance/{id}` - Get specific insurance policy
- `POST /api/insurance` - Create new insurance policy
- `PUT /api/insurance/{id}` - Update insurance policy
- `DELETE /api/insurance/{id}` - Delete insurance policy

**Vehicle Management**:
- `POST /api/insurance/{id}/vehicles` - Add vehicle to policy
- `DELETE /api/insurance/{id}/vehicles/{vehicleId}` - Remove vehicle from policy
- `GET /api/insurance/vehicle/{vehicleId}` - Get policies for vehicle

**Additional Endpoints**:
- `GET /api/insurance/statistics` - Insurance statistics

**Filtering Options**:
- Provider name, Coverage type, Active status, Expiring policies

### 5. FinancialsController (`/api/financials`)
**Core Operations**:
- `GET /api/financials` - List all financial records
- `GET /api/financials/{id}` - Get specific financial record
- `POST /api/financials` - Create new financial record
- `PUT /api/financials/{id}` - Update financial record
- `DELETE /api/financials/{id}` - Delete financial record

**Vehicle-Specific Endpoints**:
- `GET /api/financials/vehicle/{vehicleId}` - Get financial records for vehicle
- `GET /api/financials/vehicle/{vehicleId}/statistics` - Financial statistics
- `GET /api/financials/vehicle/{vehicleId}/depreciation` - Depreciation calculation

**Fleet Management**:
- `GET /api/financials/upcoming-payments` - Upcoming payments
- `GET /api/financials/fleet-summary` - Fleet-wide financial summary

**Filtering Options**:
- Vehicle ID, Financial type, Provider, Date range

## Data Transfer Objects (DTOs)

### Request DTOs
- `Create*Dto` - For creating new records (required fields only)
- `Update*Dto` - For updating existing records (optional fields)
- `Manage*Dto` - For specific operations (e.g., vehicle insurance management)

### Response DTOs
- `*Dto` - For API responses with full entity data
- Include related data (e.g., vehicle name in fuel logs)
- Consistent timestamp fields (CreatedAt, UpdatedAt)

## Common Features

### Error Handling
- Consistent error response format
- Proper HTTP status codes
- Detailed error messages with context

### Validation
- Data annotations for input validation
- Business rule validation (e.g., VIN format, date ranges)
- Model state validation in controllers

### Pagination
- Consistent pagination across all list endpoints
- Configurable page size with maximum limits
- Page and pageSize query parameters

### Filtering
- Rich filtering options for each domain
- Date range filtering where applicable
- Enumeration-based filtering (status, types, etc.)

### Logging
- Comprehensive logging throughout all controllers
- Structured logging with correlation IDs
- Error logging with context information

### Documentation
- Complete XML documentation for all endpoints
- Swagger/OpenAPI integration
- Response type documentation

## Testing

### HTTP Test File
A comprehensive test file (`fleet-api-test.http`) has been created with:
- Sample requests for all endpoints
- Various filtering scenarios
- CRUD operation examples
- Error condition testing

### Example Usage
```http
### Create a new vehicle
POST {{baseUrl}}/vehicles
Content-Type: application/json

{
  "name": "Fleet Vehicle 001",
  "vin": "1HGBH41JXMN109186",
  "make": "Honda",
  "model": "Civic",
  "year": 2023,
  "odometerReading": 15000,
  "status": "Active"
}

### Get vehicle statistics
GET {{baseUrl}}/vehicles/statistics

### Filter vehicles by status
GET {{baseUrl}}/vehicles?status=Active&page=1&pageSize=10
```

## Current Status

✅ **Completed in Step 1**:
- Complete domain models for all 5 data domains
- Comprehensive DTOs for all operations
- Full CRUD controllers with rich filtering
- Proper error handling and validation
- Extensive API documentation
- HTTP testing file with sample requests

⏳ **Next Steps (Step 2)**:
- Entity Framework Core integration
- Azure SQL Database connection
- Database migrations and seeding
- Actual data persistence implementation

🔄 **Future Steps (Step 3)**:
- Azure Blob Storage integration for document uploads
- File upload endpoints for insurance and financial documents
- Document management and retrieval

## Notes

- All controller methods are currently implemented with placeholder responses
- The actual data access layer will be implemented in Step 2 with Entity Framework
- The API structure is designed to be database-agnostic and follows REST conventions
- Authentication and authorization will be added in future iterations
- All endpoints include proper OpenAPI documentation for automatic client generation

The API provides a solid foundation for fleet management with comprehensive coverage of all required domains and operations.
