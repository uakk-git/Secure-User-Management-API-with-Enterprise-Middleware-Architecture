# Middleware Testing Guide

## Test Scenarios

### 1. Authentication Middleware Testing

#### Test 1.1: Valid Login and Token Generation
**Steps:**
1. Send POST request to `/api/auth/login` with valid username/password
2. Extract the token from the response
3. Use the token in subsequent requests

**Expected Result:**
- Status Code: 200 OK
- Response contains valid JWT token
- Token includes claims (userId, email)

#### Test 1.2: Access Protected Endpoint with Valid Token
**Steps:**
1. Login and get token
2. Make GET request to `/api/users` with `Authorization: Bearer {token}`

**Expected Result:**
- Status Code: 200 OK
- Users list is returned
- Request is logged with authentication details

#### Test 1.3: Access Protected Endpoint Without Token
**Steps:**
1. Make GET request to `/api/users` without Authorization header

**Expected Result:**
- Status Code: 401 Unauthorized
- Error message: "Missing or invalid Authorization header"
- Request is logged as unauthorized

#### Test 1.4: Access Protected Endpoint with Invalid Token
**Steps:**
1. Make GET request with `Authorization: Bearer invalid.token.here`

**Expected Result:**
- Status Code: 401 Unauthorized
- Error message: "Invalid or expired token"
- Request is logged with token validation failure

#### Test 1.5: Access Protected Endpoint with Malformed Header
**Steps:**
1. Make GET request with `Authorization: InvalidScheme token`

**Expected Result:**
- Status Code: 401 Unauthorized
- Error message: "Missing or invalid Authorization header"

#### Test 1.6: Public Endpoint Access (No Auth Required)
**Steps:**
1. Make GET request to `/health` without Authorization header

**Expected Result:**
- Status Code: 200 OK
- Returns health status
- No authentication required

### 2. Logging Middleware Testing

#### Test 2.1: Request Logging
**Steps:**
1. Make any API request
2. Check application logs

**Expected Log Content:**