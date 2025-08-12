Install Required Packages

dotnet add package Serilog.Sinks.MSSqlServer
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Enrichers.Process
dotnet add package Serilog.Enrichers.Thread
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.AspNetCore

CREATE TABLE [dbo].[Logs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Message] [nvarchar](max) NULL,
	[MessageTemplate] [nvarchar](max) NULL,
	[Level] [nvarchar](max) NULL,
	[TimeStamp] [datetime] NULL,
	[Exception] [nvarchar](max) NULL,
	[Properties] [nvarchar](max) NULL,
	[CorrelationId] [nvarchar](256) NULL,
	[RequestId] [nvarchar](256) NULL,
	[UserId] [nvarchar](256) NULL,
	[ServiceName] [nvarchar](256) NULL,
	[Environment] [nvarchar](256) NULL,
	[RequestPath] [nvarchar](500) NULL,
	[ClientIP] [nvarchar](256) NULL,
	[UserAgent] [nvarchar](256) NULL,
	[OperationName] [nvarchar](256) NULL,
	[ExecutionTimeMs] [int] NULL,
 CONSTRAINT [PK_Logs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO




---

### **Core Log Metadata**

| Field          | Purpose                                                                                                    | Example                                              |
| -------------- | ---------------------------------------------------------------------------------------------------------- | ---------------------------------------------------- |
| **Id**         | Auto-increment primary key for unique identification of each log entry in the database.                    | `1`, `2`, `3`                                        |
| **Timestamp**  | When the event happened (UTC recommended). Critical for ordering logs and time-based analysis.             | `2025-08-11 14:22:05`                                |
| **Level**      | Severity of the log message (e.g., `Information`, `Warning`, `Error`, `Fatal`). Helps filter logs quickly. | `Error`                                              |
| **Message**    | Human-readable description of what happened. Should be concise but descriptive.                            | `"Checkout process started"`                         |
| **Exception**  | Stack trace or exception message if an error occurred. Useful for debugging failures.                      | `System.NullReferenceException: Object reference...` |
| **Properties** | JSON blob storing extra structured log data from Serilog (anything not explicitly mapped to a column).     | `{"OrderId":123, "Amount": 99.99}`                   |

---

### **Contextual / Correlation Fields** *(Highly Recommended for Distributed Systems)*

| Field             | Purpose                                                                                                                    | Example                                |
| ----------------- | -------------------------------------------------------------------------------------------------------------------------- | -------------------------------------- |
| **CorrelationId** | A unique identifier across multiple services for a single logical transaction. Links logs together for end-to-end tracing. | `9f3a8f02-a61f-4c7b-98e0-9f6d4a8b0e7f` |
| **RequestId**     | Unique per HTTP request within a single service (helps when the same CorrelationId is reused across multiple requests).    | `REQ-20250811-00023`                   |
| **UserId**        | The authenticated user or account that triggered the action.                                                               | `"user123"`                            |
| **ServiceName**   | Name of the microservice or application writing the log (critical in multi-service environments).                          | `"OrderService"`                       |
| **Environment**   | Which deployment environment produced the log. Helps distinguish between Dev, Staging, and Production logs.                | `"Production"`                         |

---

### **Request/Operation Context**

| Field               | Purpose                                                                                   | Example                |
| ------------------- | ----------------------------------------------------------------------------------------- | ---------------------- |
| **RequestPath**     | API route or endpoint requested — essential for tracing API issues.                       | `/api/orders/checkout` |
| **ClientIP**        | The IP address of the request origin (helps detect abuse, debugging client connectivity). | `192.168.1.42`         |
| **UserAgent**       | Browser, app, or client making the request.                                               | `Chrome 126`           |
| **OperationName**   | Logical operation or business action performed.                                           | `"CreateOrder"`        |
| **ExecutionTimeMs** | How long the operation took in milliseconds — used for performance tracking.              | `153`                  |

---

### **Why These Matter in Practice**

* **Troubleshooting** → CorrelationId + RequestId let you track an error through multiple services and requests.
* **Security Auditing** → UserId, ClientIP, and UserAgent tell you *who* did *what* from *where*.
* **Performance Tuning** → ExecutionTimeMs reveals slow endpoints or methods.
* **Multi-Environment Safety** → Environment ensures logs aren’t confused between staging and production.
* **Business Tracking** → OperationName and ServiceName give clear business context.

---