# Support Tickets API

A secure REST API for managing support tickets with JWT Bearer authentication, designed to integrate seamlessly with Teams bots and other client applications. This API provides CRUD operations for support tickets with file-based storage and comprehensive security features.

## Features

### Authentication & Security
- ✅ **JWT Bearer Token Authentication** - Validates tokens issued by Azure AD
- ✅ **Azure AD Integration** - Uses the same app registration as the Teams bot
- ✅ **CORS Support** - Configured for cross-origin requests from Teams and web clients
- ✅ **Request Validation** - Input validation and sanitization
- ✅ **Error Handling** - Comprehensive error responses with proper HTTP status codes

### API Functionality
- ✅ **Ticket Management** - Create, read, update, and delete support tickets
- ✅ **Status Tracking** - Update ticket status (Open, In Progress, Resolved, Closed)
- ✅ **File Storage** - Persistent JSON-based storage with thread safety
- ✅ **Health Checks** - Built-in health monitoring endpoint
- ✅ **Swagger Documentation** - Interactive API documentation

### Integration Features
- ✅ **Teams Bot Integration** - Designed to work with Teams SSO bots
- ✅ **User Identification** - Supports user-specific ticket creation
- ✅ **Real-time Operations** - Immediate ticket creation and updates
- ✅ **Scalable Architecture** - Ready for production deployment

## Architecture

```
Teams Bot → JWT Client Credentials → Support Tickets API → File Storage
     ↓                                      ↓
Azure AD App Registration              App_Data/tickets.json
```

### Authentication Flow
1. Client (Teams Bot) obtains JWT token from Azure AD using client credentials
2. Client includes Bearer token in Authorization header
3. API validates token with Azure AD
4. If valid, API processes the request
5. API returns response with appropriate HTTP status

## API Endpoints

### Health Check
```
GET /health
```
Returns API health status and basic information.

### Tickets Management

#### Get All Tickets
```
GET /api/tickets
Authorization: Bearer <jwt-token>
```
Returns all support tickets.

#### Get Specific Ticket
```
GET /api/tickets/{id}
Authorization: Bearer <jwt-token>
```
Returns a specific ticket by ID.

#### Create New Ticket
```
POST /api/tickets
Authorization: Bearer <jwt-token>
Content-Type: application/json

{
  "title": "Support Request from John Doe",
  "description": "User needs help with authentication",
  "priority": "Medium",
  "category": "Technical Support"
}
```

#### Update Ticket Status
```
PUT /api/tickets/{id}/status
Authorization: Bearer <jwt-token>
Content-Type: application/json

{
  "status": "In Progress"
}
```

#### Delete Ticket
```
DELETE /api/tickets/{id}
Authorization: Bearer <jwt-token>
```

## Data Models

### Ticket Entity
```csharp
public class TicketEntity
{
    public string Id { get; set; }              // Unique identifier
    public string Title { get; set; }           // Ticket title
    public string Description { get; set; }     // Detailed description
    public string Status { get; set; }          // Current status
    public string Priority { get; set; }        // Priority level
    public string Category { get; set; }        // Ticket category
    public string CreatedBy { get; set; }       // User who created
    public DateTime CreatedAt { get; set; }     // Creation timestamp
    public DateTime UpdatedAt { get; set; }     // Last update timestamp
}
```

### Create Ticket Request
```csharp
public class CreateTicketRequest
{
    public string Title { get; set; }           // Required
    public string Description { get; set; }     // Required
    public string Priority { get; set; }        // Optional (default: "Medium")
    public string Category { get; set; }        // Optional (default: "General")
}
```

### Update Status Request
```csharp
public class UpdateTicketStatusRequest
{
    public string Status { get; set; }          // Required: Open, In Progress, Resolved, Closed
}
```

## Configuration

### Required Settings in `appsettings.json`

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<your-tenant-id>",
    "ClientId": "89155d3a-359d-4603-b821-0504395e331f",
    "Audience": "89155d3a-359d-4603-b821-0504395e331f"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Environment Variables for Production

```bash
AzureAd__TenantId=<your-tenant-id>
AzureAd__ClientId=89155d3a-359d-4603-b821-0504395e331f
AzureAd__Audience=89155d3a-359d-4603-b821-0504395e331f
ASPNETCORE_ENVIRONMENT=Production
```

## Project Structure

```
SupportTicketsApi/
├── Controllers/
│   └── TicketsController.cs      # API endpoints and request handling
├── Models/
│   ├── TicketEntity.cs          # Core ticket data model
│   ├── TicketDto.cs             # Data transfer object
│   ├── CreateTicketRequest.cs   # Request model for ticket creation
│   └── UpdateTicketStatusRequest.cs # Request model for status updates
├── Services/
│   ├── ITicketRepository.cs     # Repository interface
│   ├── TicketRepository.cs      # In-memory repository (unused)
│   └── FileTicketRepository.cs  # File-based repository implementation
├── Mapping/
│   └── TicketMapping.cs         # Entity to DTO mapping
├── App_Data/
│   └── tickets.json             # Persistent ticket storage
├── Program.cs                   # Application startup and configuration
├── appsettings.json            # Configuration settings
└── SupportTicketsApi.http      # HTTP test requests
```

## Key Components

### TicketsController.cs - API Endpoints

**Purpose**: Handles HTTP requests and provides RESTful endpoints for ticket operations.

**Key Features**:
- JWT authentication requirement for all endpoints
- Input validation with model binding
- Proper HTTP status codes and error handling
- Swagger documentation attributes

**Dependencies**: `ITicketRepository` for data operations

### FileTicketRepository.cs - Data Storage

**Purpose**: Manages persistent storage of tickets using JSON file system.

**Key Features**:
- Thread-safe file operations with locking
- Automatic ID generation for new tickets
- JSON serialization/deserialization
- Directory creation if not exists
- Error handling for file operations

**Storage Location**: `App_Data/tickets.json`

### Program.cs - Application Configuration

**Purpose**: Configures the web application with authentication, CORS, and services.

**Key Configurations**:
- JWT Bearer authentication with Azure AD
- CORS policy for cross-origin requests
- Swagger/OpenAPI documentation
- Health checks endpoint
- Service registration and dependency injection

## Local Development

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 or VS Code
- Azure AD app registration

### Setup Steps

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd SupportTicketsApi
   ```

2. **Configure settings:**
   Update `appsettings.Development.json` with your Azure AD details.

3. **Build and run:**
   ```bash
   dotnet build
   dotnet run
   ```

4. **Test the API:**
   Navigate to `https://localhost:7092/swagger` for interactive documentation.

### Testing Authentication

Use the provided HTTP test file (`SupportTicketsApi.http`) with your favorite HTTP client:

```http
### Get Health Check
GET https://localhost:7092/health

### Get Access Token (for testing)
POST https://login.microsoftonline.com/{{tenantId}}/oauth2/v2.0/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id={{clientId}}
&client_secret={{clientSecret}}
&scope={{clientId}}/.default

### Get All Tickets
GET https://localhost:7092/api/tickets
Authorization: Bearer {{accessToken}}
```

## Deployment to Azure

### Azure App Service Deployment

1. **Create Azure App Service:**
   ```bash
   az group create --name rg-support-tickets --location eastus
   az appservice plan create --name plan-support-tickets --resource-group rg-support-tickets --sku B1
   az webapp create --name your-tickets-api --resource-group rg-support-tickets --plan plan-support-tickets
   ```

2. **Configure App Settings:**
   ```bash
   az webapp config appsettings set --name your-tickets-api --resource-group rg-support-tickets --settings \
     AzureAd__TenantId=<your-tenant-id> \
     AzureAd__ClientId=89155d3a-359d-4603-b821-0504395e331f \
     AzureAd__Audience=89155d3a-359d-4603-b821-0504395e331f
   ```

3. **Deploy Application:**
   ```bash
   dotnet publish -c Release
   az webapp deployment source config-zip --name your-tickets-api --resource-group rg-support-tickets --src publish.zip
   ```

### Post-Deployment Verification

1. **Health Check:**
   ```bash
   curl https://your-tickets-api.azurewebsites.net/health
   ```

2. **Swagger Documentation:**
   Navigate to `https://your-tickets-api.azurewebsites.net/swagger`

3. **API Test:**
   Use the HTTP test file with production URLs.

## Security Considerations

### Authentication Security
- **Token Validation**: All API calls require valid JWT Bearer tokens
- **Azure AD Integration**: Tokens validated against Azure AD authority
- **Scope Validation**: API accepts tokens with appropriate audience
- **HTTPS Enforcement**: All production traffic uses HTTPS

### Data Security
- **Input Validation**: All inputs validated and sanitized
- **Error Handling**: No sensitive information leaked in error responses
- **File Permissions**: Storage files have appropriate access restrictions
- **Thread Safety**: Concurrent access to storage is properly managed

### Production Recommendations
- **Use Azure Key Vault** for storing sensitive configuration
- **Implement Rate Limiting** to prevent abuse
- **Enable Application Insights** for monitoring and logging
- **Configure Custom Domains** with proper SSL certificates
- **Use Azure Storage** for scalable file storage instead of local files

## Monitoring and Logging

### Health Checks
The API includes a health check endpoint at `/health` that returns:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "self": {
      "status": "Healthy"
    }
  }
}
```

### Application Insights Integration

Add Application Insights for production monitoring:

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Custom Logging

The application uses structured logging:
```csharp
_logger.LogInformation("Creating new ticket with title: {Title}", request.Title);
_logger.LogWarning("Ticket not found: {TicketId}", id);
_logger.LogError(ex, "Error creating ticket");
```

## Integration with Teams Bot

### Bot Configuration

Configure the Teams bot to use this API:

```json
{
  "SupportTicketsApiBaseUrl": "https://your-tickets-api.azurewebsites.net"
}
```

### Authentication Flow

1. Teams bot obtains JWT token using client credentials:
   ```csharp
   var app = ConfidentialClientApplicationBuilder
       .Create(clientId)
       .WithClientSecret(clientSecret)
       .WithAuthority(authority)
       .Build();
       
   var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
   ```

2. Bot includes token in API requests:
   ```csharp
   httpClient.DefaultRequestHeaders.Authorization = 
       new AuthenticationHeaderValue("Bearer", accessToken);
   ```

### Error Handling

The bot should handle API errors gracefully:
- **401 Unauthorized**: Token expired or invalid - refresh token
- **404 Not Found**: Ticket doesn't exist - inform user
- **500 Internal Server Error**: API issue - retry or inform user

## Troubleshooting

### Common Issues

1. **401 Unauthorized Errors**
   - Verify JWT token is valid and not expired
   - Check Azure AD app registration configuration
   - Ensure correct audience and scopes

2. **CORS Errors**
   - Verify CORS policy includes your client origin
   - Check that preflight requests are handled correctly
   - Ensure credentials are included if required

3. **File Storage Issues**
   - Check App_Data directory permissions
   - Verify disk space availability
   - Look for file locking issues in logs

4. **Startup Errors**
   - Verify all required configuration settings
   - Check Azure AD tenant and client ID values
   - Ensure .NET 9.0 runtime is available

### Debug Tools

- **Swagger UI**: Test API endpoints directly
- **Application Insights**: Monitor production issues
- **Azure Portal**: Check App Service logs and metrics
- **Postman/HTTP files**: Test authentication flows

### Performance Considerations

#### File-Based Storage Limitations
- **Concurrent Access**: File locking may cause delays under high load
- **Scalability**: Single file storage doesn't scale horizontally
- **Backup**: Manual backup procedures required

#### Production Recommendations
- **Azure SQL Database**: For higher performance and scalability
- **Azure Cosmos DB**: For global distribution
- **Azure Table Storage**: For simple key-value operations
- **Redis Cache**: For frequently accessed data

## Future Enhancements

### Potential Improvements
- [ ] Implement paging for large ticket lists
- [ ] Add ticket assignment and user management
- [ ] Include file attachment support
- [ ] Add email notifications for ticket updates
- [ ] Implement ticket categories and priorities
- [ ] Add audit logging for all operations
- [ ] Include search and filtering capabilities
- [ ] Add bulk operations support

### Database Migration
To upgrade from file storage to Azure SQL:

1. **Create Database Schema:**
   ```sql
   CREATE TABLE Tickets (
       Id NVARCHAR(50) PRIMARY KEY,
       Title NVARCHAR(200) NOT NULL,
       Description NVARCHAR(MAX),
       Status NVARCHAR(50),
       Priority NVARCHAR(50),
       Category NVARCHAR(100),
       CreatedBy NVARCHAR(100),
       CreatedAt DATETIME2,
       UpdatedAt DATETIME2
   );
   ```

2. **Update Repository Implementation:**
   Replace `FileTicketRepository` with `SqlTicketRepository`

3. **Migrate Data:**
   Export existing JSON data and import to SQL database

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/new-feature`)
3. Commit your changes (`git commit -am 'Add new feature'`)
4. Push to the branch (`git push origin feature/new-feature`)
5. Create a Pull Request

### Development Guidelines
- Follow RESTful API conventions
- Include comprehensive error handling
- Add unit tests for new functionality
- Update documentation for API changes
- Maintain backward compatibility where possible

## License

This project is licensed under the MIT License - see the LICENSE file for details.

---

**Note**: This API is designed for production use with proper security, monitoring, and scalability considerations.