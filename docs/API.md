# DotnetVoyager API Documentation

This document describes all available endpoints for the DotnetVoyager web platform.

## 1\. Upload Assembly

Uploads one or more .dll or .exe files for analysis. This is an asynchronous endpoint. It creates a job and returns an ID to track its status.

### `POST /api/analysis/upload`

**Consumes:** `multipart/form-data`
**Request Body:** `IFormFile` named "File"

-----

#### ✅ Success Response (Status 202 Accepted)

Indicates the file was accepted and queued for analysis.

```json
{
  "analysisId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

-----

#### ❌ Error Response (Status 400 Bad Request)

Returned when validation fails (e.g., file too large, missing, or wrong type).

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "File": [
      "The file is required.",
      "File size exceeds the 3 MB limit."
    ]
  }
}
```

## 2\. Get Analysis Status

Polls for the current status of a background analysis job.

### `GET /api/analysis/{analysisId\}/status`

**Parameters:**

  * `analysisId` (string, required): The ID returned from the `/upload` endpoint.

-----

#### ✅ Success Response (Status 200 OK)

Returns the current state of the job. The `status` field will progress from `Pending` -\> `Processing` -\> `Completed` or `Failed`.

```json
{
  "analysisId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "Processing",
  "errorMessage": null,
  "zipStatus": "NotStarted",
  "zipErrorMessage": null,
  "lastUpdatedUtc": "2025-11-13T21:30:00Z"
}
```

-----

#### ❌ Error Response (Status 404 Not Found)

Returned if the specified `analysisId` does not exist.

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Analysis job with ID a1b2c3d4-.... not found."
}
```

## 3\. Get Inheritance Graph

Fetches the complete inheritance and implementation graph for the assembly. This endpoint should only be called after the `/status` endpoint returns `Completed`.

### `GET /api/analysis/{analysisId}/inheritance-graph`

**Parameters:**

  * `analysisId` (string, required): The ID of the completed job.

-----

#### ✅ Success Response (Status 200 OK)

Returns the full list of nodes (types) and edges (relationships).

```json
{
  "nodes": [
    {
      "id": "11223344",
      "tokenId": 11223344,
      "fullName": "DotnetVoyager.BLL.Services.MyAwesomeClass",
      "label": "MyAwesomeClass",
      "type": "Class",
      "isExternal": false
    },
    {
      "id": "11223355",
      "tokenId": 11223355,
      "fullName": "DotnetVoyager.BLL.Services.IBaseInterface",
      "label": "IBaseInterface",
      "type": "Interface",
      "isExternal": false
    },
    {
      "id": "System.Object",
      "tokenId": 0,
      "fullName": "System.Object",
      "label": "Object",
      "type": "Class",
      "isExternal": true
    }
  ],
  "edges": [
    {
      "id": "11223344_implements_11223355",
      "source": "11223344",
      "target": "11223355"
    },
    {
      "id": "11223344_inherits_System.Object",
      "source": "11223344",
      "target": "System.Object"
    }
  ]
}
```

-----

#### ❌ Error Response (Status 409 Conflict)

Returned if the analysis is not yet `Completed`.

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "Conflict",
  "status": 409,
  "detail": "Analysis is not completed. Current status: Processing"
}
```

## 4\. Get Decompiled Code

Fetches the decompiled C\# and IL code for a specific type or member using its metadata token.

### `GET /api/analysis/{analysisId}/decompile/{lookupToken}`

**Parameters:**

  * `analysisId` (string, required): The ID of the completed job.
  * `lookupToken` (int, required): The metadata token from the `structure` tree.

-----

#### ✅ Success Response (Status 200 OK)

Returns the decompiled C\# and corresponding IL code.

```json
{
  "csharpCode": "public class MyAwesomeClass : IBaseInterface\n{\n    // ... decompiled C# code ...\n}\n",
  "ilCode": ".class public auto ansi beforefieldinit MyAwesomeClass\n    extends [System.Runtime]System.Object\n    implements DotnetVoyager.BLL.Services.IBaseInterface\n{\n    // ... IL code ...\n}\n"
}
```

-----

#### ❌ Error Response (Status 404 Not Found)

Returned if the assembly file for the `analysisId` is missing (e.g., expired and cleaned up) or if the `lookupToken` is invalid.

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Assembly file not found for Analysis ID: a1b2c3d4-...."
}
```