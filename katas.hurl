# Entry 1: Validates root endpoint returns 200 OK
GET http://localhost:8080/
Accept: text/plain
HTTP 200

# Entry 2: Validates nonexistent route returns 404 Not Found
GET http://localhost:8080/nonexistent
Accept: text/plain
HTTP 404

# Entry 3: Validates /bye endpoint returns 200 OK
GET http://localhost:8080/bye
Accept: text/plain
HTTP 200

# Entry 4: Tests query string parameter binding with name parameter
GET http://localhost:8080/hello?name=Safia
Accept: text/plain
HTTP 200

# Entry 5: Tests query string parameter binding with year parameter
GET http://localhost:8080/age?year=1996
Accept: text/plain
HTTP 200

# Entry 6: Tests GET method endpoint with query parameter
GET http://localhost:8080/hello?name=Safia
Accept: text/plain
HTTP 200

# Entry 7: Tests POST method endpoint returns 201 Created
POST http://localhost:8080/post?id=123
Content-Length: 0
Accept: text/plain
HTTP 201

# Entry 8: Tests invalid HTTP method returns 405 Method Not Allowed
GET http://localhost:8080/post?id=123
Accept: text/plain
HTTP 405

# Entry 9: Tests POST with JSON body for incomplete todo item
POST http://localhost:8080/post?id=200
Content-Type: application/json
{
  "title": "Buy groceries",
  "isCompleted": false
}
HTTP 200

# Entry 10: Tests POST with JSON body for completed todo item
POST http://localhost:8080/post?id=201
Content-Type: application/json
{
  "title": "Finish project",
  "isCompleted": true
}
HTTP 200

# Entry 11: Tests POST with complex JSON body
POST http://localhost:8080/post?id=202
Content-Type: application/json
{
  "title": "Learn ASP.NET Core internals",
  "isCompleted": false
}
HTTP 200

# Entry 12: Tests public endpoint returns 200 OK without authentication
GET http://localhost:8080/public
Accept: text/plain
HTTP 200

# Entry 13: Tests protected endpoint returns 401 Unauthorized without auth
GET http://localhost:8080/protected
Accept: text/plain
HTTP 401
