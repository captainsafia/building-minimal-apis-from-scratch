# Clickstop 1-5: Routing and Middleware

GET http://localhost:8080/
Accept: text/plain
HTTP 200

GET http://localhost:8080/nonexistent
Accept: text/plain
HTTP 404

GET http://localhost:8080/bye
Accept: text/plain
HTTP 200

###############################################################################
# Clickstop 6: Parameter Binding Tests
# Test query string parameter binding

GET http://localhost:8080/hello?name=Safia
Accept: text/plain
HTTP 200

# Test with different parameter values

GET http://localhost:8080/age?year=1996
Accept: text/plain
HTTP 200

###############################################################################
# Clickstop 8: HTTP Method Routing Tests
# Test GET endpoint

GET http://localhost:8080/hello?name=Safia
Accept: text/plain
HTTP 200

# Test POST endpoint

POST http://localhost:8080/post?id=123
Content-Length: 0
Accept: text/plain
HTTP 201

# Test invalid HTTP method endpoint

GET http://localhost:8080/post?id=123
Accept: text/plain
HTTP 405


###############################################################################
# Clickstop 14: JSON Body Binding Tests
# Test POST with JSON body

POST http://localhost:8080/post?id=200
Content-Type: application/json
{
  "title": "Buy groceries",
  "isCompleted": false
}
HTTP 200

# Test POST with JSON body - completed todo

POST http://localhost:8080/post?id=201
Content-Type: application/json
{
  "title": "Finish project",
  "isCompleted": true
}
HTTP 200

# Test POST with complex JSON

POST http://localhost:8080/post?id=202
Content-Type: application/json
{
  "title": "Learn ASP.NET Core internals",
  "isCompleted": false
}
HTTP 200

###############################################################################
# Clickstop 15-16: Metadata and Authorization Tests
# Test public endpoint (should work)

GET http://localhost:8080/public
Accept: text/plain
HTTP 200

# Test protected endpoint without auth (should return 401)

GET http://localhost:8080/protected
Accept: text/plain
HTTP 401
