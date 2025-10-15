# Building Minimal APIs from Scratch

This repo contains the code for my talk "Building Minimal APIs from Scratch" which explores the underpinnings of minimal APIs in particular and API frameworks in general by building a simple prototype that replicates the behavior of ASP.NET Core's minimal APIs.

The `katas.hurl` file contains a set of test cases that can be used to evaluate the behavior of the framework implemented.

## Running the Application

The entire API is implemented in a single file-based C# app and can be executed by running:

```bash
dotnet run app.cs
```

The server will start listening on `http://localhost:8080`. You can stop the server by pressing the `Esc` key.

## Running Tests with Hurl

This project uses [Hurl](https://hurl.dev/) to run HTTP tests against the API. Hurl is a command-line tool that runs HTTP requests defined in a simple plain text format.

### Installing Hurl

Install Hurl using one of the following methods:

**macOS:**

```bash
brew install hurl
```

**Linux:**

```bash
curl -LO https://github.com/Orange-OpenSource/hurl/releases/latest/download/hurl_x.x.x_amd64.deb
sudo dpkg -i hurl_x.x.x_amd64.deb
```

**Windows:**

```bash
choco install hurl
```

For more installation options, see the [Hurl installation guide](https://hurl.dev/docs/installation.html).

### Running the Tests

1. First, start the application in one terminal:

   ```bash
   dotnet run app.cs
   ```

2. In another terminal, run the Hurl tests:

   ```bash
   hurl --test katas.hurl
   ```

   This will execute all test entries in the file and display the results.

3. To run tests with verbose output:

   ```bash
   hurl --test --verbose katas.hurl
   ```

4. To run a specific test or see detailed output for debugging:

   ```bash
   hurl katas.hurl
   ```

The `katas.hurl` file contains 13 test entries organized by "clickstops" that test various features:

- **Clickstop 1-5**: Basic routing and middleware (404 handling)
- **Clickstop 6**: Query string parameter binding
- **Clickstop 8**: HTTP method routing (GET, POST, 405 errors)
- **Clickstop 14**: JSON body binding
- **Clickstop 15-16**: Metadata and authorization
